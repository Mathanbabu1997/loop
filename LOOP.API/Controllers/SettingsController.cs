using LOOP.API.Data;
using LOOP.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LOOP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SettingsController : ControllerBase
    {
        private readonly LoopDbContext _context;

        public SettingsController(LoopDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // GET: api/Settings/profile
        // Get current logged-in user's profile
        // =========================================================

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var userId = GetUserId();

                if (userId == null)
                {
                    return Unauthorized(new
                    {
                        message = "User not found."
                    });
                }

                var user = await _context.Users
                    .AsNoTracking()
                    .Include(u => u.Workspace)
                    .FirstOrDefaultAsync(
                        u => u.Id == userId.Value);

                if (user == null)
                {
                    return NotFound(new
                    {
                        message = "User not found."
                    });
                }

                return Ok(new
                {
                    id = user.Id,
                    name = user.Name,
                    email = user.Email,
                    role = user.Role.ToString(),
                    workspaceId = user.WorkspaceId,
                    workspaceName =
                        user.Workspace != null
                            ? user.Workspace.Name
                            : ""
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "GET PROFILE ERROR");

                Console.WriteLine(
                    ex.ToString());

                return StatusCode(500, new
                {
                    message =
                        "Unable to load profile.",
                    error =
                        ex.Message
                });
            }
        }


        // =========================================================
        // PUT: api/Settings/profile
        // Update current user's name
        // =========================================================

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile(
            [FromBody] UpdateProfileRequest request)
        {
            try
            {
                var userId = GetUserId();

                if (userId == null)
                {
                    return Unauthorized(new
                    {
                        message = "User not found."
                    });
                }

                var user = await _context.Users
                    .FirstOrDefaultAsync(
                        u => u.Id == userId.Value);

                if (user == null)
                {
                    return NotFound(new
                    {
                        message = "User not found."
                    });
                }


                // =================================================
                // VALIDATE NAME
                // =================================================

                if (string.IsNullOrWhiteSpace(
                    request.Name))
                {
                    return BadRequest(new
                    {
                        message =
                            "Name cannot be empty."
                    });
                }

                var newName =
                    request.Name.Trim();


                if (newName.Length < 2)
                {
                    return BadRequest(new
                    {
                        message =
                            "Name must be at least 2 characters."
                    });
                }


                if (newName.Length > 100)
                {
                    return BadRequest(new
                    {
                        message =
                            "Name cannot exceed 100 characters."
                    });
                }


                // =================================================
                // CHECK SAME NAME
                // =================================================

                if (string.Equals(
                    user.Name?.Trim(),
                    newName,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new
                    {
                        message =
                            "No changes were made."
                    });
                }


                // =================================================
                // UPDATE NAME
                // =================================================

                user.Name = newName;

                await _context.SaveChangesAsync();


                return Ok(new
                {
                    message =
                        "Profile updated successfully.",

                    name =
                        user.Name,

                    email =
                        user.Email,

                    role =
                        user.Role.ToString(),

                    workspaceId =
                        user.WorkspaceId
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "UPDATE PROFILE ERROR");

                Console.WriteLine(
                    ex.ToString());

                return StatusCode(500, new
                {
                    message =
                        "Unable to update profile.",

                    error =
                        ex.Message
                });
            }
        }


        // =========================================================
        // PUT: api/Settings/password
        // Change current user's password
        // =========================================================

        [HttpPut("password")]
        public async Task<IActionResult> ChangePassword(
            [FromBody] ChangePasswordRequest request)
        {
            try
            {
                var userId = GetUserId();

                if (userId == null)
                {
                    return Unauthorized(new
                    {
                        message = "User not found."
                    });
                }


                var user = await _context.Users
                    .FirstOrDefaultAsync(
                        u => u.Id == userId.Value);

                if (user == null)
                {
                    return NotFound(new
                    {
                        message = "User not found."
                    });
                }


                // =================================================
                // CURRENT PASSWORD VALIDATION
                // =================================================

                if (string.IsNullOrWhiteSpace(
                    request.CurrentPassword))
                {
                    return BadRequest(new
                    {
                        message =
                            "Current password is required."
                    });
                }


                // =================================================
                // NEW PASSWORD VALIDATION
                // =================================================

                if (string.IsNullOrWhiteSpace(
                    request.NewPassword))
                {
                    return BadRequest(new
                    {
                        message =
                            "New password is required."
                    });
                }


                if (request.NewPassword.Length < 8)
                {
                    return BadRequest(new
                    {
                        message =
                            "New password must be at least 8 characters."
                    });
                }


                if (request.NewPassword.Length > 100)
                {
                    return BadRequest(new
                    {
                        message =
                            "New password cannot exceed 100 characters."
                    });
                }


                // =================================================
                // CONFIRM PASSWORD
                // =================================================

                if (string.IsNullOrWhiteSpace(
                    request.ConfirmPassword))
                {
                    return BadRequest(new
                    {
                        message =
                            "Confirm password is required."
                    });
                }


                if (request.NewPassword !=
                    request.ConfirmPassword)
                {
                    return BadRequest(new
                    {
                        message =
                            "New password and confirmation password do not match."
                    });
                }


                // =================================================
                // VERIFY CURRENT PASSWORD
                // =================================================

                var currentPasswordValid =
                    BCrypt.Net.BCrypt.Verify(
                        request.CurrentPassword,
                        user.PasswordHash);


                if (!currentPasswordValid)
                {
                    return BadRequest(new
                    {
                        message =
                            "Current password is incorrect."
                    });
                }


                // =================================================
                // CHECK NEW PASSWORD SAME AS CURRENT
                // =================================================

                var sameAsCurrentPassword =
                    BCrypt.Net.BCrypt.Verify(
                        request.NewPassword,
                        user.PasswordHash);


                if (sameAsCurrentPassword)
                {
                    return BadRequest(new
                    {
                        message =
                            "New password cannot be the same as your current password."
                    });
                }


                // =================================================
                // HASH NEW PASSWORD
                // =================================================

                user.PasswordHash =
                    BCrypt.Net.BCrypt.HashPassword(
                        request.NewPassword);


                await _context.SaveChangesAsync();


                return Ok(new
                {
                    message =
                        "Password changed successfully."
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "CHANGE PASSWORD ERROR");

                Console.WriteLine(
                    ex.ToString());

                return StatusCode(500, new
                {
                    message =
                        "Unable to change password.",

                    error =
                        ex.Message
                });
            }
        }


        // =========================================================
        // GET USER ID FROM JWT
        // =========================================================

        private Guid? GetUserId()
        {
            var claim =
                User.FindFirst(
                    ClaimTypes.NameIdentifier)?.Value
                ??
                User.FindFirst("sub")?.Value;


            if (Guid.TryParse(
                claim,
                out var userId))
            {
                return userId;
            }


            return null;
        }
    }


    // =============================================================
    // UPDATE PROFILE REQUEST
    // =============================================================

    public class UpdateProfileRequest
    {
        public string Name { get; set; }
            = string.Empty;
    }


    // =============================================================
    // CHANGE PASSWORD REQUEST
    // =============================================================

    public class ChangePasswordRequest
    {
        public string CurrentPassword { get; set; }
            = string.Empty;

        public string NewPassword { get; set; }
            = string.Empty;

        public string ConfirmPassword { get; set; }
            = string.Empty;
    }
}