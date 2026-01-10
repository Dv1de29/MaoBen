using Backend.Data;
using Backend.DTOs.CommentsController;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CommentsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IAiContentService _aiService;

        public CommentsController(AppDbContext context, IAiContentService aiService)
        {
            _context = context;
            _aiService = aiService;
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> AddComment([FromBody] AddCommentDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Content))
            {
                return BadRequest("Comment content cannot be empty.");
            }
            if (dto.Content.Length > 500)
            {
                return BadRequest("Comment content cannot exceed 500 characters.");
            }

            bool isSafe = await _aiService.IsContentSafeAsync(dto.Content);
            if (!isSafe)
            {
                return BadRequest("Your comment contains inappropriate terms (insults, hate speech, etc.). Please reformulate.");
            }

            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var currentUserRole = User.FindFirstValue(ClaimTypes.Role);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized("You are not authenticated.");
            }

            var post = await _context.Posts
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == dto.PostId);

            if (post == null)
            {
                return NotFound("The post was not found.");
            }

            bool canComment = false;

            if (post.User.Id == currentUserId)
            {
                canComment = true;
            }
            else if (!post.User.IsPrivate)
            {
                canComment = true;
            }
            //Admins can comment to any posts
            else if (currentUserRole == "Admin")
            {
                canComment = true;
            }    
            else
            {
                var isAcceptedFollower = await _context.UserFollows
                    .AnyAsync(f => f.SourceUserId == currentUserId &&
                                   f.TargetUserId == post.User.Id &&
                                   f.Status == FollowStatus.Accepted);
                if (isAcceptedFollower)
                {
                    canComment = true;
                }
            }
            if (!canComment)
            {
                return StatusCode(403, "You do not have permission to comment on this post. The user profile is private.");
            }

            var comment = new Comment
            {
                PostId = dto.PostId,
                UserId = currentUserId,
                Content = dto.Content.Trim(),
                CreatedAt = DateTime.UtcNow,
            };

            _context.Comments.Add(comment);

            post.Nr_Comms++;

            await _context.SaveChangesAsync();

            var userDetails = await _context.Users
                .Where(u => u.Id == currentUserId)
                .Select(u => new
                {
                    u.UserName,
                    u.ProfilePictureUrl
                })
                .FirstOrDefaultAsync();
            
            //Maybe add a response DTO in the future
            return CreatedAtAction(nameof(AddComment), new { id = comment.Id }, new
            {
                id = comment.Id,
                postId = comment.PostId,
                userId = comment.UserId,
                content = comment.Content,
                createdAt = comment.CreatedAt,
                username = userDetails?.UserName,
                profilePictureUrl = userDetails?.ProfilePictureUrl,
                message = "Comment added successfully!"
            });
        }

        [HttpGet("{postId}")]
        public async Task<IActionResult> GetComments(int postId)
        {
            var comments = await _context.Comments
                .Include(c => c.User)
                .Where(c => c.PostId == postId)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new
                {
                    c.Id,
                    c.Content,
                    c.CreatedAt,
                    Username = c.User.UserName,
                    ProfilePictureUrl = c.User.ProfilePictureUrl,
                    UserId = c.UserId
                })
                .ToListAsync();

            return Ok(comments);
        }
        
        
        [Authorize]
        [HttpDelete("{commentId}")]
        public async Task<IActionResult> DeleteComment(int commentId)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var comment = await _context.Comments
                .Include(c => c.Post)
                .FirstOrDefaultAsync(c => c.Id == commentId);

            if (comment == null) return NotFound("The comment does not exist.");

            bool isAuthor = comment.UserId == currentUserId;
            bool isPostOwner = comment.Post.OwnerID == currentUserId;
            bool isAdmin = User.IsInRole("Admin");

            if (!isAuthor && !isPostOwner && !isAdmin)
            {
                return StatusCode(403, "You do not have the right to delete this comment.");
            }

            _context.Comments.Remove(comment);

            if (comment.Post.Nr_Comms > 0)
            {
                comment.Post.Nr_Comms--;
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "The comment has been deleted." });
        }
    }
}