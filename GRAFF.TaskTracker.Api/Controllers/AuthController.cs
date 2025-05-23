using GRAFF.TaskTracker.Api.Data;
using GRAFF.TaskTracker.Api.DTOs;
using GRAFF.TaskTracker.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace GRAFF.TaskTracker.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole<int>> _roleManager; // If using roles
        private readonly IConfiguration _configuration;
        private readonly TaskTrackerDbContext _context; // For direct queries if needed

        public AuthController(
            UserManager<User> userManager,
            RoleManager<IdentityRole<int>> roleManager,
            IConfiguration configuration,
            TaskTrackerDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _context = context;
        }

        [HttpPost]
        [Route("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userExists = await _userManager.FindByNameAsync(model.Username!);
            if (userExists != null)
                return StatusCode(StatusCodes.Status409Conflict, new { Status = "Error", Message = "User already exists!" });

            var emailExists = await _userManager.FindByEmailAsync(model.Email!);
            if (emailExists != null)
                return StatusCode(StatusCodes.Status409Conflict, new { Status = "Error", Message = "Email already exists!" });
            
            User user = new()
            {
                Email = model.Email,
                SecurityStamp = Guid.NewGuid().ToString(), // Important for security operations
                UserName = model.Username
                // UserId will be set by Identity
            };
            var result = await _userManager.CreateAsync(user, model.Password!);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description);
                return StatusCode(StatusCodes.Status500InternalServerError, new { Status = "Error", Message = "User creation failed! Please check user details and try again.", Errors = errors });
            }

            // TODO: Optionally assign roles here if you have them
            // if (!await _roleManager.RoleExistsAsync("User"))
            //    await _roleManager.CreateAsync(new IdentityRole<int>("User"));
            // await _userManager.AddToRoleAsync(user, "User");

            return Ok(new { Status = "Success", Message = "User created successfully!" });
        }

        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.FindByNameAsync(model.Username!);
            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password!))
            {
                return Unauthorized(new { Status="Error", Message = "Invalid username or password."});
            }

            var userRoles = await _userManager.GetRolesAsync(user);

            var authClaims = new List<Claim>
            {
               new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), // Use Id from IdentityUser
               new Claim(ClaimTypes.Name, user.UserName!),
               new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            foreach (var userRole in userRoles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, userRole));
            }

            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"] ?? throw new ArgumentNullException("JWT:Secret")));
            
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(authClaims),
                Expires = DateTime.UtcNow.AddHours(Convert.ToDouble(_configuration["JWT:TokenValidityInHours"] ?? "1")), // Token validity
                Issuer = _configuration["JWT:ValidIssuer"],
                Audience = _configuration["JWT:ValidAudience"],
                SigningCredentials = new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            };
            
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            
            return Ok(new AuthResponseModel
            {
                Token = tokenHandler.WriteToken(token),
                Expiration = token.ValidTo,
                Username = user.UserName,
                Email = user.Email,
                UserId = user.Id // Send back the user ID
            });
        }
    }
}
