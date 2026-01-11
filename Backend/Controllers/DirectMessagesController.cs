using Backend.Data;
using Backend.DTOs; // Asigura-te ca namespace-ul e corect pentru DTOs
using Backend.DTOs.DirectMessageController;
using Backend.Hubs;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DirectMessagesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAiContentService _aiService;
        private readonly IHubContext<DirectMessageHub> _hubContext;

        public DirectMessagesController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            IAiContentService aiService,
            IHubContext<DirectMessageHub> hubContext)
        {
            _context = context;
            _userManager = userManager;
            _aiService = aiService;
            _hubContext = hubContext;
        }

        [HttpPost("send/{recipientUsername}")]
        public async Task<IActionResult> SendMessage(string recipientUsername, [FromBody] SendDirectMessageDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Content)) return BadRequest(new { error = "Message content cannot be empty." });

            if (dto.Content.Length > 500) return BadRequest(new { error = "Message content is too long (max 500 characters)." });

            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var recipient = await _userManager.FindByNameAsync(recipientUsername);

            if (recipient == null) return NotFound(new { error = "The recipient user does not exist." });

            if (currentUserId == recipient.Id) return BadRequest(new { error = "You cannot send a message to yourself." });

            // AI Safety Check-optional, I want to test the real-time speed and notifications
            
            //bool isSafe = await _aiService.IsContentSafeAsync(dto.Content);
            //if (!isSafe)
            //{ 
            //    return BadRequest(new { error = "Your content contains inappropriate terms. Please reformulate." });
            //}

            var message = new DirectMessage
            {
                SenderId = currentUserId!,
                ReceiverId = recipient.Id!,
                Content = dto.Content.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.DirectMessages.Add(message);
            await _context.SaveChangesAsync();

            // --- Get Sender Info for Response ---
            // MAO: Folosim FindByIdAsync, dar e bine sa verificam null chiar daca e putin probabil
            var sender = await _userManager.FindByIdAsync(currentUserId);
            if (sender == null) return Unauthorized();

            // --- Prepare DTO ---
            var messageDto = new DirectMessageResponseDTO
            {
                Id = message.Id,
                Content = message.Content,
                CreatedAt = message.CreatedAt,
                SenderId = sender.Id,
                SenderUsername = sender.UserName ?? "Unknown",
                SenderProfilePictureUrl = sender.ProfilePictureUrl,
                IsMine = false // Pentru receiver, acest mesaj NU este "al lui" (IsMine se calculeaza pe client sau la fetch)
            };

            // --- Real-Time SignalR ---
            var conversationId = GenerateConversationId(currentUserId, recipient.Id);

            // MAO: Trimitem DTO-ul complet prin SignalR ca frontend-ul să îl poată afișa direct
            await _hubContext.Clients.Group(conversationId).SendAsync("ReceiveDirectMessage", messageDto);

            return CreatedAtAction(nameof(SendMessage), new { id = message.Id }, new
            {
                id = message.Id,
                message = "Message sent successfully!"
            });
        }

        [HttpGet("conversation/{otherUsername}")]
        public async Task<IActionResult> GetConversation(string otherUsername)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId)) return Unauthorized();

            var otherUser = await _userManager.FindByNameAsync(otherUsername);
            if (otherUser == null) return NotFound(new { error = "Utilizatorul nu există." });

            // MAO: Folosim AsNoTracking() pentru performanta la citire
            var messages = await _context.DirectMessages
                .AsNoTracking()
                .Where(m => (m.SenderId == currentUserId && m.ReceiverId == otherUser.Id) ||
                            (m.SenderId == otherUser.Id && m.ReceiverId == currentUserId))
                .Include(m => m.Sender)
                .OrderBy(m => m.CreatedAt)
                .Select(m => new DirectMessageResponseDTO
                {
                    Id = m.Id,
                    Content = m.Content,
                    CreatedAt = m.CreatedAt,
                    SenderId = m.SenderId!,
                    SenderUsername = m.Sender!.UserName!,
                    SenderProfilePictureUrl = m.Sender.ProfilePictureUrl,
                    IsMine = m.SenderId == currentUserId
                })
                .ToListAsync();

            return Ok(messages);
        }

        // MAO: OPTIMIZARE MAJORA AICI
        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations()
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId)) return Unauthorized();

            // MAO: Strategie Optimizată
            // 1. Luăm toate mesajele unde suntem implicați.
            // 2. Grupăm după "celălalt utilizator" (dacă eu sunt sender, grupez după receiver, și invers).
            // 3. Selectăm cel mai recent mesaj din fiecare grup.
            // 4. Facem Join cu tabela de Users pentru a lua detaliile profilului dintr-un singur foc.

            var conversationsQuery = _context.DirectMessages
                .AsNoTracking()
                .Where(m => m.SenderId == currentUserId || m.ReceiverId == currentUserId)
                .GroupBy(m => m.SenderId == currentUserId ? m.ReceiverId : m.SenderId) // Group by the "other" person ID
                .Select(g => new
                {
                    OtherUserId = g.Key,
                    LastMessage = g.OrderByDescending(m => m.CreatedAt).FirstOrDefault()
                });

            // Executam query-ul pentru a aduce datele despre mesaje (Grouping in EF Core pe SQL complex poate fi tricky, 
            // uneori e mai sigur sa aducem cheile si apoi sa facem join, dar incercam varianta directa).

            // Nota: EF Core 6/7/8 suporta translarea complexa, dar cea mai sigura metoda performanta e sa luam ID-urile si LastMessage
            // si apoi sa luam userii. Pentru simplitate si siguranta in EF:

            var rawConversations = await conversationsQuery.ToListAsync();

            if (!rawConversations.Any())
                return Ok(new List<ConversationResponseDTO>());

            // Acum luam detaliile utilizatorilor pentru ID-urile gasite (IN clause query - foarte rapid)
            var userIds = rawConversations.Select(c => c.OtherUserId).ToList();
            var users = await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u);

            // Construim DTO-ul final in memorie
            var result = rawConversations
                .Select(c => {
                    var userExists = users.TryGetValue(c.OtherUserId, out var partner);
                    return new ConversationResponseDTO
                    {
                        OtherUserId = c.OtherUserId,
                        OtherUserUsername = userExists ? partner!.UserName : "Deleted User",
                        OtherUserProfilePictureUrl = userExists ? partner!.ProfilePictureUrl : null,
                        LastMessagePreview = c.LastMessage != null
                            ? (c.LastMessage.Content.Length > 50 ? c.LastMessage.Content.Substring(0, 50) + "..." : c.LastMessage.Content)
                            : "",
                        LastMessageTime = c.LastMessage?.CreatedAt ?? DateTime.MinValue,
                        UnreadCount = 0 // Logica de unread necesita un camp 'IsRead' in model
                    };
                })
                .OrderByDescending(c => c.LastMessageTime)
                .ToList();

            return Ok(result);
        }

        [HttpGet("{messageId}")]
        public async Task<IActionResult> GetMessage(int messageId)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var message = await _context.DirectMessages
               .AsNoTracking()
               .Include(m => m.Sender)
               .FirstOrDefaultAsync(m => m.Id == messageId);

            if (message == null) return NotFound();

            // Optional: Verificam daca userul are voie sa vada mesajul (e sender sau receiver)
            if (message.SenderId != currentUserId && message.ReceiverId != currentUserId)
                return Forbid();

            return Ok(new DirectMessageResponseDTO
            {
                Id = message.Id,
                Content = message.Content,
                CreatedAt = message.CreatedAt,
                SenderUsername = message.Sender?.UserName ?? "Unknown",
                SenderId = message.SenderId,
                SenderProfilePictureUrl = message.Sender.ProfilePictureUrl,
                IsMine = message.SenderId == currentUserId
            });
        }

        // PUT api/DirectMessages/{otherUsername}/{messageId}
        [HttpPut("{otherUsername}/{messageId}")]
        public async Task<IActionResult> EditMessage(string otherUsername, int messageId, [FromBody] SendDirectMessageDTO dto)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId)) return Unauthorized();

            // 1. Fetch the message first to verify existence and ownership
            var message = await _context.DirectMessages.FindAsync(messageId);

            if (message == null) return NotFound(new { error = "Message not found." });

            // 2. Verify Ownership
            if (message.SenderId != currentUserId)
            {
                return StatusCode(403, new { error = "You can only edit your own messages." });
            }

            // 3. Verify Context (Conversation integrity)
            var otherUser = await _userManager.FindByNameAsync(otherUsername);
            if (otherUser == null || message.ReceiverId != otherUser.Id)
            {
                return BadRequest(new { error = "Message does not belong to this conversation." });
            }

            var conversationId = GenerateConversationId(currentUserId, otherUser.Id);

            // ==================================================================================
            // LOGIC CHANGE: If content is empty/whitespace -> DELETE the message
            // ==================================================================================
            if (string.IsNullOrWhiteSpace(dto.Content))
            {
                _context.DirectMessages.Remove(message);
                await _context.SaveChangesAsync();

                // Notify SignalR that the message was DELETED
                // Matching the frontend signature: ({ messageId }) => ...
                await _hubContext.Clients.Group(conversationId).SendAsync("MessageDeleted", new { messageId = messageId });

                return Ok(new { message = "Message deleted because content was empty." });
            }

            // ==================================================================================
            // OTHERWISE -> UPDATE the message
            // ==================================================================================
            if (dto.Content.Length > 500)
                return BadRequest(new { error = "Content too long." });

            message.Content = dto.Content.Trim();

            _context.DirectMessages.Update(message);
            await _context.SaveChangesAsync();

            // Notify SignalR that the message was EDITED
            await _hubContext.Clients.Group(conversationId).SendAsync("MessageEdited", new
            {
                id = message.Id,
                content = message.Content
            });

            return Ok(new { message = "Message updated successfully", content = message.Content });
        }

        [HttpDelete("{messageId}")]
        public async Task<IActionResult> DeleteMessage(int messageId)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Nu folosim AsNoTracking aici pentru ca vrem sa stergem entitatea
            var message = await _context.DirectMessages.FindAsync(messageId);

            if (message == null) return NotFound();

            // Check ownership
            if (message.SenderId != currentUserId)
                return StatusCode(403, new { error = "Nu poți șterge mesajele altora." });

            _context.DirectMessages.Remove(message);
            await _context.SaveChangesAsync();

            // Notificam grupul SignalR
            var conversationId = GenerateConversationId(currentUserId, message.ReceiverId);
            await _hubContext.Clients.Group(conversationId).SendAsync("MessageDeleted", messageId); // Trimitem doar ID-ul, nu obiect

            return Ok(new { message = "Mesaj șters." });
        }

        // Helper pentru ID unic de conversatie (alfabetic)
        private static string GenerateConversationId(string userId1, string userId2)
        {
            var sorted = new[] { userId1, userId2 }.OrderBy(x => x).ToArray();
            return $"conversation_{sorted[0]}_{sorted[1]}";
        }
    }
}