
using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PostsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly int MaxPostsLimit = 50;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public PostsController(AppDbContext context, IWebHostEnvironment webHostEnvironment) {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        private string GenerateRandomId()
        {
            Random random = new Random();
            long randomNumber = random.Next(1000000000) + 1000000000L; // Asigur? 10 cifre
            return randomNumber.ToString();
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPostID(int id)
        {
            var post = await _context.Posts
                                    .Include(p => p.User)
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
                .ToListAsync();

            return Ok(myPosts);
        }

        [HttpGet]
        public async Task<IActionResult> GetRecentPosts(
            [FromQuery] int count = 20,
            [FromQuery] int skip = 0
            )
        {
            int limit = Math.Min(count, MaxPostsLimit);

            var posts = await _context.Posts
                .Include(p => p.User)
                .OrderByDescending(p => p.Id)
                .Skip(skip)
                .Take(limit)
                .ToListAsync();

            if (posts == null || !posts.Any())
            {
                return Ok(new List<Posts>());
            }

            return Ok(posts);
        }

        [HttpGet("ByOwner/{ownerId}")]
        public async Task<IActionResult> GetPostsByOwnerId(string ownerId)
        {
            if (string.IsNullOrEmpty(ownerId))
            {
                return BadRequest("Owner ID cannot be empty.");
            }

            var posts = await _context.Posts
                .Include(p => p.User)
                .Where(p => p.OwnerID == ownerId)
                .OrderByDescending(p => p.Id)
                .ToListAsync();


            // DACA NU SE GASESC POSTARI, RETURNEZ O LISTA GOALA
            //if (posts == null || !posts.Any()){ 
            //    return new List<Posts>();
            //}

            return Ok(posts);
        }

        [Authorize]
        [HttpPost("create_post")]
        [Consumes("multipart/form-data")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> CreatePost([FromForm] CreatePostDTO dto)
        {
            if (dto.Image == null || dto.Image.Length == 0)
            {
                return BadRequest("Image is required.");
            }

            if (!dto.Image.ContentType.StartsWith("image/"))
            {
                return BadRequest("File type is wrong and should be: image/");
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
                Created = DateTime.UtcNow
                
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
    }
}
