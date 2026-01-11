using Backend.Data;
using Backend.DTOs.PostsController;
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
    public class PostsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly int MaxPostsLimit = 50;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IAiContentService _aiService;

        public PostsController(AppDbContext context, IWebHostEnvironment webHostEnvironment, IAiContentService aiService)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _aiService = aiService;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPostID(int id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var post = await _context.Posts
                .Include(p => p.User)
                .Select(p => new GetPostsWithUserResponseDTO
                {
                    Id = p.Id,
                    OwnerID = p.OwnerID,
                    Nr_likes = p.Nr_likes,
                    Nr_Comms = p.Nr_Comms,
                    Image_path = p.Image_path,
                    Description = p.Description,
                    Created = p.Created,
                    Username = p.User.UserName!,
                    user_image_path = p.User.ProfilePictureUrl,
                    Has_liked = _context.PostLikes.Any(pl => pl.PostId == p.Id && pl.UserId == currentUserId),
                })
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null)
            {
                return NotFound(new { error = "Post not found." });
            }

            return Ok(post);
        }

        [Authorize]
        [HttpGet("my_posts")]
        public async Task<IActionResult> GetMyPosts()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "User ID not found in token." });
            }

            var myPosts = await _context.Posts
                .Include(p => p.User)
                .Where(p => p.OwnerID == userId)
                .OrderByDescending(p => p.Created)
                .Select(p => new GetPostsWithUserResponseDTO
                {
                    Id = p.Id,
                    OwnerID = p.OwnerID,
                    Nr_likes = p.Nr_likes,
                    Nr_Comms = p.Nr_Comms,
                    Image_path = p.Image_path,
                    Description = p.Description,
                    Created = p.Created,
                    Username = p.User.UserName!,
                    user_image_path = p.User.ProfilePictureUrl,
                    Has_liked = _context.PostLikes.Any(pl => pl.PostId == p.Id && pl.UserId == userId),
                })
                .ToListAsync();

            return Ok(myPosts);
        }

        [Authorize]
        [HttpGet("feed")]
        public async Task<IActionResult> GetFeed([FromQuery] int count = 20, [FromQuery] int skip = 0)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var followingIds = await _context.UserFollows
                .Where(f => f.SourceUserId == currentUserId && f.Status == FollowStatus.Accepted)
                .Select(f => f.TargetUserId)
                .ToListAsync();

            var posts = await _context.Posts
                .Include(p => p.User)
                .Where(p => followingIds.Contains(p.OwnerID))
                .OrderByDescending(p => p.Created)
                .Skip(skip)
                .Take(count)
                .Select(p => new GetPostsWithUserResponseDTO
                {
                    Id = p.Id,
                    OwnerID = p.OwnerID,
                    Nr_likes = p.Nr_likes,
                    Nr_Comms = p.Nr_Comms,
                    Image_path = p.Image_path,
                    Description = p.Description,
                    Created = p.Created,
                    Username = p.User.UserName!,
                    user_image_path = p.User.ProfilePictureUrl,
                    Has_liked = _context.PostLikes.Any(pl => pl.PostId == p.Id && pl.UserId == currentUserId),
                })
                .ToListAsync();

            return Ok(posts);
        }

        [HttpGet]
        public async Task<IActionResult> GetRecentPosts([FromQuery] int count = 20, [FromQuery] int skip = 0)
        {
            int limit = Math.Min(count, MaxPostsLimit);
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var posts = await _context.Posts
                .AsNoTracking()
                .OrderByDescending(p => p.Created)
                .Skip(skip)
                .Take(limit)
                .Select(p => new GetPostsWithUserResponseDTO
                {
                    Id = p.Id,
                    OwnerID = p.OwnerID,
                    Nr_likes = p.Nr_likes,
                    Nr_Comms = p.Nr_Comms,
                    Image_path = p.Image_path,
                    Description = p.Description,
                    Created = p.Created,
                    Username = p.User.UserName!,
                    user_image_path = p.User.ProfilePictureUrl,
                    Has_liked = _context.PostLikes.Any(pl => pl.PostId == p.Id && pl.UserId == currentUserId),
                })
                .ToListAsync();

            return Ok(posts ?? new List<GetPostsWithUserResponseDTO>());
        }


        [HttpGet("ByOwner/{ownerUsername}")]
        public async Task<IActionResult> GetPostsByOwnerId(string ownerUsername)
        {
            if (string.IsNullOrEmpty(ownerUsername)) return BadRequest(new { error = "Username cannot be empty." });

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var targetUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == ownerUsername);

            if (targetUser == null) return NotFound(new { error = "User not found." });

            bool canView = false;
            if (!targetUser.IsPrivate) canView = true;
            else if (targetUser.Id == currentUserId) canView = true;
            else
            {
                var isFollowing = await _context.UserFollows
                    .AnyAsync(f => f.SourceUserId == currentUserId && f.TargetUserId == targetUser.Id && f.Status == FollowStatus.Accepted);
                if (isFollowing) canView = true;
            }

            if (!canView) return StatusCode(403, new { error = "This account is private." });

            var posts = await _context.Posts
                .Include(p => p.User)
                .Where(p => p.User.UserName!.ToLower() == ownerUsername.ToLower())
                .OrderByDescending(p => p.Created)
                .Select(p => new GetPostsWithUserResponseDTO
                {
                    Id = p.Id,
                    OwnerID = p.OwnerID,
                    Nr_likes = p.Nr_likes,
                    Nr_Comms = p.Nr_Comms,
                    Image_path = p.Image_path,
                    Description = p.Description,
                    Created = p.Created,
                    Username = p.User.UserName!,
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
            if (!string.IsNullOrWhiteSpace(dto.Description))
            {
                bool isSafe = await _aiService.IsContentSafeAsync(dto.Description);
                if (!isSafe)
                {
                    return BadRequest(new { error = "Your content contains inappropriate terms. Please reformulate." });
                }
            }

            if (dto.Image == null || dto.Image.Length == 0)
            {
                return BadRequest(new { error = "An image or video file is required." });
            }

            if (!dto.Image.ContentType.StartsWith("image/") && !dto.Image.ContentType.StartsWith("video/"))
            {
                return BadRequest(new { error = "Unsupported file type. Please upload an image or video." });
            }

            string extension = Path.GetExtension(dto.Image.FileName);
            string fileName = $"{Guid.NewGuid()}{extension}";
            string relativePath = $"/be_assets/img/posts/{fileName}";

            string targetFolder = Path.Combine(_webHostEnvironment.WebRootPath, "be_assets", "img", "posts");
            string physicalPath = Path.Combine(targetFolder, fileName);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized(new { error = "Authentication session expired." });

            var newPost = new Posts
            {
                OwnerID = userId!,
                Image_path = relativePath,
                Description = dto.Description,
                Created = DateTime.UtcNow,
            };

            try
            {
                if (!Directory.Exists(targetFolder)) Directory.CreateDirectory(targetFolder);

                using (var stream = new FileStream(physicalPath, FileMode.Create))
                {
                    await dto.Image.CopyToAsync(stream);
                }

                _context.Posts.Add(newPost);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Post created successfully!", postId = newPost.Id, imageUrl = relativePath });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Server error while saving post: {ex.Message}" });
            }
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePost(int id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var post = await _context.Posts.FindAsync(id);

            if (post == null) return NotFound(new { error = "Post not found." });

            bool isOwner = post.OwnerID == currentUserId;
            bool isAdmin = User.IsInRole("Admin");

            if (!isOwner && !isAdmin)
            {
                return StatusCode(403, new { error = "You do not have permission to delete this post." });
            }

            try
            {
                if (!string.IsNullOrEmpty(post.Image_path))
                {
                    string absolutePath = Path.Combine(_webHostEnvironment.WebRootPath, post.Image_path.TrimStart('/', '\\'));
                    if (System.IO.File.Exists(absolutePath)) System.IO.File.Delete(absolutePath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"File deletion failed: {ex.Message}");
            }

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Post deleted successfully." });
        }

        [Authorize]
        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdatePost(int id, [FromForm] EditPostDto dto)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var post = await _context.Posts.FindAsync(id);

            if (post == null) return NotFound(new { error = "Post not found." });

            if (post.OwnerID != currentUserId)
            {
                return StatusCode(403, new { error = "You can only edit your own posts." });
            }

            if (dto.Description != null)
            {
                bool isSafe = await _aiService.IsContentSafeAsync(dto.Description);
                if (!isSafe) return BadRequest(new { error = "Updated description contains inappropriate terms." });
                post.Description = dto.Description;
            }

            if (dto.Image != null && dto.Image.Length > 0)
            {
                if (!dto.Image.ContentType.StartsWith("image/") && !dto.Image.ContentType.StartsWith("video/"))
                {
                    return BadRequest(new { error = "Invalid file type." });
                }

                try
                {
                    if (!string.IsNullOrEmpty(post.Image_path))
                    {
                        string oldPath = Path.Combine(_webHostEnvironment.WebRootPath, post.Image_path.TrimStart('/', '\\'));
                        if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                    }

                    string extension = Path.GetExtension(dto.Image.FileName);
                    string newFileName = $"{Guid.NewGuid()}{extension}";
                    string targetFolder = Path.Combine(_webHostEnvironment.WebRootPath, "be_assets", "img", "posts");
                    string newPhysicalPath = Path.Combine(targetFolder, newFileName);

                    using (var stream = new FileStream(newPhysicalPath, FileMode.Create))
                    {
                        await dto.Image.CopyToAsync(stream);
                    }

                    post.Image_path = $"/be_assets/img/posts/{newFileName}";
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { error = $"Error updating media: {ex.Message}" });
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Post updated successfully.",
                updatedDescription = post.Description,
                updatedImagePath = post.Image_path
            });
        }
    }
}