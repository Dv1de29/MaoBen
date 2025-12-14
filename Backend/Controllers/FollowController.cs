using Backend.Data;
using Backend.DTOs;
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
    [Authorize] // Doar userii logați pot folosi aceste funcții
    public class FollowController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public FollowController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // 1. POST: api/Follow/{targetUserId}
        // Trimite o cerere de urmărire sau dă follow direct (în funcție de Privacy)
        [HttpPost("{targetUserId}")]
        public async Task<IActionResult> FollowUser(string targetUserId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (currentUserId == targetUserId)
                return BadRequest("Nu te poți urmări singur.");

            // Verificăm dacă userul țintă există
            var targetUser = await _userManager.FindByIdAsync(targetUserId);
            if (targetUser == null) return NotFound("Utilizatorul nu a fost găsit.");

            // Verificăm dacă există deja o relație (fie ea Pending sau Accepted)
            var existingFollow = await _context.UserFollows
                .FindAsync(currentUserId, targetUserId);

            if (existingFollow != null)
            {
                if (existingFollow.Status == FollowStatus.Pending)
                    return BadRequest("Ai trimis deja o cerere către acest utilizator.");

                return BadRequest("Urmărești deja acest utilizator.");
            }

            // --- LOGICA DE PRIVACY ACTUALIZATĂ ---
            // Dacă Privacy == true, înseamnă că e cont privat -> Pending
            // Dacă Privacy == false, înseamnă că e public -> Accepted
            var status = targetUser.Privacy ? FollowStatus.Pending : FollowStatus.Accepted;

            var newFollow = new UserFollow
            {
                SourceUserId = currentUserId!,
                TargetUserId = targetUserId,
                Status = status,
                CreatedAt = DateTime.UtcNow
            };

            _context.UserFollows.Add(newFollow);
            await _context.SaveChangesAsync();

            if (status == FollowStatus.Pending)
                return Ok(new { message = "Cont privat: Cererea a fost trimisă și așteaptă aprobare.", status = "Pending" });

            return Ok(new { message = "Succes: Acum urmărești acest utilizator.", status = "Accepted" });
        }

        // 2. DELETE: api/Follow/unfollow/{targetUserId}
        // Șterge un follow sau o cerere trimisă
        [HttpDelete("unfollow/{targetUserId}")]
        public async Task<IActionResult> UnfollowUser(string targetUserId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var followRelation = await _context.UserFollows
                .FindAsync(currentUserId, targetUserId);

            if (followRelation == null) return NotFound("Nu urmărești acest utilizator.");

            _context.UserFollows.Remove(followRelation);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Unfollow realizat cu succes." });
        }

        // --- ZONA PENTRU CEI CU CONT PRIVAT (Gestionare cereri primite) ---

        // 3. GET: api/Follow/requests
        // Îmi arată cine vrea să mă urmărească (Doar cererile Pending către mine)
        [HttpGet("requests")]
        public async Task<IActionResult> GetPendingRequests()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var requests = await _context.UserFollows
                .Where(f => f.TargetUserId == currentUserId && f.Status == FollowStatus.Pending)
                .Include(f => f.SourceUser) // Join ca să luăm datele celui care a cerut
                .Select(f => new FollowRequestDTO
                {
                    RequestId = f.SourceUserId,
                    Username = f.SourceUser.UserName!,
                    ProfilePictureUrl = f.SourceUser.ProfilePictureUrl
                })
                .ToListAsync();

            return Ok(requests);
        }

        // 4. PUT: api/Follow/accept/{sourceUserId}
        // Accept cererea lui X de a mă urmări
        [HttpPut("accept/{sourceUserId}")]
        public async Task<IActionResult> AcceptRequest(string sourceUserId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Căutăm relația unde EU sunt ținta (Target) și EL este sursa (Source)
            var relation = await _context.UserFollows
                .FirstOrDefaultAsync(f => f.SourceUserId == sourceUserId && f.TargetUserId == currentUserId);

            if (relation == null) return NotFound("Cererea nu există.");

            if (relation.Status == FollowStatus.Accepted)
                return BadRequest("Cererea este deja acceptată.");

            // Modificăm statusul
            relation.Status = FollowStatus.Accepted;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Cerere acceptată. Utilizatorul te urmărește acum." });
        }

        // 5. DELETE: api/Follow/decline/{sourceUserId}
        // Resping cererea lui X
        [HttpDelete("decline/{sourceUserId}")]
        public async Task<IActionResult> DeclineRequest(string sourceUserId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var relation = await _context.UserFollows
                .FirstOrDefaultAsync(f => f.SourceUserId == sourceUserId && f.TargetUserId == currentUserId);

            if (relation == null) return NotFound("Cererea nu există.");

            // O ștergem din bază
            _context.UserFollows.Remove(relation);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Cererea a fost respinsă." });
        }

        // 6. GET: api/Follow/status/{targetUserId}
        // Verifică statusul relației dintre mine și un alt user (pentru a ști ce buton să afișez pe Frontend)
        [HttpGet("status/{targetUserId}")]
        public async Task<IActionResult> GetFollowStatus(string targetUserId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (currentUserId == targetUserId) return Ok(new { status = "Self" });

            var relation = await _context.UserFollows
               .FindAsync(currentUserId, targetUserId);

            if (relation == null) return Ok(new { status = "None" }); // Nu îl urmăresc

            // Returnează "Pending" sau "Accepted"
            return Ok(new { status = relation.Status.ToString() });
        }
    }
}