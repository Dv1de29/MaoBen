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

            var messageDto = new DirectMessageResponseDTO
            {
                Id = message.Id,
                Content = message.Content,
                CreatedAt = message.CreatedAt,
                SenderId = message.SenderId!,
                SenderUsername = message.Sender.UserName!,
                SenderProfilePictureUrl= message.Sender.ProfilePictureUrl,
                IsMine = false
            };

            var conversationId = GenerateConversationId(currentUserId!, recipient.Id);
            await _hubContext.Clients.Group(conversationId).SendAsync("ReceiveDirectMessage", messageDto);

            return CreatedAtAction(nameof(GetMessage), new { messageId = message.Id }, new
            {
                id = message.Id,
                message = "Message sent successfully!"
            });
        }


        [HttpGet("conversation/{otherUsername}")]
        public async Task<IActionResult> GetConversation(string otherUsername)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var otherUser = await _userManager.FindByNameAsync(otherUsername);

            if (otherUser == null)
                return NotFound(new { error = "Target user not found." });

            var messages = await _context.DirectMessages
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


        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations()
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId)) return Unauthorized();

            var conversationPartners = await _context.DirectMessages
                .Where(m => m.SenderId == currentUserId || m.ReceiverId == currentUserId)
                .Select(m => m.SenderId == currentUserId ? m.ReceiverId : m.SenderId)
                .Distinct()
                .ToListAsync();

            var conversations = new List<ConversationResponseDTO>();

            foreach (var partnerId in conversationPartners)
            {
                var partner = await _userManager.FindByIdAsync(partnerId);
                if (partner == null) continue;

                var lastMessage = await _context.DirectMessages
                    .Where(m => (m.SenderId == currentUserId && m.ReceiverId == partnerId) ||
                               (m.SenderId == partnerId && m.ReceiverId == currentUserId))
                    .OrderByDescending(m => m.CreatedAt)
                    .FirstOrDefaultAsync();

                conversations.Add(new ConversationResponseDTO
                {
                    OtherUserId = partner.Id!,
                    OtherUserUsername = partner.UserName!,
                    OtherUserProfilePictureUrl = partner.ProfilePictureUrl,
                    LastMessagePreview = lastMessage?.Content?.Length > 10
                        ? lastMessage.Content.Substring(0, 10) + "..."
                        : lastMessage?.Content,
                    LastMessageTime = lastMessage?.CreatedAt
                });
            }
            return Ok(conversations.OrderByDescending(c => c.LastMessageTime));
        }


        [HttpGet("{messageId}")]
        public async Task<IActionResult> GetMessage(int messageId)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var message = await _context.DirectMessages
                .Include(m => m.Sender)
                .FirstOrDefaultAsync(m => m.Id == messageId && (m.SenderId == currentUserId || m.ReceiverId == currentUserId));

            if (message == null) return NotFound(new { error = "Message not found or access denied." });

            return Ok(new DirectMessageResponseDTO
            {
                Id = message.Id,
                Content = message.Content,
                CreatedAt = message.CreatedAt,
                SenderId = message.SenderId!,
                SenderUsername = message.Sender?.UserName!,
                SenderProfilePictureUrl = message.Sender?.ProfilePictureUrl!,
                IsMine = message.SenderId == currentUserId
            });
        }

        [HttpDelete("{messageId}")]
        public async Task<IActionResult> DeleteMessage(int messageId)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var message = await _context.DirectMessages.FindAsync(messageId);

            if (message == null) return NotFound(new { error = "Message not found." });

            if (message.SenderId != currentUserId && User.IsInRole("Admin"))
                return StatusCode(403, new { error = "You are only allowed to delete your own messages." });

            _context.DirectMessages.Remove(message);
            await _context.SaveChangesAsync();

            var conversationId = GenerateConversationId(message.SenderId, message.ReceiverId);
            await _hubContext.Clients.Group(conversationId).SendAsync("MessageDeleted", new { messageId });

            return Ok(new { message = "Message deleted successfully." });
        }


        [HttpPost("{messageId}/edit")]
        public async Task<IActionResult> EditMessage(int messageId, [FromBody] SendDirectMessageDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Content))
                return BadRequest(new { error = "Updated content cannot be empty." });

            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var message = await _context.DirectMessages.FindAsync(messageId);

            if (message == null) return NotFound(new { error = "Message not found." });

            if (message.SenderId != currentUserId && User.IsInRole("Admin"))
                return StatusCode(403, new { error = "You are only allowed to edit your own messages." });


            //AI Verification for profanity - optional, I want to test the real-time speed and notifications
            
            //bool isSafe = await _aiService.IsContentSafeAsync(dto.Content);
            //if (!isSafe)
            //    return BadRequest(new { error = "Your content contains inappropriate terms. Please reformulate." });

            message.Content = dto.Content.Trim();
            _context.DirectMessages.Update(message);
            await _context.SaveChangesAsync();

            var conversationId = GenerateConversationId(message.SenderId, message.ReceiverId);
            await _hubContext.Clients.Group(conversationId).SendAsync("MessageEdited", new { messageId, content = message.Content });

            return Ok(new { message = "Message updated successfully.", content = message.Content });
        }
        [NonAction]
        private static string GenerateConversationId(string userId1, string userId2)
        {
            var sorted = new[] { userId1, userId2 }.OrderBy(x => x).ToArray();
            return $"conversation_{sorted[0]}_{sorted[1]}";
        }
    }
}