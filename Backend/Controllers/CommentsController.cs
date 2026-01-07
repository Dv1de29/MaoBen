using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Backend.Services; // Import pentru AI Service
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Garanteaza ca utilizatorul este logat/inregistrat
    public class CommentsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IAiContentService _aiService; // Injectare serviciu AI

        public CommentsController(AppDbContext context, IAiContentService aiService)
        {
            _context = context;
            _aiService = aiService;
        }

        [HttpPost]
        public async Task<IActionResult> AddComment([FromBody] AddCommentDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Content))
            {
                return BadRequest("Comentariul nu poate fi gol.");
            }

            if (dto.Content.Length > 500)
            {
                return BadRequest("Comentariul nu poate depași 500 de caractere.");
            }

            // --- VALIDARE AI: Google Gemini ---
            bool isSafe = await _aiService.IsContentSafeAsync(dto.Content);
            if (!isSafe)
            {
                return BadRequest("Comentariul tău conține termeni nepotriviți (insulte, hate speech, etc.). Te rugăm să reformulezi.");
            }
            // ----------------------------------

            // --- VALIDARE 1: User Authentication ---
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized("Nu ești autentificat.");
            }

            // --- VALIDARE 2: Post Exists ---
            var post = await _context.Posts
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == dto.PostId);

            if (post == null)
            {
                return NotFound("Postarea nu a fost găsită.");
            }

            // --- VALIDARE 3: Visibility Check (Can user view/comment on post?) ---
            bool canComment = false;

            // Case A: Own Post - Always allowed
            if (post.User.Id == currentUserId)
            {
                canComment = true;
            }
            // Case B: Public Profile - Anyone logged in can comment
            else if (!post.User.Privacy)
            {
                canComment = true;
            }
            // Case C: Private Profile - Only accepted followers can comment
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
                return StatusCode(403, "Nu ai permisiunea de a comenta la această postare. Profilul utilizatorului este privat.");
            }

            // --- ACTION: Add Comment ---
            var comment = new Comment
            {
                PostId = dto.PostId,
                UserId = currentUserId,
                Content = dto.Content.Trim(),
                CreatedAt = DateTime.UtcNow,
            };

            _context.Comments.Add(comment);

            // --- Update Post Comment Count ---
            post.Nr_Comms++;

            await _context.SaveChangesAsync();

            // Returnăm detalii complete pentru UI (inclusiv poza și numele celui care a comentat)
            var userDetails = await _context.Users
                .Where(u => u.Id == currentUserId)
                .Select(u => new
                {
                    u.UserName,
                    u.ProfilePictureUrl
                })
                .FirstOrDefaultAsync();

            return CreatedAtAction(nameof(AddComment), new { id = comment.Id }, new
            {
                id = comment.Id,
                postId = comment.PostId,
                userId = comment.UserId,
                content = comment.Content,
                createdAt = comment.CreatedAt,
                username = userDetails?.UserName,
                profilePictureUrl = userDetails?.ProfilePictureUrl,
                message = "Comentariu adăugat cu succes!"
            });
        }

        // GET: api/Comments/{postId}
        [HttpGet("{postId}")]
        public async Task<IActionResult> GetComments(int postId)
        {
            var comments = await _context.Comments
                .Include(c => c.User) // Aducem datele userului pentru a afișa poza și numele
                .Where(c => c.PostId == postId)
                .OrderByDescending(c => c.CreatedAt) // Cele mai noi primele
                .Select(c => new
                {
                    c.Id,
                    c.Content,
                    c.CreatedAt,
                    Username = c.User.UserName,
                    ProfilePictureUrl = c.User.ProfilePictureUrl,
                    UserId = c.UserId // Util pentru a ști dacă e comentariul meu
                })
                .ToListAsync();

            return Ok(comments);
        }

        // DELETE: api/Comments/{commentId}
        [HttpDelete("{commentId}")]
        public async Task<IActionResult> DeleteComment(int commentId)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var comment = await _context.Comments
                .Include(c => c.Post) // Avem nevoie de post pt a vedea cine e proprietarul postării
                .FirstOrDefaultAsync(c => c.Id == commentId);

            if (comment == null) return NotFound("Comentariul nu există.");

            // Cine are voie să șteargă?
            // 1. Autorul comentariului
            // 2. Proprietarul postării
            // 3. Adminul

            bool isAuthor = comment.UserId == currentUserId;
            bool isPostOwner = comment.Post.OwnerID == currentUserId;
            bool isAdmin = User.IsInRole("Admin") || User.IsInRole("Administrator");

            if (!isAuthor && !isPostOwner && !isAdmin)
            {
                return StatusCode(403, "Nu ai dreptul să ștergi acest comentariu.");
            }

            _context.Comments.Remove(comment);

            // Scădem contorul de comentarii din postare
            if (comment.Post.Nr_Comms > 0)
            {
                comment.Post.Nr_Comms--;
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Comentariul a fost șters." });
        }
    }
}