using Backend.Data;
using Backend.DTOs.ProfileController;
using Backend.Models; // Asigură-te că ai namespace-ul corect pentru DTO-uri
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Backend.DTOs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

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

        public ProfileController(UserManager<ApplicationUser> userManager, IWebHostEnvironment webHostEnvironment, AppDbContext context)
        {
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
            _context = context;
        }

        private string GenerateRandomId()
        {
            Random random = new Random();
            long randomNumber = random.Next(1000000000) + 1000000000L; // Asigură 10 cifre
            return randomNumber.ToString();
        }


        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
          
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId!);
            if (user == null) return NotFound("User not found.");

            var response = new GetProfileUserResponseDTO
            {
                Name = user.FullName,
                Username = user.UserName!,
                Email = user.Email!,
                ProfilePictureUrl = user.ProfilePictureUrl,
                Privacy = user.Privacy,
                Description = user.Description,
                FollowersCount=user.FollowersCount,
                FollowingCount=user.FollowingCount
            };

            return Ok(response);
        }


        [HttpGet("{username}")]
        public async Task<IActionResult> GetProfileOfUser(string username)
        {
            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine(username);
            Console.WriteLine("--------------------------------------------------");

            if (string.IsNullOrEmpty(username))
            {
                return BadRequest("Username is required");
            }

            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.UserName!.ToLower() == username.ToLower());

            if (user == null) return NotFound($"User {username} not found");

            var response = new GetProfileUserResponseDTO
            {
                Name = user.FullName,
                Username = user.UserName!,
                Email = user.Email!,
                ProfilePictureUrl = user.ProfilePictureUrl,
                Privacy = user.Privacy,
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

            if (user == null) return NotFound("User not found.");

            if (!string.IsNullOrWhiteSpace(dto.Username) && dto.Username != user.UserName)
            {
                var existingUser = await _userManager.FindByNameAsync(dto.Username);
                if (existingUser != null)
                {
                    return BadRequest(new { message = $"Numele de utilizator '{dto.Username}' este deja folosit." });
                }

                var setUserNameResult = await _userManager.SetUserNameAsync(user, dto.Username);
                if (!setUserNameResult.Succeeded) return BadRequest(setUserNameResult.Errors);
            }

            if (dto.Privacy.HasValue)
            {
                user.Privacy = dto.Privacy.Value;
            }

            if (dto.Description != null)
            {
                user.Description = dto.Description;
            }

            if (dto.ProfilePictureUrl != null)
            {
                user.ProfilePictureUrl = dto.ProfilePictureUrl;
            }

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                return Ok(new
                {
                    message = "Profil actualizat cu succes!",
                    username = user.UserName,
                    privacy = user.Privacy,
                    description = user.Description,
                    profilePictureUrl=user.ProfilePictureUrl,
                });
            }

            return BadRequest(result.Errors);
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteAccount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId!);

            if (user == null) return NotFound("User not found.");
            

            //Stergem manual userul
            var follows = _context.UserFollows
                .Where(f => f.SourceUserId == userId || f.TargetUserId == userId);
            _context.UserFollows.RemoveRange(follows);

            await _context.SaveChangesAsync();

            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                return Ok(new { message = "Contul și toate datele asociate au fost șterse." });
            }
            return BadRequest(result.Errors);
        }


        [HttpGet("allUsers/{searchValue?}")]
        public async Task<IActionResult> GetAllUsers(string searchValue)
        {
            List<GetAllProfilesDTO> users;

            if (string.IsNullOrEmpty(searchValue))
            {
                users = await _userManager.Users
                                    .AsNoTracking()
                                    .Select(u => new GetAllProfilesDTO
                                    {
                                        name = u.FullName,
                                        username = u.UserName,
                                        ProfilePictureUrl = u.ProfilePictureUrl,
                                    })
                                    .Take(30)
                                    .ToListAsync();
            }
            else
            {
                users = await _userManager.Users
                                    .AsNoTracking()
                                    .Where(u => u.UserName.Contains(searchValue) || (u.FirstName + " " + u.LastName).Contains(searchValue))
                                    .Select(u => new GetAllProfilesDTO
                                    {
                                        name = u.FullName,
                                        username = u.UserName,
                                        ProfilePictureUrl = u.ProfilePictureUrl,
                                    })
                                    .Take(30)
                                    .ToListAsync();
            }



            if ( users == null)
            {
                return Ok(new List<GetAllProfilesDTO>());
            }

            return Ok(users);
        }


        [HttpPost("upload_image")]
        [Consumes("multipart/form-data")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> UploadImage(IFormFile image_path)
        {

            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine("DEBUG: UploadImage method hit. Attempting to use be_assets.");
            Console.WriteLine("--------------------------------------------------");

            if (image_path == null || image_path.Length == 0)
            {
                return BadRequest("Nu a fost furnizat niciun fișier.");
            }

            if(!image_path.ContentType.StartsWith("image/"))
            {
                return BadRequest("File type is wrong and should be: image/");
            }

            string uniqueId = this.GenerateRandomId();
            string extension = Path.GetExtension(image_path.FileName);
            string fileName = uniqueId + extension;

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
                return StatusCode(StatusCodes.Status500InternalServerError,
                    $"Eroare la salvarea fișierului: {ex.Message}");
            }
        }
        
        [NonAction]
        public async Task<IActionResult> GetFollowers()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId!);
            if (user == null) return NotFound("User not found.");
            var followers = _context.UserFollows
                .Where(f => f.TargetUserId == userId && f.Status == FollowStatus.Accepted)
                .Select(f => f.SourceUserId)
                .ToList();
            var followerDetails = new List<GetProfileUserResponseDTO>();
            foreach (var followerId in followers)
            {
                var followerUser = await _userManager.FindByIdAsync(followerId);
                if (followerUser != null)
                {
                    followerDetails.Add(new GetProfileUserResponseDTO
                    {
                        Username = followerUser.UserName!,
                        Email = followerUser.Email!,
                        ProfilePictureUrl = followerUser.ProfilePictureUrl,
                        Privacy = followerUser.Privacy,
                        Description = followerUser.Description
                    });
                }
            }
            return Ok(followerDetails);
        }
    }
}