using Backend.Data;
using Backend.DTOs;
using Backend.DTOs.GroupController;
using Backend.Models;
using Backend.Services;
using Backend.Hubs;
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
    public class GroupsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAiContentService _aiService;
        private readonly IHubContext<DirectMessageHub> _hubContext;

        public GroupsController(
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

        [HttpPost]
        public async Task<IActionResult> CreateGroup([FromBody] CreateGroupDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Description))
                return BadRequest(new { error = "Name and description are required." });

            // --- AI VALIDATION: Name and Description ---
            //if (!await _aiService.IsContentSafeAsync(dto.Name) || !await _aiService.IsContentSafeAsync(dto.Description))
            //{
            //    return BadRequest(new { error = "The group name or description contains inappropriate terms." });
            //}

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(currentUserId))
                return BadRequest(new { error = "User identification error." });

            var group = new Group
            {
                Name = dto.Name,
                Description = dto.Description,
                OwnerId = currentUserId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Groups.Add(group);
            await _context.SaveChangesAsync();

            var member = new GroupMember
            {
                GroupId = group.Id,
                UserId = currentUserId,
                Status = GroupMemberStatus.Accepted
            };
            _context.GroupMembers.Add(member);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Group created successfully!", groupId = group.Id });
        }

        [HttpGet]
        public async Task<IActionResult> GetAllGroups()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(currentUserId))
                return BadRequest(new { error = "User identification error." });

            var groups = await _context.Groups
                .Include(g => g.Owner)
                .Select(g => new ShowGroupResponseDTO
                {
                    Id = g.Id,
                    Name = g.Name,
                    Description = g.Description,
                    OwnerUsername = g.Owner.UserName!,
                    IsUserMember = _context.GroupMembers.Any(gm => gm.GroupId == g.Id && gm.UserId == currentUserId)
                })
                .ToListAsync();
            return Ok(groups);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGroup(int id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var group = await _context.Groups.FindAsync(id);

            if (group == null) return NotFound(new { error = "Group not found." });


            if (group.OwnerId != currentUserId)
                return StatusCode(403, new { error = "Only the moderator or an administrator can delete the group." });

            _context.Groups.Remove(group);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Group has been deleted." });
        }

        [HttpPost("{id}/join")]
        public async Task<IActionResult> JoinGroup(int id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(currentUserId))
                return BadRequest(new { error = "User identification error." });

            var groupExists = await _context.Groups.AnyAsync(g => g.Id == id);
            if (!groupExists) return NotFound(new { error = "Group not found." });

            var existingMember = await _context.GroupMembers.FindAsync(id, currentUserId);
            if (existingMember != null)
            {
                if (existingMember.Status == GroupMemberStatus.Pending)
                    return BadRequest(new { error = "You have already sent a join request." });
                return BadRequest(new { error = "You are already a member of this group." });
            }

            var newMember = new GroupMember
            {
                GroupId = id,
                UserId = currentUserId,
                Status = GroupMemberStatus.Pending
            };

            _context.GroupMembers.Add(newMember);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Join request sent to the moderator." });
        }

        [HttpGet("{id}/requests")]
        public async Task<IActionResult> GetPendingRequests(int id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(currentUserId))
                return BadRequest(new { error = "User identification error." });
            var group = await _context.Groups.FindAsync(id);

            if (group == null) return NotFound(new { error = "Group not found." });
            if (group.OwnerId != currentUserId) return StatusCode(403, new { error = "You are not the moderator of this group." });

            var requests = await _context.GroupMembers
                .Where(gm => gm.GroupId == id && gm.Status == GroupMemberStatus.Pending)
                .Include(gm => gm.User)
                .Select(gm => new PendingRequestsResponseDTO
                {
                    Username = gm.User.UserName!,
                    ProfilePictureUrl = gm.User.ProfilePictureUrl
                })
                .ToListAsync();

            return Ok(requests);
        }

        // PUT: api/Groups/{id}/accept/{username}
        [HttpPut("{id}/accept/{username}")]
        public async Task<IActionResult> AcceptMember(int id, string username)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(currentUserId))
                return BadRequest(new { error = "User identification error." });

            var group = await _context.Groups.FindAsync(id);
            if (group == null)
                return NotFound(new { error = "Group not found." });

            bool isModerator = group.OwnerId == currentUserId;

            if (!isModerator)
            {
                return StatusCode(403, new { error = "Only the group moderator or an administrator can accept members." });
            }

            var targetUser = await _userManager.FindByNameAsync(username);
            if (targetUser == null)
                return NotFound(new { error = $"User '{username}' not found." });

            var member = await _context.GroupMembers.FindAsync(id, targetUser.Id);
            if (member == null)
                return NotFound(new { error = "Join request not found for this user." });

            if (member.Status == GroupMemberStatus.Accepted)
            {
                return BadRequest(new { error = "User is already an accepted member of this group." });
            }

            member.Status = GroupMemberStatus.Accepted;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"User '{username}' has been accepted into the group." });
        }

        // DELETE: api/Groups/{id}/members/{username}
        [HttpDelete("{id}/members/{username}")]
        public async Task<IActionResult> RemoveMember(int id, string username)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(currentUserId))
                return BadRequest(new { error = "User identification error." });

            var group = await _context.Groups.FindAsync(id);
            if (group == null) return NotFound(new { error = "Group not found." });

            var targetUser = await _userManager.FindByNameAsync(username);
            if (targetUser == null)
                return NotFound(new { error = $"User '{username}' not found." });

            var memberToRemove = await _context.GroupMembers.FindAsync(id, targetUser.Id);
            if (memberToRemove == null)
                return NotFound(new { error = "This user is not a member of this group." });

            bool isGroupModerator = group.OwnerId == currentUserId;
            bool isSelf = targetUser.Id == currentUserId;

            if (!isSelf && !isGroupModerator)
            {
                return StatusCode(403, new { error = "You do not have permission to remove this member." });
            }

            if (targetUser.Id == group.OwnerId)
            {
                return BadRequest(new { error = "The moderator cannot leave the group. The group must be deleted instead." });
            }

            _context.GroupMembers.Remove(memberToRemove);
            await _context.SaveChangesAsync();

            return Ok(new { message = isSelf ? "You have left the group." : $"User '{username}' was removed from the group." });
        }

        [HttpGet("{id}/messages")]
        public async Task<IActionResult> GetMessages(int id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(currentUserId))
                return BadRequest(new { error = "User identification error." });
            var isMember = await _context.GroupMembers
                .AnyAsync(gm => gm.GroupId == id && gm.UserId == currentUserId && gm.Status == GroupMemberStatus.Accepted);

            if (!isMember) return StatusCode(403, new { error = "You must be an accepted member to view group messages." });

            var messages = await _context.GroupMessages
                .Where(m => m.GroupId == id)
                .Include(m => m.User)
                .OrderBy(m => m.CreatedAt)
                .Select(m => new GroupMessageResponseDTO
                {
                    Id = m.Id,
                    Content = m.Content,
                    CreatedAt = m.CreatedAt,
                    Username = m.User.UserName!,
                    ProfilePictureUrl = m.User.ProfilePictureUrl,
                    IsMine = m.UserId == currentUserId
                })
                .ToListAsync();

            return Ok(messages);
        }

        [HttpPost("{id}/messages")]
        public async Task<IActionResult> SendMessage(int id, [FromBody] SendGroupMessageDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Content)) return BadRequest(new { error = "Message cannot be empty." });

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = await _userManager.FindByIdAsync(currentUserId!);

            if (string.IsNullOrWhiteSpace(currentUserId) || currentUser == null)
                return BadRequest(new { error = "User identification error." });

            var isMember = await _context.GroupMembers
                .AnyAsync(gm => gm.GroupId == id && gm.UserId == currentUserId && gm.Status == GroupMemberStatus.Accepted);

            if (!isMember) return StatusCode(403, new { error = "You cannot send messages to a group you are not a member of." });

            var message = new GroupMessage
            {
                GroupId = id,
                UserId = currentUserId,
                Content = dto.Content,
                CreatedAt = DateTime.UtcNow
            };

            _context.GroupMessages.Add(message);
            await _context.SaveChangesAsync();

            // --- SIGNALR REAL-TIME BROADCAST ---
            var messageDto = new GroupMessageResponseDTO
            {
                Id = message.Id,
                Content = message.Content,
                CreatedAt = message.CreatedAt,
                Username = currentUser.UserName!,
                ProfilePictureUrl = currentUser.ProfilePictureUrl,
                IsMine = false // Frontend will calculate 'IsMine'
            };

            
            await _hubContext.Clients.Group($"group_{id}").SendAsync("ReceiveGroupMessage", messageDto);

            return Ok(new { message = "Message sent successfully." });
        }


        [HttpDelete("{groupId}/messages/{messageId}")]
        public async Task<IActionResult> DeleteMessage(int groupId, int messageId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(currentUserId))
                return BadRequest(new { error = "User identification error." });

            var message = await _context.GroupMessages.FindAsync(messageId);
            if (message == null || message.GroupId != groupId)
                return NotFound(new { error = "Message not found in this group." });

            var group = await _context.Groups.FindAsync(groupId);
            if (group == null)
                return NotFound(new { error = "Group not found." });

            if (message.UserId != currentUserId && group.OwnerId != currentUserId)
                return StatusCode(403, new { error = "You can only delete your own messages unless you are an administrator." });

            _context.GroupMessages.Remove(message);
            await _context.SaveChangesAsync();

            // CHANGE 3: Add Broadcast for Deletion
            await _hubContext.Clients.Group($"group_{groupId}").SendAsync("GroupMessageDeleted", new { messageId = messageId });

            return Ok(new { message = "Message deleted successfully." });
        }



        [HttpPut("{groupId}/messages/{messageId}")]
        public async Task<IActionResult> EditMessage(int groupId, int messageId, [FromBody] SendGroupMessageDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Content))
                return BadRequest(new { error = "Updated content cannot be empty." });

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(currentUserId))
                return BadRequest(new { error = "User identification error." });

            var message = await _context.GroupMessages.FindAsync(messageId);
            if (message == null || message.GroupId != groupId)
                return NotFound(new { error = "Message not found in this group." });

            var group = await _context.Groups.FindAsync(groupId);
            if (group == null)
                return NotFound(new { error = "Group not found." });

            if (message.UserId != currentUserId && group.OwnerId != currentUserId)
                return StatusCode(403, new { error = "You can only edit your own messages unless you are an administrator." });

            message.Content = dto.Content.Trim();
            _context.GroupMessages.Update(message);
            await _context.SaveChangesAsync();

            // CHANGE 4: Broadcast Edit to the correct "group_{id}"
            await _hubContext.Clients.Group($"group_{groupId}").SendAsync("GroupMessageEdited", new
            {
                messageId = message.Id,
                content = message.Content
            });

            return Ok(new { message = "Message updated successfully.", content = message.Content });
        }
    }
}