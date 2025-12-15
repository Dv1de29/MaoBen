using Backend.Models; // Asigură-te că ai namespace-ul corect pentru DTO-uri
using Microsoft.AspNetCore.Authorization;
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

        public ProfileController(UserManager<ApplicationUser> userManager, IWebHostEnvironment webHostEnvironment)
        {
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
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
                Username = user.UserName!,
                Email = user.Email!,
                ProfilePictureUrl = user.ProfilePictureUrl,
                Privacy = user.Privacy,
                Description = user.Description
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
                Username = user.UserName!,
                Email = user.Email!,
                ProfilePictureUrl = user.ProfilePictureUrl,
                Privacy = user.Privacy,
                Description = user.Description
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

            // Actualizăm câmpurile dorite
            // NOTĂ: Nu lăsăm userul să schimbe ID-ul sau Username-ul aici, de obicei
            user.Privacy = dto.Privacy;
            user.Description=dto.Description;
            user.ProfilePictureUrl = dto.ProfilePictureUrl;
            // Exemplu: Dacă vrei să poată schimba și telefonul
            // user.PhoneNumber = dto.PhoneNumber; 

            // Salvăm modificările în baza de date
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                return Ok(new { message = "Profil actualizat cu succes!" });
            }

            // Dacă apar erori la salvare
            return BadRequest(result.Errors);
        }

        // 3. DELETE: api/profile
        // Șterge contul utilizatorului curent
        [HttpDelete]
        public async Task<IActionResult> DeleteAccount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId!);

            if (user == null) return NotFound("User not found.");

            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                return Ok(new { message = "Contul a fost șters definitiv." });
            }

            return BadRequest(result.Errors);
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
    }
}