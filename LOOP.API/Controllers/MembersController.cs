using LOOP.API.Data;
using LOOP.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LOOP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "ADMIN")]
    public class MembersController : ControllerBase
    {
        private readonly LoopDbContext _context;

        public MembersController(LoopDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // GET: api/Members
        // Get all members in current workspace
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> GetMembers()
        {
            var workspaceId = GetWorkspaceId();

            if (workspaceId == null)
            {
                return Unauthorized(new
                {
                    message = "Workspace not found."
                });
            }

            var members = await _context.Users
                .AsNoTracking()
                .Where(u => u.WorkspaceId == workspaceId.Value)
                .OrderBy(u => u.Name)
                .Select(u => new
                {
                    id = u.Id,
                    name = u.Name,
                    email = u.Email,
                    role = u.Role.ToString()
                })
                .ToListAsync();

            return Ok(members);
        }

        // =====================================================
        // POST: api/Members
        // Create new member
        // =====================================================

        [HttpPost]
        public async Task<IActionResult> CreateMember(
            [FromBody] CreateMemberRequest request)
        {
            // IMPORTANT:
            // Workspace comes from logged-in ADMIN JWT.
            // Frontend does NOT send workspaceId.

            var workspaceId = GetWorkspaceId();

            if (workspaceId == null)
            {
                return Unauthorized(new
                {
                    message = "Workspace not found."
                });
            }

            // =================================================
            // Validate request
            // =================================================

            if (request == null)
            {
                return BadRequest(new
                {
                    message = "Invalid request."
                });
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new
                {
                    message = "Name is required."
                });
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new
                {
                    message = "Email is required."
                });
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new
                {
                    message = "Password is required."
                });
            }

            if (request.Password.Length < 6)
            {
                return BadRequest(new
                {
                    message =
                        "Password must contain at least 6 characters."
                });
            }

            if (string.IsNullOrWhiteSpace(request.Role))
            {
                return BadRequest(new
                {
                    message = "Role is required."
                });
            }

            // =================================================
            // Validate role
            // =================================================

            if (!Enum.TryParse<UserRole>(
                request.Role.Trim(),
                true,
                out var newRole))
            {
                return BadRequest(new
                {
                    message =
                        "Invalid role. Allowed roles are ANALYST and VIEWER."
                });
            }

            // ADMIN cannot create another ADMIN
            if (newRole == UserRole.ADMIN)
            {
                return BadRequest(new
                {
                    message =
                        "New members can only be ANALYST or VIEWER."
                });
            }

            // =================================================
            // Normalize email
            // =================================================

            var normalizedEmail =
                request.Email.Trim().ToLowerInvariant();

            // =================================================
            // Check duplicate email
            // =================================================

            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.Email.ToLower() == normalizedEmail);

            if (existingUser != null)
            {
                return BadRequest(new
                {
                    message =
                        "This email is already registered."
                });
            }

            // =================================================
            // Create member
            // =================================================

            var member = new User
            {
                Id = Guid.NewGuid(),

                Name = request.Name.Trim(),

                Email = normalizedEmail,

                PasswordHash =
                    BCrypt.Net.BCrypt.HashPassword(
                        request.Password
                    ),

                Role = newRole,

                // IMPORTANT:
                // Assign logged-in ADMIN's workspace.
                WorkspaceId = workspaceId.Value
            };

            _context.Users.Add(member);

            await _context.SaveChangesAsync();

            // =================================================
            // Response
            // =================================================

            return Ok(new
            {
                message = "Member created successfully.",

                memberId = member.Id,

                name = member.Name,

                email = member.Email,

                role = member.Role.ToString(),

                workspaceId = member.WorkspaceId
            });
        }

        // =====================================================
        // PUT: api/Members/{id}/role
        // Change member role
        // =====================================================

        [HttpPut("{id:guid}/role")]
        public async Task<IActionResult> ChangeRole(
            Guid id,
            [FromBody] ChangeRoleRequest request)
        {
            var workspaceId = GetWorkspaceId();

            if (workspaceId == null)
            {
                return Unauthorized(new
                {
                    message = "Workspace not found."
                });
            }

            if (request == null ||
                string.IsNullOrWhiteSpace(request.Role))
            {
                return BadRequest(new
                {
                    message = "Role is required."
                });
            }

            if (!Enum.TryParse<UserRole>(
                request.Role.Trim(),
                true,
                out var newRole))
            {
                return BadRequest(new
                {
                    message =
                        "Invalid role. Allowed roles are ADMIN, ANALYST, VIEWER."
                });
            }

            // Only find member inside current workspace
            var member = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.Id == id &&
                    u.WorkspaceId == workspaceId.Value);

            if (member == null)
            {
                return NotFound(new
                {
                    message = "Member not found."
                });
            }

            if (member.Role == newRole)
            {
                return BadRequest(new
                {
                    message =
                        "Member already has this role."
                });
            }

            member.Role = newRole;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message =
                    "Member role updated successfully.",

                memberId = member.Id,

                name = member.Name,

                email = member.Email,

                role = member.Role.ToString()
            });
        }

        // =====================================================
        // GET WORKSPACE ID FROM JWT
        // =====================================================

        private Guid? GetWorkspaceId()
        {
            var workspaceClaim =
                User.FindFirst("workspaceId")?.Value;

            if (Guid.TryParse(
                workspaceClaim,
                out var workspaceId))
            {
                return workspaceId;
            }

            return null;
        }
    }

    // =========================================================
    // CREATE MEMBER REQUEST
    // =========================================================

    public class CreateMemberRequest
    {
        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;
    }

    // =========================================================
    // CHANGE ROLE REQUEST
    // =========================================================

    public class ChangeRoleRequest
    {
        public string Role { get; set; } = string.Empty;
    }
}