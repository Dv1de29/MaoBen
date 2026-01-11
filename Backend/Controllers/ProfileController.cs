using Backend.Data;
using Backend.DTOs.ProfileController;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Backend.Services;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly AppDbContext _context;
        private readonly IAiContentService _aiService;

        public ProfileController(UserManager<ApplicationUser> userManager, IWebHostEnvironment webHostEnvironment, AppDbContext context, IAiContentService aiService)
        {
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
            _context = context;
            _aiService = aiService;
        }

        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId!);

            if (user == null) return NotFound(new { error = "User not found." });

            var response = new GetProfileUserResponseDTO
            {
                Name = user.FullName,
                Username = user.UserName!,
                Email = user.Email!,
                ProfilePictureUrl = user.ProfilePictureUrl,
                Privacy = user.IsPrivate,
                Description = user.Description,
                FollowersCount = user.FollowersCount,
                FollowingCount = user.FollowingCount
            };

            return Ok(response);
        }

        [HttpGet("{username}")]
        public async Task<IActionResult> GetProfileOfUser(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return BadRequest(new { error = "Username is required." });
            }

            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.UserName!.ToLower() == username.ToLower());

            if (user == null) return NotFound(new { error = $"User '{username}' not found." });

            var response = new GetProfileUserResponseDTO
            {
                Name = user.FullName,
                Username = user.UserName!,
                Email = user.Email!,
                ProfilePictureUrl = user.ProfilePictureUrl,
                Privacy = user.IsPrivate,
                Description = user.Description,
                FollowersCount = user.FollowersCount,
                FollowingCount = user.FollowingCount
            };

            return Ok(response);
        }


        [HttpPut]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileUserDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId!);

            if (user == null) return NotFound(new { error = "User not found." });

            bool hasChanges = false;

            if (!string.IsNullOrWhiteSpace(dto.Username) && dto.Username != user.UserName)
            {
                var existingUser = await _userManager.FindByNameAsync(dto.Username);
                if (existingUser != null && existingUser.Id != user.Id)
                {
                    return BadRequest(new { error = $"The username '{dto.Username}' is already in use." });
                }

                // We update the property directly; UpdateAsync will handle normalization
                user.UserName = dto.Username;
                hasChanges = true;
            }

        
            if (dto.Privacy.HasValue && user.IsPrivate != dto.Privacy.Value)
            {
                user.IsPrivate = dto.Privacy.Value;
                hasChanges = true;
            }

 
            if (dto.Description != null && user.Description != dto.Description)
            {
                bool isSafe = await _aiService.IsContentSafeAsync(dto.Description);
                if (!isSafe)
                {
                    return BadRequest(new { error = "The profile description contains inappropriate terms. Please reformulate." });
                }

                user.Description = dto.Description;
                hasChanges = true;
            }

            if (dto.ProfilePictureUrl != null && user.ProfilePictureUrl != dto.ProfilePictureUrl)
            {
                user.ProfilePictureUrl = dto.ProfilePictureUrl;
                hasChanges = true;
            }

           
            if (hasChanges)
            {
                // UpdateAsync handles both the basic fields and the NormalizedUserName
                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded) return BadRequest(result.Errors);
            }

            return Ok(new
            {
                message = "Profile updated successfully!",
                username = user.UserName,
                privacy = user.IsPrivate,
                description = user.Description,
                profilePictureUrl = user.ProfilePictureUrl,
            });
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteAccount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId!);

            if (user == null) return NotFound(new { error = "User not found." });

            var follows = _context.UserFollows
                .Where(f => f.SourceUserId == userId || f.TargetUserId == userId);
            _context.UserFollows.RemoveRange(follows);

            await _context.SaveChangesAsync();

            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                return Ok(new { message = "Account and all associated data have been deleted successfully." });
            }
            return BadRequest(result.Errors);
        }


        [HttpGet("allUsers/{searchValue?}")]
        public async Task<IActionResult> GetAllUsers(string? searchValue)
        {
            IQueryable<ApplicationUser> query = _userManager.Users.AsNoTracking();

            if (!string.IsNullOrEmpty(searchValue))
            {
                query = query.Where(u => u.UserName!.Contains(searchValue) ||
                                       (u.FirstName + " " + u.LastName).Contains(searchValue));
            }

            var users = await query
                .Select(u => new GetAllProfilesDTO
                {
                    name = u.FullName,
                    username = u.UserName!,
                    ProfilePictureUrl = u.ProfilePictureUrl,
                })
                .Take(30)
                .ToListAsync();

            return Ok(users ?? new List<GetAllProfilesDTO>());
        }

        [HttpPost("upload_image")]
        [Consumes("multipart/form-data")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> UploadImage(IFormFile image_path)
        {
            if (image_path == null || image_path.Length == 0)
            {
                return BadRequest(new { error = "No file provided." });
            }

            if (!image_path.ContentType.StartsWith("image/"))
            {
                return BadRequest(new { error = "Invalid file type. Please upload an image." });
            }


            string extension = Path.GetExtension(image_path.FileName);
            string fileName = $"{Guid.NewGuid()}{extension}";

            string relativePath = $"/be_assets/img/profile/{fileName}";
            string targetFolder = Path.Combine(_webHostEnvironment.WebRootPath, "be_assets", "img", "profile");
            string physicalPath = Path.Combine(targetFolder, fileName);

            try
            {
                if (!Directory.Exists(targetFolder))
                {
                    Directory.CreateDirectory(targetFolder);
                }

                using (var stream = new FileStream(physicalPath, FileMode.Create))
                {
                    await image_path.CopyToAsync(stream);
                }

                return Ok(new { filePath = relativePath });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Error saving the file: {ex.Message}" });
            }
        }
    }
}