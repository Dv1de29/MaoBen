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
        private readonly IHubContext<ChatHub> _hubContext;

        // Injectăm IHubContext în constructor
        public GroupsController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            IAiContentService aiService,
            IHubContext<ChatHub> hubContext) // <--- PARAMETRU NOU
        {
            _context = context;
            _userManager = userManager;
            _aiService = aiService;
            _hubContext = hubContext; // <--- ATRIBUIRE
        }

        // POST: api/Groups (Creare Grup)
        [HttpPost]
        public async Task<IActionResult> CreateGroup([FromBody] CreateGroupDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Description))
                return BadRequest("Numele și descrierea sunt obligatorii.");

            // --- VALIDARE AI: Nume si Descriere ---
            if (!await _aiService.IsContentSafeAsync(dto.Name) || !await _aiService.IsContentSafeAsync(dto.Description))
            {
                return BadRequest("Numele sau descrierea grupului conțin termeni nepotriviți.");
            }
            // -------------------------------------

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

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

            return Ok(new { message = "Grup creat cu succes!", groupId = group.Id });
        }

        // GET: api/Groups
        [HttpGet]
        public async Task<IActionResult> GetAllGroups()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var groups = await _context.Groups
                .Include(g => g.Owner)
                .Select(g => new GroupDto
                {
                    Id = g.Id,
                    Name = g.Name,
                    Description = g.Description,
                    OwnerUsername = g.Owner.UserName,
                    IsUserMember = _context.GroupMembers.Any(gm => gm.GroupId == g.Id && gm.UserId == currentUserId)
                })
                .ToListAsync();

            return Ok(groups);
        }

        // DELETE: api/Groups/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGroup(int id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var group = await _context.Groups.FindAsync(id);

            if (group == null) return NotFound("Grupul nu există.");

            bool isAdmin = User.IsInRole("Admin") || User.IsInRole("Administrator");

            if (group.OwnerId != currentUserId && !isAdmin)
                return StatusCode(403, "Doar moderatorul poate șterge grupul.");

            _context.Groups.Remove(group);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Grupul a fost șters." });
        }

        // POST: api/Groups/{id}/join
        [HttpPost("{id}/join")]
        public async Task<IActionResult> JoinGroup(int id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var groupExists = await _context.Groups.AnyAsync(g => g.Id == id);
            if (!groupExists) return NotFound("Grupul nu există.");

            var existingMember = await _context.GroupMembers.FindAsync(id, currentUserId);
            if (existingMember != null)
            {
                if (existingMember.Status == GroupMemberStatus.Pending)
                    return BadRequest("Ai trimis deja o cerere.");
                return BadRequest("Ești deja membru în acest grup.");
            }

            var newMember = new GroupMember
            {
                GroupId = id,
                UserId = currentUserId,
                Status = GroupMemberStatus.Pending
            };

            _context.GroupMembers.Add(newMember);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Cererea a fost trimisă către moderator." });
        }

        // GET: api/Groups/{id}/requests
        [HttpGet("{id}/requests")]
        public async Task<IActionResult> GetPendingRequests(int id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var group = await _context.Groups.FindAsync(id);

            if (group == null) return NotFound();
            if (group.OwnerId != currentUserId) return StatusCode(403, "Nu ești moderator.");

            var requests = await _context.GroupMembers
                .Where(gm => gm.GroupId == id && gm.Status == GroupMemberStatus.Pending)
                .Include(gm => gm.User)
                .Select(gm => new GroupRequestDto
                {
                    UserId = gm.UserId,
                    Username = gm.User.UserName,
                    ProfilePictureUrl = gm.User.ProfilePictureUrl
                })
                .ToListAsync();

            return Ok(requests);
        }

        // PUT: api/Groups/{id}/accept/{userId}
        [HttpPut("{id}/accept/{userId}")]
        public async Task<IActionResult> AcceptMember(int id, string userId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var group = await _context.Groups.FindAsync(id);

            if (group == null) return NotFound();
            if (group.OwnerId != currentUserId) return StatusCode(403, "Nu ești moderator.");

            var member = await _context.GroupMembers.FindAsync(id, userId);
            if (member == null) return NotFound("Cererea nu există.");

            member.Status = GroupMemberStatus.Accepted;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Utilizator acceptat în grup." });
        }

        // DELETE: api/Groups/{id}/members/{userId}
        [HttpDelete("{id}/members/{userId}")]
        public async Task<IActionResult> RemoveMember(int id, string userId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var group = await _context.Groups.FindAsync(id);
            if (group == null) return NotFound();

            var memberToRemove = await _context.GroupMembers.FindAsync(id, userId);
            if (memberToRemove == null) return NotFound("Utilizatorul nu este membru.");

            if (currentUserId != userId && currentUserId != group.OwnerId)
            {
                return StatusCode(403, "Nu ai permisiunea de a elimina acest membru.");
            }

            if (userId == group.OwnerId)
            {
                return BadRequest("Moderatorul nu poate părăsi grupul. Trebuie șters grupul.");
            }

            _context.GroupMembers.Remove(memberToRemove);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Utilizatorul a părăsit grupul." });
        }

        // GET: api/Groups/{id}/messages
        [HttpGet("{id}/messages")]
        public async Task<IActionResult> GetMessages(int id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var isMember = await _context.GroupMembers
                .AnyAsync(gm => gm.GroupId == id && gm.UserId == currentUserId && gm.Status == GroupMemberStatus.Accepted);

            if (!isMember) return StatusCode(403, "Trebuie să fii membru acceptat pentru a vedea mesajele.");

            var messages = await _context.GroupMessages
                .Where(m => m.GroupId == id)
                .Include(m => m.User)
                .OrderBy(m => m.CreatedAt)
                .Select(m => new GroupMessageDto
                {
                    Id = m.Id,
                    Content = m.Content,
                    CreatedAt = m.CreatedAt,
                    Username = m.User.UserName,
                    ProfilePictureUrl = m.User.ProfilePictureUrl,
                    IsMine = m.UserId == currentUserId
                })
                .ToListAsync();

            return Ok(messages);
        }

        // POST: api/Groups/{id}/messages
        // MODIFICAT PENTRU SIGNALR
        [HttpPost("{id}/messages")]
        public async Task<IActionResult> SendMessage(int id, [FromBody] SendMessageDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Content)) return BadRequest("Mesajul nu poate fi gol.");

            // --- VALIDARE AI: Mesaje Grup ---
            bool isSafe = await _aiService.IsContentSafeAsync(dto.Content);
            if (!isSafe)
            {
                return BadRequest("Mesajul tău conține termeni nepotriviți (insulte, hate speech).");
            }
            // --------------------------------

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = await _userManager.FindByIdAsync(currentUserId!); // Avem nevoie de user pentru poze/nume

            var isMember = await _context.GroupMembers
                .AnyAsync(gm => gm.GroupId == id && gm.UserId == currentUserId && gm.Status == GroupMemberStatus.Accepted);

            if (!isMember) return StatusCode(403, "Nu poți trimite mesaje dacă nu ești membru.");

            var message = new GroupMessage
            {
                GroupId = id,
                UserId = currentUserId,
                Content = dto.Content,
                CreatedAt = DateTime.UtcNow
            };

            _context.GroupMessages.Add(message);
            await _context.SaveChangesAsync();

            // --- REAL-TIME: SIGNALR BROADCAST ---
            // Trimitem mesajul prin socket către toți cei din grupul respectiv
            var messageDto = new GroupMessageDto
            {
                Id = message.Id,
                Content = message.Content,
                CreatedAt = message.CreatedAt,
                Username = currentUser.UserName,
                ProfilePictureUrl = currentUser.ProfilePictureUrl,
                IsMine = false // Frontend-ul va verifica dacă e mesajul propriu
            };

            // Notificăm grupul SignalR corespunzător ID-ului grupului din baza de date
            await _hubContext.Clients.Group(id.ToString()).SendAsync("ReceiveGroupMessage", messageDto);

            return Ok(new { message = "Mesaj trimis." });
        }

        // DELETE: api/Groups/{groupId}/messages/{messageId}
        [HttpDelete("{groupId}/messages/{messageId}")]
        public async Task<IActionResult> DeleteMessage(int groupId, int messageId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var message = await _context.GroupMessages.FindAsync(messageId);

            if (message == null) return NotFound("Mesajul nu există.");

            bool isAdmin = User.IsInRole("Admin") || User.IsInRole("Administrator");

            if (message.UserId != currentUserId && !isAdmin) return StatusCode(403, "Poți șterge doar propriile mesaje.");

            _context.GroupMessages.Remove(message);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Mesaj șters." });
        }
    }
}