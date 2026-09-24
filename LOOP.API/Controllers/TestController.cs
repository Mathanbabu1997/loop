using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LOOP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        [HttpGet("public")]
        public IActionResult Public()
        {
            return Ok(new
            {
                message = "This endpoint is public."
            });
        }

        [Authorize]
        [HttpGet("protected")]
        public IActionResult Protected()
        {
            return Ok(new
            {
                message = "JWT authentication is working!",
                user = User.Identity?.Name,
                role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value,
                workspaceId = User.FindFirst("workspaceId")?.Value
            });
        }


        // ADMIN only
        [Authorize(Roles = "ADMIN")]
        [HttpGet("admin")]
        public IActionResult Admin()
        {
            return Ok(new
            {
                message = "ADMIN access granted.",
                user = User.Identity?.Name
            });
        }


        // ANALYST only
        [Authorize(Roles = "ANALYST")]
        [HttpGet("analyst")]
        public IActionResult Analyst()
        {
            return Ok(new
            {
                message = "ANALYST access granted.",
                user = User.Identity?.Name
            });
        }


        // VIEWER only
        [Authorize(Roles = "VIEWER")]
        [HttpGet("viewer")]
        public IActionResult Viewer()
        {
            return Ok(new
            {
                message = "VIEWER access granted.",
                user = User.Identity?.Name
            });
        }
    }
}