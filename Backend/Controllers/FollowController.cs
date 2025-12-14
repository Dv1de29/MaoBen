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
    [Authorize]
    public class FollowController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public FollowController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // 1. POST: api/Follow/{targetUsername}
        [HttpPost("{targetUsername}")]
        public async Task<IActionResult> FollowUser(string targetUsername)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Pas 1: Găsim User-ul țintă după NUME
            var targetUser = await _userManager.FindByNameAsync(targetUsername);
            if (targetUser == null) return NotFound("Utilizatorul nu a fost găsit.");

            // Pas 2: Verificăm să nu te urmărești singur
            if (currentUserId == targetUser.Id)
                return BadRequest("Nu te poți urmări singur.");

            // Pas 3: Verificăm relația existentă folosind ID-urile reale
            var existingFollow = await _context.UserFollows
                .FindAsync(currentUserId, targetUser.Id);

            if (existingFollow != null)
            {
                if (existingFollow.Status == FollowStatus.Pending)
                    return BadRequest($"Ai trimis deja o cerere către {targetUsername}.");

                return BadRequest($"Urmărești deja utilizatorul {targetUsername}.");
            }

            // Pas 4: Logica de Privacy
            var status = targetUser.Privacy ? FollowStatus.Pending : FollowStatus.Accepted;

            var newFollow = new UserFollow
            {
                SourceUserId = currentUserId!,
                TargetUserId = targetUser.Id, // Salvăm ID-ul în baza de date
                Status = status,
                CreatedAt = DateTime.UtcNow
            };

            _context.UserFollows.Add(newFollow);
            await _context.SaveChangesAsync();

            if (status == FollowStatus.Pending)
                return Ok(new { message = $"Cererea către {targetUsername} a fost trimisă.", status = "Pending" });

            return Ok(new { message = $"Acum îl urmărești pe {targetUsername}.", status = "Accepted" });
        }

        // 2. DELETE: api/Follow/unfollow/{targetUsername}
        [HttpDelete("unfollow/{targetUsername}")]
        public async Task<IActionResult> UnfollowUser(string targetUsername)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Găsim ID-ul userului pe baza numelui
            var targetUser = await _userManager.FindByNameAsync(targetUsername);
            if (targetUser == null) return NotFound("Utilizatorul nu există.");

            var followRelation = await _context.UserFollows
                .FindAsync(currentUserId, targetUser.Id);

            if (followRelation == null) return NotFound($"Nu îl urmărești pe {targetUsername}.");

            _context.UserFollows.Remove(followRelation);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Unfollow realizat pentru {targetUsername}." });
        }

        // 3. GET: api/Follow/requests
        // Aici nu se schimbă URL-ul, dar returnăm Username-ul celor care vor să ne urmărească
        [HttpGet("requests")]
        public async Task<IActionResult> GetPendingRequests()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var requests = await _context.UserFollows
                .Where(f => f.TargetUserId == currentUserId && f.Status == FollowStatus.Pending)
                .Include(f => f.SourceUser)
                .Select(f => new FollowRequestDTO
                {
                    // Returnăm Username ca să fie ușor de afișat pe Frontend
                    RequestId = f.SourceUser.UserName!,
                    Username = f.SourceUser.UserName!,
                    ProfilePictureUrl = f.SourceUser.ProfilePictureUrl
                })
                .ToListAsync();

            return Ok(requests);
        }

        // 4. PUT: api/Follow/accept/{sourceUsername}
        [HttpPut("accept/{sourceUsername}")]
        public async Task<IActionResult> AcceptRequest(string sourceUsername)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Găsim cine a trimis cererea
            var sourceUser = await _userManager.FindByNameAsync(sourceUsername);
            if (sourceUser == null) return NotFound("Utilizatorul sursă nu există.");

            var relation = await _context.UserFollows
                .FirstOrDefaultAsync(f => f.SourceUserId == sourceUser.Id && f.TargetUserId == currentUserId);

            if (relation == null) return NotFound("Cererea nu există.");

            if (relation.Status == FollowStatus.Accepted)
                return BadRequest("Cererea este deja acceptată.");

            relation.Status = FollowStatus.Accepted;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Cererea lui {sourceUsername} a fost acceptată." });
        }

        // 5. DELETE: api/Follow/decline/{sourceUsername}
        [HttpDelete("decline/{sourceUsername}")]
        public async Task<IActionResult> DeclineRequest(string sourceUsername)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var sourceUser = await _userManager.FindByNameAsync(sourceUsername);
            if (sourceUser == null) return NotFound("Utilizatorul nu există.");

            var relation = await _context.UserFollows
                .FirstOrDefaultAsync(f => f.SourceUserId == sourceUser.Id && f.TargetUserId == currentUserId);

            if (relation == null) return NotFound("Cererea nu există.");

            _context.UserFollows.Remove(relation);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Cererea lui {sourceUsername} a fost respinsă." });
        }

        // 6. GET: api/Follow/status/{targetUsername}
        [HttpGet("status/{targetUsername}")]
        public async Task<IActionResult> GetFollowStatus(string targetUsername)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Căutăm userul țintă
            var targetUser = await _userManager.FindByNameAsync(targetUsername);
            if (targetUser == null) return NotFound("Utilizatorul nu există.");

            if (currentUserId == targetUser.Id) return Ok(new { status = "Self" });

            // Verificăm relația folosind ID-urile
            var relation = await _context.UserFollows
               .FindAsync(currentUserId, targetUser.Id);

            if (relation == null) return Ok(new { status = "None" });

            return Ok(new { status = relation.Status.ToString() });
        }
    }
}