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

        [HttpPost("toggle/{postId}")]
        public async Task<IActionResult> ToggleLike(int postId, [FromQuery] bool likeState)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized(new { error = "User must be logged in to like posts." });

            var post = await _context.Posts.FindAsync(postId);
            if (post == null) return NotFound(new { error = "Post not found." });

            var existingLike = await _context.PostLikes
                .FirstOrDefaultAsync(pl => pl.PostId == postId && pl.UserId == userId);

            bool isLiked = (existingLike != null);

            if (likeState)
            {
                if (existingLike == null)
                {
                    var newLike = new PostLike
                    {
                        PostId = postId,
                        UserId = userId
                    };
                    _context.PostLikes.Add(newLike);
                    post.Nr_likes++;
                    isLiked = true;
                }
            }
            else
            {
                if (existingLike != null)
                {
                    _context.PostLikes.Remove(existingLike);
                    post.Nr_likes--;

                    if (post.Nr_likes < 0) post.Nr_likes = 0;
                    isLiked = false;
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = isLiked ? "Like added successfully." : "Like removed successfully.",
                newLikeCount = post.Nr_likes,
                isLikedByCurrentUser = isLiked
            });
        }

        [HttpGet("check/{postId}")]
        public async Task<IActionResult> CheckIfLiked(int postId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized(new { error = "Authentication required." });

            var isLiked = await _context.PostLikes
                .AnyAsync(pl => pl.PostId == postId && pl.UserId == userId);
            return Ok(new { isLiked });
        }
    }
}