using Backend.DTOs;   // FIX 1: Matches your folder name 'DTOs'
using Backend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public AuthController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [HttpPost("register")]
        // FIX 2: Changed 'RegisterDto' to 'RegisterUserDTO' to match your file
        public async Task<IActionResult> Register([FromBody] RegisterUserDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
            {
                return BadRequest(new { message = "Email is already in use." });
            }

            var existingUsername = await _userManager.FindByNameAsync(dto.Username);
            if (existingUsername != null)
            {
                return BadRequest(new { message = "Username is already taken." });
            }

            var user = new ApplicationUser
            {
                UserName = dto.Username,
                Email = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Description = string.Empty,
                ProfilePictureUrl = string.Empty,
                IsPrivate = false
            };
            try
            {
                // 1. ATTEMPT creation
                // This handles standard logic (Password strength, Duplicate Email, etc.)
                var result = await _userManager.CreateAsync(user, dto.Password);

                // 2. CHECK Identity Logic Failures
                // If Identity says "No", we stop here.
                if (!result.Succeeded)
                {
                    return BadRequest(result.Errors);
                }

                // Success!
                return Ok(user);
            }
            catch (DbUpdateException dbEx)
            {
                // 3. CATCH Database Constraint Violations
                // This runs ONLY if the Database actually rejected the SQL command 
                // (e.g., a custom unique index violation, or a Foreign Key error)

                // Log the error for yourself (optional)
                // _logger.LogError(dbEx, "Database constraint failed");

                // Return a generic error to the user so you don't leak DB details
                return BadRequest(new { message = "A database constraint was violated." });
            }
            catch (Exception ex)
            {
                // 4. CATCH Everything Else (Server crashed, Null Reference, etc.)
                return StatusCode(500, new { message = "An internal server error occurred." });
            }


        }
    }
}