using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Backend.Services; // Import pentru AI Service
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System.Security.Claims;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PostsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly int MaxPostsLimit = 50;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IAiContentService _aiService; // Injectare serviciu AI

        public PostsController(AppDbContext context, IWebHostEnvironment webHostEnvironment, IAiContentService aiService)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _aiService = aiService;
        }

        private string GenerateRandomId()
        {
            Random random = new Random();
            long randomNumber = random.Next(1000000000) + 1000000000L; // Asigură 10 cifre
            return randomNumber.ToString();
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPostID(int id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var post = await _context.Posts
                .Include(p => p.User)
                .Select(p => new GetPostsWithUser
                {
                    Id = p.Id,
                    OwnerID = p.OwnerID,
                    Nr_likes = p.Nr_likes,
                    Nr_Comms = p.Nr_Comms,
                    Image_path = p.Image_path,
                    Description = p.Description,
                    Created = p.Created,
                    Username = p.User.UserName,
                    user_image_path = p.User.ProfilePictureUrl,
                    Has_liked = _context.PostLikes.Any(pl => pl.PostId == p.Id && pl.UserId == currentUserId),
                })
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null)
            {
                return NotFound();
            }

            return Ok(post);
        }

        [Authorize]
        [HttpGet("my_posts")]
        public async Task<IActionResult> GetMyPosts()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token.");
            }

            var myPosts = await _context.Posts
                .Include(p => p.User)
                .Where(p => p.OwnerID == userId)
                .OrderByDescending(p => p.Created)
                .Select(p => new GetPostsWithUser
                {
                    Id = p.Id,
                    OwnerID = p.OwnerID,
                    Nr_likes = p.Nr_likes,
                    Nr_Comms = p.Nr_Comms,
                    Image_path = p.Image_path,
                    Description = p.Description,
                    Created = p.Created,
                    Username = p.User.UserName,
                    user_image_path = p.User.ProfilePictureUrl,
                    Has_liked = _context.PostLikes.Any(pl => pl.PostId == p.Id && pl.UserId == userId),
                })
                .ToListAsync();

            return Ok(myPosts);
        }

        // --- FEED PERSONALIZAT ---
        [Authorize]
        [HttpGet("feed")]
        public async Task<IActionResult> GetFeed([FromQuery] int count = 20, [FromQuery] int skip = 0)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 1. Găsim ID-urile celor pe care îi urmărim
            var followingIds = await _context.UserFollows
                .Where(f => f.SourceUserId == currentUserId && f.Status == FollowStatus.Accepted)
                .Select(f => f.TargetUserId)
                .ToListAsync();

            // 2. Adăugăm și ID-ul nostru
            followingIds.Add(currentUserId!);

            // 3. Luăm postările
            var posts = await _context.Posts
                .Include(p => p.User)
                .Where(p => followingIds.Contains(p.OwnerID))
                .OrderByDescending(p => p.Created)
                .Skip(skip)
                .Take(count)
                .Select(p => new GetPostsWithUser
                {
                    Id = p.Id,
                    OwnerID = p.OwnerID,
                    Nr_likes = p.Nr_likes,
                    Nr_Comms = p.Nr_Comms,
                    Image_path = p.Image_path,
                    Description = p.Description,
                    Created = p.Created,
                    Username = p.User.UserName,
                    user_image_path = p.User.ProfilePictureUrl,
                    Has_liked = _context.PostLikes.Any(pl => pl.PostId == p.Id && pl.UserId == currentUserId),
                })
                .ToListAsync();

            return Ok(posts);
        }

        [HttpGet]
        public async Task<IActionResult> GetRecentPosts(
            [FromQuery] int count = 20,
            [FromQuery] int skip = 0
            )
        {
            int limit = Math.Min(count, MaxPostsLimit);
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var posts = await _context.Posts
                .AsNoTracking()
                .OrderByDescending(p => p.Created)
                .Skip(skip)
                .Take(limit)
                .Select(p => new GetPostsWithUser
                {
                    Id = p.Id,
                    OwnerID = p.OwnerID,
                    Nr_likes = p.Nr_likes,
                    Nr_Comms = p.Nr_Comms,
                    Image_path = p.Image_path,
                    Description = p.Description,
                    Created = p.Created,
                    Username = p.User.UserName,
                    user_image_path = p.User.ProfilePictureUrl,
                    Has_liked = _context.PostLikes.Any(pl => pl.PostId == p.Id && pl.UserId == currentUserId),
                })
                .ToListAsync();

            if (posts == null || !posts.Any())
            {
                return Ok(new List<GetPostsWithUser>());
            }

            return Ok(posts);
        }

        [HttpGet("ByOwner/{ownerUsername}")]
        public async Task<IActionResult> GetPostsByOwnerId(string ownerUsername)
        {
            if (string.IsNullOrEmpty(ownerUsername)) return BadRequest("Username cannot be empty.");

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var targetUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == ownerUsername);

            if (targetUser == null) return NotFound("User not found.");

            // --- LOGICA DE PRIVACY ---
            bool canView = false;
            if (!targetUser.Privacy) canView = true;
            else if (targetUser.Id == currentUserId) canView = true;
            else
            {
                var isFollowing = await _context.UserFollows
                    .AnyAsync(f => f.SourceUserId == currentUserId && f.TargetUserId == targetUser.Id && f.Status == FollowStatus.Accepted);
                if (isFollowing) canView = true;
            }

            if (!canView) return StatusCode(403, "Cont privat.");

            var posts = await _context.Posts
                .Include(p => p.User)
                .Where(p => p.User.UserName!.ToLower() == ownerUsername.ToLower())
                .OrderByDescending(p => p.Created)
                .Select(p => new GetPostsWithUser
                {
                    Id = p.Id,
                    OwnerID = p.OwnerID,
                    Nr_likes = p.Nr_likes,
                    Nr_Comms = p.Nr_Comms,
                    Image_path = p.Image_path,
                    Description = p.Description,
                    Created = p.Created,
                    Username = p.User.UserName,
                    user_image_path = p.User.ProfilePictureUrl,
                    Has_liked = _context.PostLikes.Any(pl => pl.PostId == p.Id && pl.UserId == currentUserId),
                })
                .ToListAsync();

            return Ok(posts);
        }

        [Authorize]
        [HttpPost("create_post")]
        [Consumes("multipart/form-data")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> CreatePost([FromForm] CreatePostDTO dto)
        {
            // --- VALIDARE AI: Google Gemini ---
            if (!string.IsNullOrWhiteSpace(dto.Description))
            {
                bool isSafe = await _aiService.IsContentSafeAsync(dto.Description);
                if (!isSafe)
                {
                    return BadRequest("Descrierea conține termeni nepotriviți. Te rugăm să reformulezi.");
                }
            }
            // ----------------------------------

            if (dto.Image == null || dto.Image.Length == 0)
            {
                return BadRequest("Image is required.");
            }

            if (!dto.Image.ContentType.StartsWith("image/") && !dto.Image.ContentType.StartsWith("video/"))
            {
                Console.WriteLine(dto.Image.ContentType);
                return BadRequest("File type is wrong and should be image/ or video/");
            }

            string uniqueId = this.GenerateRandomId();
            string extension = Path.GetExtension(dto.Image.FileName);
            string fileName = uniqueId + extension;

            string relativePath = $"/be_assets/img/posts/{fileName}";

            string targetFolder = Path.Combine(_webHostEnvironment.WebRootPath, "be_assets", "img", "posts");
            string physicalPath = Path.Combine(targetFolder, fileName);

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token.");
            }

            var newPost = new Posts
            {
                OwnerID = userId!,
                Image_path = relativePath,
                Description = dto.Description,
                Created = DateTime.UtcNow,
            };

            try
            {
                if (!Directory.Exists(targetFolder))
                {
                    Directory.CreateDirectory(targetFolder);
                }

                using (var stream = new FileStream(physicalPath, FileMode.Create))
                {
                    await dto.Image.CopyToAsync(stream);
                }

                _context.Posts.Add(newPost);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Post created!", postId = newPost.Id, imageUrl = relativePath });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    $"Eroare la salvarea postarii: {ex.Message}");
            }
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePost(int id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId)) return Unauthorized();

            var post = await _context.Posts.FindAsync(id);
            if (post == null)
            {
                return NotFound("Postarea nu a fost găsită.");
            }

            // 4. LOGICA DE PERMISIUNI (Admin sau Owner)
            bool isOwner = post.OwnerID == currentUserId;
            bool isAdmin = User.IsInRole("Admin") || User.IsInRole("Administrator");

            if (!isOwner && !isAdmin)
            {
                return StatusCode(403, "Nu ai permisiunea de a șterge această postare.");
            }

            // 5. Șterge și fișierul fizic
            try
            {
                if (!string.IsNullOrEmpty(post.Image_path))
                {
                    string relativePath = post.Image_path.TrimStart('/', '\\');
                    string absolutePath = Path.Combine(_webHostEnvironment.WebRootPath, relativePath);

                    if (System.IO.File.Exists(absolutePath))
                    {
                        System.IO.File.Delete(absolutePath);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Nu s-a putut șterge fișierul: {ex.Message}");
            }

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Postarea a fost ștersă cu succes." });
        }

        [Authorize]
        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdatePost(int id, [FromForm] EditPostDto dto)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId)) return Unauthorized();

            var post = await _context.Posts.FindAsync(id);
            if (post == null) return NotFound("Postarea nu a fost găsită.");

            // Doar proprietarul poate edita (Adminul doar sterge)
            if (post.OwnerID != currentUserId)
            {
                return StatusCode(403, "Nu ai dreptul să editezi această postare.");
            }

            if (dto.Description != null)
            {
                // --- VALIDARE AI PENTRU EDITARE ---
                bool isSafe = await _aiService.IsContentSafeAsync(dto.Description);
                if (!isSafe)
                {
                    return BadRequest("Descrierea conține termeni nepotriviți.");
                }
                // ----------------------------------
                post.Description = dto.Description;
            }

            if (dto.Image != null && dto.Image.Length > 0)
            {
                if (!dto.Image.ContentType.StartsWith("image/") && !dto.Image.ContentType.StartsWith("video/"))
                {
                    return BadRequest("Fișierul trebuie să fie o imagine.");
                }

                try
                {
                    // A. ȘTERGEM POZA VECHE
                    if (!string.IsNullOrEmpty(post.Image_path))
                    {
                        string oldRelativePath = post.Image_path.TrimStart('/', '\\');
                        string oldAbsolutePath = Path.Combine(_webHostEnvironment.WebRootPath, oldRelativePath);

                        if (System.IO.File.Exists(oldAbsolutePath))
                        {
                            System.IO.File.Delete(oldAbsolutePath);
                        }
                    }

                    // B. SALVĂM POZA NOUĂ
                    string uniqueId = GenerateRandomId();
                    string extension = Path.GetExtension(dto.Image.FileName);
                    string newFileName = uniqueId + extension;

                    string targetFolder = Path.Combine(_webHostEnvironment.WebRootPath, "be_assets", "img", "posts");
                    if (!Directory.Exists(targetFolder)) Directory.CreateDirectory(targetFolder);

                    string newPhysicalPath = Path.Combine(targetFolder, newFileName);

                    using (var stream = new FileStream(newPhysicalPath, FileMode.Create))
                    {
                        await dto.Image.CopyToAsync(stream);
                    }

                    post.Image_path = $"/be_assets/img/posts/{newFileName}";
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Eroare la actualizarea imaginii: {ex.Message}");
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Postarea a fost actualizată.",
                updatedDescription = post.Description,
                updatedImagePath = post.Image_path
            });
        }
    }
}