using LOOP.API.Data;
using LOOP.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LOOP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly LoopDbContext _context;

        public DashboardController(LoopDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboard()
        {
            var workspaceClaim = User.FindFirst("workspaceId")?.Value;

            if (!Guid.TryParse(workspaceClaim, out var workspaceId))
            {
                return Unauthorized();
            }

            // Workspace-scoped feedback
            var feedbackQuery = _context.Feedbacks
                .Where(f => f.WorkspaceId == workspaceId);

            // Total feedback
            var totalFeedback = await feedbackQuery.CountAsync();

            // Negative feedback percentage
            var negativeFeedback = await feedbackQuery
                .CountAsync(f => f.Sentiment == Sentiment.NEG);

            var negativePercentage = totalFeedback > 0
                ? Math.Round(
                    negativeFeedback * 100.0 / totalFeedback,
                    2)
                : 0;

            // New feedback this week
            var today = DateTime.UtcNow.Date;

            var startOfWeek = today.AddDays(
                -(int)today.DayOfWeek);

            var newThisWeek = await feedbackQuery
                .CountAsync(f =>
                    f.CreatedAt >= startOfWeek);

            // Sentiment breakdown
            var sentimentBreakdown = await feedbackQuery
                .GroupBy(f => f.Sentiment)
                .Select(g => new
                {
                    sentiment = g.Key.ToString(),
                    count = g.Count()
                })
                .ToListAsync();

            // Feedback volume by date
            var volumeOverTime = await feedbackQuery
                .GroupBy(f => f.CreatedAt.Date)
                .Select(g => new
                {
                    date = g.Key,
                    count = g.Count()
                })
                .OrderBy(x => x.date)
                .ToListAsync();

            // Top themes
            var topThemes = await _context.FeedbackThemes
                .Where(ft =>
                    ft.Feedback.WorkspaceId == workspaceId)
                .GroupBy(ft => new
                {
                    ft.ThemeId,
                    ft.Theme.Name
                })
                .Select(g => new
                {
                    themeId = g.Key.ThemeId,
                    themeName = g.Key.Name,
                    count = g.Count()
                })
                .OrderByDescending(x => x.count)
                .Take(10)
                .ToListAsync();

            return Ok(new
            {
                totalFeedback,
                negativePercentage,
                newThisWeek,
                sentimentBreakdown,
                volumeOverTime,
                topThemes
            });
        }
    }
}