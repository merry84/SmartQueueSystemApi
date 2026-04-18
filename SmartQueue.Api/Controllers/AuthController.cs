using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SmartQueue.Api.DTOs;
using SmartQueue.Api.Models;

namespace SmartQueue.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IConfiguration configuration;

        public AuthController(UserManager<ApplicationUser> userManager,
                SignInManager<ApplicationUser> signInManager,
                IConfiguration configuration)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register(RegisterRequestDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existingUser = await userManager.FindByEmailAsync(model.Email);

            if (existingUser != null)
            {
                return BadRequest(new AuthResponseDto
                {
                    Succeeded = false,
                    Message = "User with this email already exists."
                });
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email
            };

            var result = await userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));

                return BadRequest(new AuthResponseDto
                {
                    Succeeded = false,
                    Message = errors
                });
            }

            return Ok(new AuthResponseDto
            {
                Succeeded = true,
                Message = "User registered successfully."
            });
        }
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseDto>> Login(LoginRequestDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                return Unauthorized(new LoginResponseDto
                {
                    Succeeded = false,
                    Message = "Invalid email or password."
                });
            }

            var result = await signInManager.CheckPasswordSignInAsync(user, model.Password, false);

            if (!result.Succeeded)
            {
                return Unauthorized(new LoginResponseDto
                {
                    Succeeded = false,
                    Message = "Invalid email or password."
                });
            }

            var userRoles = await userManager.GetRolesAsync(user);

            var claims = new List<Claim>
    {
                    new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                    new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Name, user.UserName ?? user.Email ?? string.Empty),
                    new Claim(ClaimTypes.Email, user.Email ?? string.Empty)
    };

            foreach (var role in userRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var jwtKey = configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("JWT Key is missing.");

            var jwtIssuer = configuration["Jwt:Issuer"]
                ?? throw new InvalidOperationException("JWT Issuer is missing.");

            var jwtAudience = configuration["Jwt:Audience"]
                ?? throw new InvalidOperationException("JWT Audience is missing.");

            var expiresInMinutes = int.TryParse(configuration["Jwt:ExpiresInMinutes"], out var minutes)
                ? minutes
                : 60;

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var expiresAtUtc = DateTime.UtcNow.AddMinutes(expiresInMinutes);

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: expiresAtUtc,
                signingCredentials: credentials);

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return Ok(new LoginResponseDto
            {
                Succeeded = true,
                Message = "Login successful.",
                Token = tokenString,
                ExpiresAtUtc = expiresAtUtc
            });
        }

        [HttpPost("assign-role")]
        public async Task<ActionResult<AuthResponseDto>> AssignRole(AssignRoleRequestDto model)
        {
           var user = await userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                return NotFound(new AuthResponseDto
                {
                    Succeeded = false,
                    Message = "User not found."
                });
            }

            var validRoles = new[] { "Admin", "Operator" };

            if (!validRoles.Contains(model.Role))
            {
                return BadRequest(new AuthResponseDto
                {
                    Succeeded = false,
                    Message = "Invalid role."
                });
            }

            var isAlreadyInRole = await userManager.IsInRoleAsync(user, model.Role);

            if (isAlreadyInRole)
            {
                return BadRequest(new AuthResponseDto
                {
                    Succeeded = false,
                    Message = $"User already has role {model.Role}."
                });
            }

            var result = await userManager.AddToRoleAsync(user, model.Role);

            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));

                return BadRequest(new AuthResponseDto
                {
                    Succeeded = false,
                    Message = errors
                });
            }

            return Ok(new AuthResponseDto
            {
                Succeeded = true,
                Message = $"Role {model.Role} assigned successfully."
            });
        }
    }
}