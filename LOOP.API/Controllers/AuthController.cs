using LOOP.API.Data;
using LOOP.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace LOOP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly LoopDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(
            LoopDbContext context,
            IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // POST: api/auth/signup
        [HttpPost("signup")]
        public async Task<IActionResult> Signup(SignupRequest request)
        {
            // Check whether email already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (existingUser != null)
            {
                return BadRequest(new
                {
                    message = "Email already registered."
                });
            }

            // Create new workspace
            var workspace = new Workspace
            {
                Id = Guid.NewGuid(),
                Name = request.WorkspaceName,
                CreatedAt = DateTime.UtcNow
            };

            // Hash password
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(
                request.Password
            );

            // Create first user as ADMIN
            var user = new User
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Email = request.Email,
                PasswordHash = passwordHash,
                Role = UserRole.ADMIN,
                WorkspaceId = workspace.Id
            };

            _context.Workspaces.Add(workspace);
            _context.Users.Add(user);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Signup successful.",
                userId = user.Id,
                workspaceId = user.WorkspaceId,
                role = user.Role.ToString()
            });
        }

        // POST: api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            // Find user by email
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
            {
                return Unauthorized(new
                {
                    message = "Invalid email or password."
                });
            }

            // Verify password
            var passwordValid = BCrypt.Net.BCrypt.Verify(
                request.Password,
                user.PasswordHash
            );

            if (!passwordValid)
            {
                return Unauthorized(new
                {
                    message = "Invalid email or password."
                });
            }

            // Create JWT claims
            var claims = new List<Claim>
            {
                new Claim(
                    JwtRegisteredClaimNames.Sub,
                    user.Id.ToString()
                ),

                new Claim(
                    ClaimTypes.Name,
                    user.Name
                ),

                new Claim(
                    ClaimTypes.Email,
                    user.Email
                ),

                new Claim(
                    ClaimTypes.Role,
                    user.Role.ToString()
                ),

                new Claim(
                    "workspaceId",
                    user.WorkspaceId.ToString()
                )
            };

            // Get JWT configuration
            var jwtKey = _configuration["Jwt:Key"];

            if (string.IsNullOrWhiteSpace(jwtKey))
            {
                return StatusCode(500, new
                {
                    message = "JWT key is not configured."
                });
            }

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)
            );

            var credentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256
            );

            // Create JWT token
            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: credentials
            );

            var tokenString = new JwtSecurityTokenHandler()
                .WriteToken(token);

            // Return login response
            return Ok(new
            {
                message = "Login successful.",
                token = tokenString,
                userId = user.Id,
                name = user.Name,
                email = user.Email,
                role = user.Role.ToString(),
                workspaceId = user.WorkspaceId
            });
        }
    }

    // Signup request model
    public class SignupRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string WorkspaceName { get; set; } = string.Empty;
    }   

    // Login request model
    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}