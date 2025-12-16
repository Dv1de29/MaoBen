using Backend.Data;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class LikesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public LikesController(AppDbContext context)
        {
            _context = context;
        }

        // POST: api/Likes/toggle/{postId}
        [HttpPost("toggle/{postId}")]
        public async Task<IActionResult> ToggleLike(int postId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var post = await _context.Posts.FindAsync(postId);
            if (post == null) return NotFound("Postarea nu a fost găsită.");

            // Verificăm dacă există deja un like de la acest user
            var existingLike = await _context.PostLikes
                .FirstOrDefaultAsync(pl => pl.PostId == postId && pl.UserId == userId);

            bool isLiked;

            if (existingLike != null)
            {
                // -- UNLIKE --
                _context.PostLikes.Remove(existingLike);
                post.Nr_likes--; // Scădem contorul
                if (post.Nr_likes < 0) post.Nr_likes = 0; // Siguranță
                isLiked = false;
            }
            else
            {
                // -- LIKE --
                var newLike = new PostLike
                {
                    PostId = postId,
                    UserId = userId
                };
                _context.PostLikes.Add(newLike);
                post.Nr_likes++; // Creștem contorul
                isLiked = true;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = isLiked ? "Like adăugat." : "Like șters.",
                newLikeCount = post.Nr_likes,
                isLikedByCurrentUser = isLiked
            });
        }

        // GET: api/Likes/check/{postId}
        // Verifică dacă userul curent a dat like la o postare (util pentru iconița de inimă colorată)
        [HttpGet("check/{postId}")]
        public async Task<IActionResult> CheckIfLiked(int postId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var isLiked = await _context.PostLikes
                .AnyAsync(pl => pl.PostId == postId && pl.UserId == userId);

            return Ok(new { isLiked });
        }
    }
}