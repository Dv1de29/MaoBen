using Backend.Data;
using Backend.DTOs.FollowController;
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

        [HttpPost("{targetUsername}")]
        public async Task<IActionResult> FollowUser(string targetUsername)
        {
            var currentUserIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var currentUser = await _userManager.FindByIdAsync(currentUserIdString!);
            if (currentUser == null) return BadRequest(new { error = "Error identifying the current user." });

            var targetUser = await _userManager.FindByNameAsync(targetUsername);
            if (targetUser == null) return NotFound(new { error = "User not found." });

            if (currentUser.Id == targetUser.Id)
                return BadRequest(new { error = "You cannot follow yourself." });

            var existingFollow = await _context.UserFollows
                .FindAsync(currentUser.Id, targetUser.Id);

            if (existingFollow != null)
            {
                if (existingFollow.Status == FollowStatus.Pending)
                    return BadRequest(new { error = $"You have already sent a follow request to {targetUsername}." });

                return BadRequest(new { error = $"You are already following {targetUsername}." });
            }

            var status = targetUser.IsPrivate ? FollowStatus.Pending : FollowStatus.Accepted;

            var newFollow = new UserFollow
            {
                SourceUserId = currentUser.Id,
                TargetUserId = targetUser.Id,
                Status = status,
                CreatedAt = DateTime.UtcNow
            };

            _context.UserFollows.Add(newFollow);

            if (status == FollowStatus.Accepted)
            {
                currentUser.FollowingCount++;
                targetUser.FollowersCount++;
            }

            await _context.SaveChangesAsync();

            if (status == FollowStatus.Pending)
                return Ok(new { message = $"Follow request sent to {targetUsername}.", status = "Pending" });

            return Ok(new { message = $"You are now following {targetUsername}.", status = "Accepted" });
        }

        [HttpDelete("unfollow/{targetUsername}")]
        public async Task<IActionResult> UnfollowUser(string targetUsername)
        {
            var currentUserIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var currentUser = await _userManager.FindByIdAsync(currentUserIdString!);
            if (currentUser == null) return BadRequest(new { error = "Current user error." });

            var targetUser = await _userManager.FindByNameAsync(targetUsername);
            if (targetUser == null) return NotFound(new { error = "The user does not exist." });

            var followRelation = await _context.UserFollows
                .FindAsync(currentUser.Id, targetUser.Id);

            if (followRelation == null) return NotFound(new { error = $"You are not following {targetUsername}." });

            if (followRelation.Status == FollowStatus.Accepted)
            {
                currentUser.FollowingCount--;
                targetUser.FollowersCount--;
            }

            _context.UserFollows.Remove(followRelation);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Successfully unfollowed {targetUsername}." });
        }

        [HttpGet("requests")]
        public async Task<IActionResult> GetPendingRequests()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var requests = await _context.UserFollows
                .Where(f => f.TargetUserId == currentUserId && f.Status == FollowStatus.Pending)
                .Include(f => f.SourceUser)
                .Select(f => new PendingRequestResponseDTO
                {
                    Username = f.SourceUser.UserName!,
                    ProfilePictureUrl = f.SourceUser.ProfilePictureUrl
                })
                .ToListAsync();

            return Ok(requests);
        }

        
        [HttpPut("accept/{sourceUsername}")]
        public async Task<IActionResult> AcceptRequest(string sourceUsername)
        {
            var currentUserIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = await _userManager.FindByIdAsync(currentUserIdString!);

            if (currentUser == null) return BadRequest(new { error = "Error identifying the current user." });

            var sourceUser = await _userManager.FindByNameAsync(sourceUsername);
            if (sourceUser == null) return NotFound(new { error = "Source user does not exist." });

            var relation = await _context.UserFollows
                .FirstOrDefaultAsync(f => f.SourceUserId == sourceUser.Id && f.TargetUserId == currentUser.Id);

            if (relation == null) return NotFound(new { error = "Follow request does not exist." });

            if (relation.Status == FollowStatus.Accepted)
                return BadRequest(new { error = "Request has already been accepted." });

            relation.Status = FollowStatus.Accepted;

            currentUser.FollowersCount++;
            sourceUser.FollowingCount++;

            await _context.SaveChangesAsync();

            return Ok(new { message = $"Follow request from {sourceUsername} has been accepted." });
        }

        [HttpDelete("decline/{sourceUsername}")]
        public async Task<IActionResult> DeclineRequest(string sourceUsername)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var sourceUser = await _userManager.FindByNameAsync(sourceUsername);
            if (sourceUser == null) return NotFound(new { error = "User does not exist." });

            var relation = await _context.UserFollows
                .FirstOrDefaultAsync(f => f.SourceUserId == sourceUser.Id && f.TargetUserId == currentUserId);

            if (relation == null) return NotFound(new { error = "Follow request does not exist." });

            _context.UserFollows.Remove(relation); 
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Follow request from {sourceUsername} has been declined." });
        }

        [HttpGet("status/{targetUsername}")]
        public async Task<IActionResult> GetFollowStatus(string targetUsername)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var targetUser = await _userManager.FindByNameAsync(targetUsername);
            if (targetUser == null) return NotFound(new { error = "User does not exist." });

            if (currentUserId == targetUser.Id) return Ok(new { status = "Self" });

            var relation = await _context.UserFollows
               .FindAsync(currentUserId, targetUser.Id);

            if (relation == null) return Ok(new { status = "None" });

            return Ok(new { status = relation.Status.ToString() });
        }
    }
}