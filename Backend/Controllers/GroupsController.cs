using Backend.Data;
using Backend.DTOs;
using Backend.DTOs.GroupController;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Doar utilizatorii logați au acces la grupuri [cite: 19]
    public class GroupsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public GroupsController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ---------------------------------------------------------
        // ZONA 1: GESTIUNE GRUPURI (CRUD basic)
        // ---------------------------------------------------------

        // POST: api/Groups (Creare Grup)
        [HttpPost]
        public async Task<IActionResult> CreateGroup([FromBody] CreateGroupDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Description))
                return BadRequest("Numele și descrierea sunt obligatorii.");

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var group = new Group
            {
                Name = dto.Name,
                Description = dto.Description,
                OwnerId = currentUserId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Groups.Add(group);
            await _context.SaveChangesAsync(); // Salvăm ca să avem ID-ul grupului

            // Moderatorul devine automat membru ACCEPTAT
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

        // GET: api/Groups (Listare toate grupurile)
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
                    // Verificăm dacă userul curent e deja membru (ca să știm ce buton afișăm în UI)
                    IsUserMember = _context.GroupMembers.Any(gm => gm.GroupId == g.Id && gm.UserId == currentUserId)
                })
                .ToListAsync();

            return Ok(groups);
        }

        // DELETE: api/Groups/{id} (Ștergere Grup - Doar Moderatorul) [cite: 23]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGroup(int id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var group = await _context.Groups.FindAsync(id);

            if (group == null) return NotFound("Grupul nu există.");

            if (group.OwnerId != currentUserId)
                return StatusCode(403, "Doar moderatorul poate șterge grupul.");

            _context.Groups.Remove(group);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Grupul a fost șters." });
        }

        // ---------------------------------------------------------
        // ZONA 2: MEMBRI (Join, Accept, Kick, Leave)
        // ---------------------------------------------------------

        // POST: api/Groups/{id}/join (Cerere de alăturare) 
        [HttpPost("{id}/join")]
        public async Task<IActionResult> JoinGroup(int id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Verificăm dacă grupul există
            var groupExists = await _context.Groups.AnyAsync(g => g.Id == id);
            if (!groupExists) return NotFound("Grupul nu există.");

            // Verificăm dacă e deja membru
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
                Status = GroupMemberStatus.Pending // Default e Pending 
            };

            _context.GroupMembers.Add(newMember);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Cererea a fost trimisă către moderator." });
        }

        // GET: api/Groups/{id}/requests (Moderatorul vede cererile)
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

        // PUT: api/Groups/{id}/accept/{userId} (Moderatorul acceptă) 
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

        // DELETE: api/Groups/{id}/members/{userId} (Leave sau Kick) [cite: 23]
        [HttpDelete("{id}/members/{userId}")]
        public async Task<IActionResult> RemoveMember(int id, string userId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var group = await _context.Groups.FindAsync(id);
            if (group == null) return NotFound();

            var memberToRemove = await _context.GroupMembers.FindAsync(id, userId);
            if (memberToRemove == null) return NotFound("Utilizatorul nu este membru.");

            // LOGICA DE PERMISIUNI:
            // 1. Userul iese singur (Leave)
            // 2. Moderatorul dă Kick
            if (currentUserId != userId && currentUserId != group.OwnerId)
            {
                return StatusCode(403, "Nu ai permisiunea de a elimina acest membru.");
            }

            // Moderatorul nu poate ieși din grup decât dacă șterge grupul (opțional, dar logic)
            if (userId == group.OwnerId)
            {
                return BadRequest("Moderatorul nu poate părăsi grupul. Trebuie șters grupul.");
            }

            _context.GroupMembers.Remove(memberToRemove);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Utilizatorul a părăsit grupul." });
        }


        // ---------------------------------------------------------
        // ZONA 3: MESAJE (Doar pentru cei acceptați)
        // ---------------------------------------------------------

        // GET: api/Groups/{id}/messages
        [HttpGet("{id}/messages")]
        public async Task<IActionResult> GetMessages(int id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // VERIFICARE CRITICĂ: Ești membru ACCEPTAT?
            var isMember = await _context.GroupMembers
                .AnyAsync(gm => gm.GroupId == id && gm.UserId == currentUserId && gm.Status == GroupMemberStatus.Accepted);

            if (!isMember) return StatusCode(403, "Trebuie să fii membru acceptat pentru a vedea mesajele.");

            var messages = await _context.GroupMessages
                .Where(m => m.GroupId == id)
                .Include(m => m.User)
                .OrderBy(m => m.CreatedAt) // Cronologic
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
        [HttpPost("{id}/messages")]
        public async Task<IActionResult> SendMessage(int id, [FromBody] SendMessageDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Content)) return BadRequest("Mesajul nu poate fi gol.");

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

           // VERIFICARE: Ești membru ACCEPTAT? 
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

            return Ok(new { message = "Mesaj trimis." });
        }

        // DELETE: api/Groups/{groupId}/messages/{messageId} (Ștergere mesaj propriu) 
        [HttpDelete("{groupId}/messages/{messageId}")]
        public async Task<IActionResult> DeleteMessage(int groupId, int messageId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var message = await _context.GroupMessages.FindAsync(messageId);

            if (message == null) return NotFound("Mesajul nu există.");
            if (message.UserId != currentUserId) return StatusCode(403, "Poți șterge doar propriile mesaje.");

            _context.GroupMessages.Remove(message);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Mesaj șters." });
        }
    }
}