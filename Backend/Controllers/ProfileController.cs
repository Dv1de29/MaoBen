using Backend.Models; // Asigură-te că ai namespace-ul corect pentru DTO-uri
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Backend.DTOs;
namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ProfileController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
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
    }
}