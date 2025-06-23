using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FHIRWebApi.Api.Controllers
{
    /// <summary>
    /// Handles user authentication and issues JWT tokens.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Injects application configuration for accessing JWT settings.
        /// </summary>
        /// <param name="configuration">Application configuration</param>
        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Represents a login request payload.
        /// </summary>
        public class LoginRequest
        {
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        /// <summary>
        /// Represents the response returned after successful authentication.
        /// </summary>
        public class AuthResponse
        {
            public string Token { get; set; } = string.Empty;
            public DateTime Expires { get; set; }
        }

        /// <summary>
        /// Authenticates a user and issues a JWT token if credentials are valid.
        /// </summary>
        /// <param name="request">The login credentials</param>
        /// <returns>
        /// 200 OK with JWT token and expiration if credentials are valid;
        /// 401 Unauthorized otherwise.
        /// </returns>
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            // TODO: Replace with actual user validation logic
            if (request.Username != "FhirDev" || request.Password != "@ppl3314")
                return Unauthorized("Invalid credentials");

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, request.Username),
                    new Claim(ClaimTypes.Role, "Admin")
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var jwt = tokenHandler.WriteToken(token);

            return Ok(new AuthResponse
            {
                Token = jwt,
                Expires = tokenDescriptor.Expires ?? DateTime.UtcNow.AddHours(1)
            });
        }
    }
}
