using Backend.Data;
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
    [Authorize] // Doar utilizatorii logați pot trimite/primi mesaje directe
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

        /// <summary>
        /// POST /api/directmessages/send/{recipientId}
        /// Trimite un mesaj direct unui utilizator
        /// 
        /// Frontend Usage:
        /// const response = await fetch('http://localhost:5000/api/directmessages/send/user-id-123', {
        ///     method: 'POST',
        ///     headers: {
        ///         'Content-Type': 'application/json',
        ///         'Authorization': `Bearer ${token}`
        ///     },
        ///     body: JSON.stringify({ content: 'Hello!' })
        /// });
        /// const data = await response.json();
        /// 
        /// Notes:
        /// - Mesajul este salvat în baza de date
        /// - Se validează conținutul folosind AI
        /// - Se trimite în timp real prin SignalR
        /// - Nu este necesar ca utilizatorii să se urmărească
        /// </summary>
        [HttpPost("send/{recipientId}")]
        public async Task<IActionResult> SendMessage(string recipientId, [FromBody] SendDirectMessageDto dto)
        {
            // --- VALIDARE 1: Input Validation ---
            if (string.IsNullOrWhiteSpace(dto.Content))
            {
                return BadRequest(new { error = "Mesajul nu poate fi gol." });
            }

            if (dto.Content.Length > 1000)
            {
                return BadRequest(new { error = "Mesajul nu poate depași 1000 de caractere." });
            }

            // --- VALIDARE 2: Current User Identification ---
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized(new { error = "Nu ești autentificat." });
            }

            // --- VALIDARE 3: Self-Message Prevention ---
            if (currentUserId == recipientId)
            {
                return BadRequest(new { error = "Nu poți trimite mesaje către tine." });
            }

            // --- VALIDARE 4: Recipient Existence ---
            var recipient = await _userManager.FindByIdAsync(recipientId);
            if (recipient == null)
            {
                return NotFound(new { error = "Utilizatorul destinatar nu există." });
            }

            // --- VALIDARE 5: AI Content Check ---
            bool isSafe = await _aiService.IsContentSafeAsync(dto.Content);
            if (!isSafe)
            {
                return BadRequest(new { error = "Mesajul conține termeni nepotriviți. Mesajul nu a fost trimis." });
            }

            // --- ACTION: Create & Save Message ---
            var message = new DirectMessage
            {
                SenderId = currentUserId,
                ReceiverId = recipientId,
                Content = dto.Content.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.DirectMessages.Add(message);
            await _context.SaveChangesAsync();

            // --- Get Sender Info ---
            var sender = await _userManager.FindByIdAsync(currentUserId);

            // --- Prepare DTO for Real-Time Broadcasting ---
            var messageDto = new DirectMessageDto
            {
                Id = message.Id,
                Content = message.Content,
                CreatedAt = message.CreatedAt,
                SenderId = sender!.Id,
                SenderUsername = sender.UserName ?? "Unknown",
                SenderProfilePictureUrl = sender.ProfilePictureUrl,
                IsMine = false // Cealaltă persoană o va vedea cu false
            };

            // --- Real-Time Broadcasting via SignalR ---
            var conversationId = GenerateConversationId(currentUserId, recipientId);
            await _hubContext.Clients.Group(conversationId).SendAsync("ReceiveDirectMessage", messageDto);

            // --- Return Success Response ---
            return CreatedAtAction(nameof(SendMessage), new { id = message.Id }, new
            {
                id = message.Id,
                content = message.Content,
                createdAt = message.CreatedAt,
                message = "Mesaj trimis cu succes!"
            });
        }

        /// <summary>
        /// GET /api/directmessages/conversation/{otherUserId}
        /// Obține toate mesajele dintr-o conversație cu un utilizator specific
        /// 
        /// Frontend Usage:
        /// const response = await fetch('http://localhost:5000/api/directmessages/conversation/user-id-123', {
        ///     headers: { 'Authorization': `Bearer ${token}` }
        /// });
        /// const messages = await response.json();
        /// messages.forEach(msg => {
        ///     console.log(msg.senderUsername + ': ' + msg.content);
        /// });
        /// 
        /// Returns:
        /// - Array of DirectMessageDto objects
        /// - Messages sorted by CreatedAt (oldest first)
        /// - Each message includes sender info and whether it's the current user's message
        /// </summary>
        [HttpGet("conversation/{otherUserId}")]
        public async Task<IActionResult> GetConversation(string otherUserId)
        {
            // --- VALIDARE 1: Other User Existence ---
            var otherUser = await _userManager.FindByIdAsync(otherUserId);
            if (otherUser == null)
            {
                return NotFound(new { error = "Utilizatorul nu există." });
            }

            // --- VALIDARE 2: Current User Identification ---
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized(new { error = "Nu ești autentificat." });
            }

            // --- VALIDARE 3: Self-Check ---
            if (currentUserId == otherUserId)
            {
                return BadRequest(new { error = "Nu poți vizualiza conversația cu tine." });
            }

            // --- ACTION: Fetch Conversation ---
            var messages = await _context.DirectMessages
                .Where(m => (m.SenderId == currentUserId && m.ReceiverId == otherUserId) ||
                           (m.SenderId == otherUserId && m.ReceiverId == currentUserId))
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

        /// <summary>
        /// GET /api/directmessages/conversations
        /// Obține lista conversațiilor curente (perechi de utilizatori cu care ai schimbat mesaje)
        /// 
        /// Frontend Usage:
        /// const response = await fetch('http://localhost:5000/api/directmessages/conversations', {
        ///     headers: { 'Authorization': `Bearer ${token}` }
        /// });
        /// const conversations = await response.json();
        /// conversations.forEach(conv => {
        ///     console.log('Conversation with ' + conv.otherUserUsername);
        ///     console.log('Last message: ' + conv.lastMessagePreview);
        ///     console.log('Unread: ' + conv.unreadCount);
        /// });
        /// 
        /// Returns:
        /// - Array of ConversationDto objects
        /// - Sorted by LastMessageTime (newest first)
        /// - Includes unread message count for each conversation
        /// </summary>
        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations()
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized(new { error = "Nu ești autentificat." });
            }

            // --- ACTION: Get Unique Conversation Partners ---
            var conversationPartners = await _context.DirectMessages
                .Where(m => m.SenderId == currentUserId || m.ReceiverId == currentUserId)
                .SelectMany(m => new[] { m.SenderId, m.ReceiverId }.Where(id => id != currentUserId))
                .Distinct()
                .ToListAsync();

            var conversations = new List<ConversationDto>();

            foreach (var partnerId in conversationPartners)
            {
                var partner = await _userManager.FindByIdAsync(partnerId);
                if (partner == null) continue;

                // Get last message in conversation
                var lastMessage = await _context.DirectMessages
                    .Where(m => (m.SenderId == currentUserId && m.ReceiverId == partnerId) ||
                               (m.SenderId == partnerId && m.ReceiverId == currentUserId))
                    .OrderByDescending(m => m.CreatedAt)
                    .FirstOrDefaultAsync();

                conversations.Add(new ConversationDto
                {
                    OtherUserId = partner.Id,
                    OtherUserUsername = partner.UserName ?? "Unknown",
                    OtherUserProfilePictureUrl = partner.ProfilePictureUrl,
                    LastMessagePreview = lastMessage?.Content?.Substring(0, Math.Min(50, lastMessage.Content.Length)) + "...",
                    LastMessageTime = lastMessage?.CreatedAt,
                    UnreadCount = 0 // TODO: Implement unread tracking if needed
                });
            }

            // Sort by last message time (newest first)
            conversations = conversations.OrderByDescending(c => c.LastMessageTime).ToList();

            return Ok(conversations);
        }

        /// <summary>
        /// GET /api/directmessages/{messageId}
        /// Obține detaliile unui mesaj specific
        /// 
        /// Frontend Usage:
        /// const response = await fetch('http://localhost:5000/api/directmessages/123', {
        ///     headers: { 'Authorization': `Bearer ${token}` }
        /// });
        /// const message = await response.json();
        /// 
        /// Returns:
        /// - Single DirectMessageDto object
        /// </summary>
        [HttpGet("{messageId}")]
        public async Task<IActionResult> GetMessage(int messageId)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized(new { error = "Nu ești autentificat." });
            }

            var message = await _context.DirectMessages
                .Where(m => m.Id == messageId && (m.SenderId == currentUserId || m.ReceiverId == currentUserId))
                .Include(m => m.Sender)
                .FirstOrDefaultAsync();

            if (message == null)
            {
                return NotFound(new { error = "Mesajul nu a fost găsit." });
            }

            var messageDto = new DirectMessageDto
            {
                Id = message.Id,
                Content = message.Content,
                CreatedAt = message.CreatedAt,
                SenderId = message.SenderId,
                SenderUsername = message.Sender!.UserName ?? "Unknown",
                SenderProfilePictureUrl = message.Sender.ProfilePictureUrl,
                IsMine = message.SenderId == currentUserId
            };

            return Ok(messageDto);
        }

        /// <summary>
        /// DELETE /api/directmessages/{messageId}
        /// Șterge un mesaj direct (doar autorul poate șterge)
        /// 
        /// Frontend Usage:
        /// const response = await fetch('http://localhost:5000/api/directmessages/123', {
        ///     method: 'DELETE',
        ///     headers: { 'Authorization': `Bearer ${token}` }
        /// });
        /// const result = await response.json();
        /// console.log(result.message); // "Mesaj șters cu succes!"
        /// 
        /// Returns:
        /// - Success message if deleted
        /// - 403 Forbidden if not the author
        /// - 404 Not Found if message doesn't exist
        /// </summary>
        [HttpDelete("{messageId}")]
        public async Task<IActionResult> DeleteMessage(int messageId)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized(new { error = "Nu ești autentificat." });
            }

            var message = await _context.DirectMessages.FindAsync(messageId);
            if (message == null)
            {
                return NotFound(new { error = "Mesajul nu a fost găsit." });
            }

            // Only the sender can delete their message
            if (message.SenderId != currentUserId)
            {
                return StatusCode(403, new { error = "Poți șterge doar propriile mesaje." });
            }

            _context.DirectMessages.Remove(message);
            await _context.SaveChangesAsync();

            // Notify the other user about the deleted message via SignalR
            var conversationId = GenerateConversationId(currentUserId, message.ReceiverId);
            await _hubContext.Clients.Group(conversationId).SendAsync("MessageDeleted", new { messageId });

            return Ok(new { message = "Mesaj șters cu succes!" });
        }

        /// <summary>
        /// PATCH /api/directmessages/{messageId}/edit
        /// Editează un mesaj direct (doar autorul poate edita)
        /// 
        /// Frontend Usage:
        /// const response = await fetch('http://localhost:5000/api/directmessages/123/edit', {
        ///     method: 'PATCH',
        ///     headers: {
        ///         'Content-Type': 'application/json',
        ///         'Authorization': `Bearer ${token}`
        ///     },
        ///     body: JSON.stringify({ content: 'Updated message content' })
        /// });
        /// const result = await response.json();
        /// 
        /// Returns:
        /// - Updated message DTO
        /// - 403 Forbidden if not the author
        /// - 404 Not Found if message doesn't exist
        /// </summary>
        [HttpPatch("{messageId}/edit")]
        public async Task<IActionResult> EditMessage(int messageId, [FromBody] SendDirectMessageDto dto)
        {
            // --- VALIDARE 1: Input Validation ---
            if (string.IsNullOrWhiteSpace(dto.Content))
            {
                return BadRequest(new { error = "Mesajul nu poate fi gol." });
            }

            if (dto.Content.Length > 1000)
            {
                return BadRequest(new { error = "Mesajul nu poate depași 1000 de caractere." });
            }

            // --- VALIDARE 2: Current User Identification ---
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized(new { error = "Nu ești autentificat." });
            }

            // --- VALIDARE 3: Message Existence & Authorization ---
            var message = await _context.DirectMessages
                .Include(m => m.Sender)
                .FirstOrDefaultAsync(m => m.Id == messageId);

            if (message == null)
            {
                return NotFound(new { error = "Mesajul nu a fost găsit." });
            }

            if (message.SenderId != currentUserId)
            {
                return StatusCode(403, new { error = "Poți edita doar propriile mesaje." });
            }

            // --- VALIDARE 4: AI Content Check ---
            bool isSafe = await _aiService.IsContentSafeAsync(dto.Content);
            if (!isSafe)
            {
                return BadRequest(new { error = "Mesajul conține termeni nepotriviți. Mesajul nu a fost actualizat." });
            }

            // --- ACTION: Update Message ---
            message.Content = dto.Content.Trim();
            _context.DirectMessages.Update(message);
            await _context.SaveChangesAsync();

            // --- Prepare DTO for Real-Time Broadcasting ---
            var messageDto = new DirectMessageDto
            {
                Id = message.Id,
                Content = message.Content,
                CreatedAt = message.CreatedAt,
                SenderId = message.SenderId,
                SenderUsername = message.Sender!.UserName ?? "Unknown",
                SenderProfilePictureUrl = message.Sender.ProfilePictureUrl,
                IsMine = false
            };

            // --- Real-Time Broadcasting via SignalR ---
            var conversationId = GenerateConversationId(currentUserId, message.ReceiverId);
            await _hubContext.Clients.Group(conversationId).SendAsync("MessageEdited", messageDto);

            return Ok(new
            {
                message = "Mesaj actualizat cu succes!",
                data = messageDto
            });
        }

        /// <summary>
        /// Helper method to generate consistent conversation IDs
        /// Ensures both users are in the same SignalR group
        /// </summary>
        private static string GenerateConversationId(string userId1, string userId2)
        {
            var sorted = new[] { userId1, userId2 }.OrderBy(x => x).ToArray();
            return $"conversation_{sorted[0]}_{sorted[1]}";
        }
    }
}