using Backend.Data;
using Backend.DTOs.ProfileController;
using Backend.Models; // Asigură-te că ai namespace-ul corect pentru DTO-uri
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
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
                Username = user.UserName!,
                Email = user.Email!,
                ProfilePictureUrl = user.ProfilePictureUrl,
                Privacy = user.Privacy,
                Description = user.Description
            };

            return Ok(response);
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



        [HttpPut]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileUserDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId!);

            if (user == null) return NotFound("User not found.");

            // --- LOGICA PENTRU SCHIMBAREA USERNAME-ULUI ---

            // 1. Verificăm dacă userul a trimis un username nou și dacă e diferit de cel curent
            if (!string.IsNullOrWhiteSpace(dto.Username) && dto.Username != user.UserName)
            {
                // 2. Verificăm în baza de date dacă acest nume este deja luat de altcineva
                var existingUser = await _userManager.FindByNameAsync(dto.Username);

                if (existingUser != null)
                {
                    // Numele există deja! Returnăm eroare.
                    return BadRequest(new { message = $"Numele de utilizator '{dto.Username}' este deja folosit." });
                }

                // 3. Dacă e liber, îl setăm
                // Putem folosi SetUserNameAsync sau setare directă + UpdateAsync. 
                // SetUserNameAsync e mai sigur pentru că actualizează și câmpul NormalizedUserName.
                var setUserNameResult = await _userManager.SetUserNameAsync(user, dto.Username);

                if (!setUserNameResult.Succeeded)
                {
                    return BadRequest(setUserNameResult.Errors);
                }
            }

            // --- ACTUALIZAREA CELORLALTE CÂMPURI ---
            user.Privacy = dto.Privacy;
            user.Description = dto.Description;
            user.ProfilePictureUrl = dto.ProfilePictureUrl;

            // Salvăm modificările finale în baza de date
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                // Putem returna și noul username în mesaj pentru confirmare
                return Ok(new
                {
                    message = "Profil actualizat cu succes!",
                    username = user.UserName
                });
            }

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

            // --- CURĂȚENIE ÎNAINTE DE ȘTERGERE ---

            // 1. Ștergem relațiile de Follow (Unde userul este Sursă SAU Țintă)
            var follows = _context.UserFollows
                .Where(f => f.SourceUserId == userId || f.TargetUserId == userId);

            _context.UserFollows.RemoveRange(follows);

            // NOTĂ: Dacă ai Postări, Comentarii sau Like-uri, trebuie să le ștergi și pe ele AICI!
            // Exemplu:
            // var posts = _context.Posts.Where(p => p.UserId == userId);
            // _context.Posts.RemoveRange(posts);

            // Salvăm ștergerea datelor dependente
            await _context.SaveChangesAsync();

            // --- ACUM PUTEM ȘTERGE USERUL ---
            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                return Ok(new { message = "Contul și toate datele asociate au fost șterse." });
            }

            return BadRequest(result.Errors);
        }
    }
}