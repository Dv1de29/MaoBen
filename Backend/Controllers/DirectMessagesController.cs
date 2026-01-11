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
        public async Task<IActionResult> SendMessage(string recipientUsername, [FromBody] SendDirectMessageDto dto)
        {
            // --- VALIDARE 1: Input Validation ---
            if (string.IsNullOrWhiteSpace(dto.Content))
                return BadRequest(new { error = "Mesajul nu poate fi gol." });

            if (dto.Content.Length > 1000)
                return BadRequest(new { error = "Mesajul nu poate depași 1000 de caractere." });

            // --- VALIDARE 2: Current User ---
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized(new { error = "Nu ești autentificat." });

            // --- VALIDARE 3: Recipient by Username ---
            var recipient = await _userManager.FindByNameAsync(recipientUsername);
            if (recipient == null)
                return NotFound(new { error = $"Utilizatorul '{recipientUsername}' nu există." });

            // --- VALIDARE 4: Self-Message Prevention ---
            if (currentUserId == recipient.Id)
                return BadRequest(new { error = "Nu poți trimite mesaje către tine." });

            // --- VALIDARE 5: AI Content Check ---
            bool isSafe = await _aiService.IsContentSafeAsync(dto.Content);
            if (!isSafe)
                return BadRequest(new { error = "Mesajul conține termeni nepotriviți (AI Filter)." });

            // --- ACTION: Save Message ---
            var message = new DirectMessage
            {
                SenderId = currentUserId,
                ReceiverId = recipient.Id,
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
            var messageDto = new DirectMessageDto
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
                content = message.Content,
                createdAt = message.CreatedAt,
                message = "Mesaj trimis cu succes!"
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
                .Select(m => new DirectMessageDto
                {
                    Id = m.Id,
                    Content = m.Content,
                    CreatedAt = m.CreatedAt,
                    SenderId = m.SenderId,
                    SenderUsername = m.Sender!.UserName ?? "Unknown",
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
                return Ok(new List<ConversationDto>());

            // Acum luam detaliile utilizatorilor pentru ID-urile gasite (IN clause query - foarte rapid)
            var userIds = rawConversations.Select(c => c.OtherUserId).ToList();
            var users = await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u);

            // Construim DTO-ul final in memorie
            var result = rawConversations
                .Select(c => {
                    var userExists = users.TryGetValue(c.OtherUserId, out var partner);
                    return new ConversationDto
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

            return Ok(new DirectMessageDto
            {
                Id = message.Id,
                Content = message.Content,
                CreatedAt = message.CreatedAt,
                SenderUsername = message.Sender?.UserName ?? "Unknown",
                IsMine = message.SenderId == currentUserId
            });
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