using LOOP.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LOOP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ThemeController : ControllerBase
    {
        private readonly LoopDbContext _context;

        public ThemeController(LoopDbContext context)
        {
            _context = context;
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllThemes()
        {
            var themes = await _context.Themes
                .Select(t => new
                {
                    t.Id,
                    t.Name,
                    t.WorkspaceId
                })
                .ToListAsync();

            return Ok(themes);
        }
    }
}