
using Backend.Data;
using Backend.DTOs;
using Backend.Models;
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
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var post = await _context.Posts
                                    .Include(p => p.User)
                                    .Select(p => new GetPostsWithUser // Project directly to DTO
                                    {
                                        Id = p.Id,
                                        OwnerID = p.OwnerID,
                                        Nr_likes = p.Nr_likes,
                                        Nr_Comms = p.Nr_Comms,
                                        Image_path = p.Image_path,
                                        Description = p.Description,
                                        Created = p.Created,
                                        Username = p.User.UserName, // Flattened relationship
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

                    // Map the data from the related User object
                    Username = p.User.UserName,

                    // MAKE SURE your ApplicationUser class has a property for the image.
                    // If it is named differently (e.g. ProfilePicture), change the right side below:
                    user_image_path = p.User.ProfilePictureUrl,
                    Has_liked = _context.PostLikes.Any(pl => pl.PostId == p.Id && pl.UserId == userId),
                })
                .ToListAsync();

            return Ok(myPosts);
        }
        [Authorize] // 1. Asigură-te că userul este logat
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
            // Verificăm dacă userul este proprietarul postării
            bool isOwner = post.OwnerID == currentUserId;

            // Verificăm dacă userul are rolul de Administrator
            // Notă: Asigură-te că în JWT (AuthController) ai pus claim-ul de rol corect ("Administrator" sau "Admin")
            bool isAdmin = User.IsInRole("Admin");

            if (!isOwner && !isAdmin)
            {
                return StatusCode(403, "Nu ai permisiunea de a șterge această postare.");
            }

            // 5. (Opțional dar recomandat) Șterge și fișierul fizic (poza) de pe server
            // Astfel nu rămâi cu poze "orfane" care ocupă spațiu degeaba
            try
            {
                if (!string.IsNullOrEmpty(post.Image_path))
                {
                    // Image_path e de forma "/be_assets/...", scoatem primul slash pentru Path.Combine
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
                // Putem loga eroarea, dar nu oprim ștergerea din baza de date doar pentru că a eșuat ștergerea fișierului
                Console.WriteLine($"Nu s-a putut șterge fișierul: {ex.Message}");
            }

            // 6. Șterge postarea din baza de date
            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Postarea a fost ștersă cu succes." });
        }
        [Authorize]
        [HttpPut("{id}")]
        [Consumes("multipart/form-data")] // Permite upload de fisiere
        public async Task<IActionResult> UpdatePost(int id, [FromForm] EditPostDto dto)
        {
            // 1. Identificăm userul
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId)) return Unauthorized();

            // 2. Căutăm postarea
            var post = await _context.Posts.FindAsync(id);
            if (post == null) return NotFound("Postarea nu a fost găsită.");

            // 3. Verificăm permisiunea (Doar proprietarul poate edita)
            // Adminul poate șterge, dar de obicei nu edităm conținutul utilizatorilor (etică/legal)
            if (post.OwnerID != currentUserId)
            {
                return StatusCode(403, "Nu ai dreptul să editezi această postare.");
            }

            // 4. Actualizăm Descrierea (dacă a fost trimisă)
            if (dto.Description != null)
            {
                post.Description = dto.Description;
            }

            // 5. Actualizăm Poza (DOAR dacă a fost trimisă una nouă)
            if (dto.Image != null && dto.Image.Length > 0)
            {
                // Validare tip fișier
                if (!dto.Image.ContentType.StartsWith("image/"))
                {
                    return BadRequest("Fișierul trebuie să fie o imagine.");
                }

                try
                {
                    // A. ȘTERGEM POZA VECHE de pe disc
                    if (!string.IsNullOrEmpty(post.Image_path))
                    {
                        // Convertim calea relativă (ex: /be_assets/...) în cale absolută
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

                    // Folderul unde salvăm
                    string targetFolder = Path.Combine(_webHostEnvironment.WebRootPath, "be_assets", "img", "posts");
                    if (!Directory.Exists(targetFolder)) Directory.CreateDirectory(targetFolder);

                    string newPhysicalPath = Path.Combine(targetFolder, newFileName);

                    using (var stream = new FileStream(newPhysicalPath, FileMode.Create))
                    {
                        await dto.Image.CopyToAsync(stream);
                    }

                    // C. Actualizăm calea în baza de date
                    post.Image_path = $"/be_assets/img/posts/{newFileName}";
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Eroare la actualizarea imaginii: {ex.Message}");
                }
            }

            // 6. Salvăm modificările în DB
            // UpdatedAt = DateTime.UtcNow; // Dacă ai avea un câmp UpdatedAt, aici l-ai seta

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Postarea a fost actualizată.",
                updatedDescription = post.Description,
                updatedImagePath = post.Image_path
            });
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
                .AsNoTracking() // Optimization: Read-only, so we don't need tracking
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

                    // Map the data from the related User object
                    Username = p.User.UserName,

                    // MAKE SURE your ApplicationUser class has a property for the image.
                    // If it is named differently (e.g. ProfilePicture), change the right side below:
                    user_image_path = p.User.ProfilePictureUrl,


                    Has_liked = _context.PostLikes.Any(pl => pl.PostId == p.Id && pl.UserId == currentUserId),
                })
                .ToListAsync();


            if (posts == null || !posts.Any())
            {
                return Ok(new List<Posts>());
            }

            return Ok(posts);
        }

        [HttpGet("ByOwner/{ownerUsername}")]
        public async Task<IActionResult> GetPostsByOwnerId(string ownerUsername)
        {
            if (string.IsNullOrEmpty(ownerUsername))
            {
                return BadRequest("Username cannot be empty.");
            }

            var posts = await _context.Posts
                .Include(p => p.User)
                .Where(p => p.User.UserName!.ToLower() == ownerUsername.ToLower())
                .OrderByDescending(p => p.Created)
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
    }
}
