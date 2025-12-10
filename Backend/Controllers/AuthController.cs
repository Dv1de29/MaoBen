using Backend.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Backend.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;

        public AuthController(UserManager<ApplicationUser> userManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterUserDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null) return BadRequest(new { message = "Email is already in use." });

            var existingUsername = await _userManager.FindByNameAsync(dto.Username);
            if (existingUsername != null) return BadRequest(new { message = "Username is already taken." });

            var user = new ApplicationUser
            {
                UserName = dto.Username,
                Email = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
            };
            try
            {
                var result = await _userManager.CreateAsync(user, dto.Password);
                if (!result.Succeeded) return BadRequest(result.Errors);

                
                await _userManager.AddToRoleAsync(user, "User");

             
                var token = await GenerateJwtToken(user);

                
                return Ok(new AuthResponseDTO
                {
                    Token = token,
                    Username = user.UserName!,
                    ProfilePictureUrl= user.ProfilePictureUrl, 
                    Role = "User" 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An internal server error occurred. "});
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginUserDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            ApplicationUser? user=null;
            if (dto.UsernameOrEmail.Contains("@"))
            { 
                user = await _userManager.FindByEmailAsync(dto.UsernameOrEmail);
            }
            else
            {
                user = await _userManager.FindByNameAsync(dto.UsernameOrEmail);
            }

            if (user == null || !await _userManager.CheckPasswordAsync(user, dto.Password))
            {
                return Unauthorized(new { message = "Invalid email/username or password" });
            }

            var token = await GenerateJwtToken(user);

            var roles = await _userManager.GetRolesAsync(user);


            return Ok(new AuthResponseDTO
            {
                Token = token,
                Username = user.UserName!,
                ProfilePictureUrl = user.ProfilePictureUrl,
                Role = roles.FirstOrDefault() ?? "User"
            });
        }
        [NonAction]
        private async Task<string> GenerateJwtToken(ApplicationUser user)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = Encoding.ASCII.GetBytes(jwtSettings["Key"]!);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName!),
                new Claim(ClaimTypes.Email, user.Email!)
            };

            var roles = await _userManager.GetRolesAsync(user);
            var userRole = roles.FirstOrDefault() ?? "User";

            claims.Add(new Claim(ClaimTypes.Role, userRole));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"]
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}