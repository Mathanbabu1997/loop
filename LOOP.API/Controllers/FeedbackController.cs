using LOOP.API.Data;
using LOOP.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using LOOP.API.DTOs;
using CsvHelper;
using System.Globalization;

namespace LOOP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FeedbackController : ControllerBase
    {
        private readonly LoopDbContext _context;

        public FeedbackController(LoopDbContext context)
        {
            _context = context;
        }

      
        // =====================================================
        // GET: api/Feedback/{id}
        // Get one feedback
        // =====================================================

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var workspaceId = GetWorkspaceId();

            if (workspaceId == null)
            {
                return Unauthorized();
            }

            var feedback = await _context.Feedbacks
                .FirstOrDefaultAsync(f =>
                    f.Id == id &&
                    f.WorkspaceId == workspaceId.Value);

            if (feedback == null)
            {
                return NotFound();
            }

            return Ok(feedback);
        }


        // =====================================================
        // POST: api/Feedback
        // ANALYST and ADMIN can create feedback
        // =====================================================

        [HttpPost]
        [Authorize(Roles = "ADMIN,ANALYST")]
        public async Task<IActionResult> Create(CreateFeedbackRequest request)
        {
            var workspaceId = GetWorkspaceId();

            if (workspaceId == null)
            {
                return Unauthorized();
            }

            var feedback = new Feedback
            {
                Id = Guid.NewGuid(),
                Content = request.Content,
                Channel = request.Channel,
                SourceRef = request.SourceRef,
                CustomerLabel = request.CustomerLabel,
                Sentiment = request.Sentiment,
                SentimentScore = request.SentimentScore,
                Status = FeedbackStatus.NEW,
                CreatedAt = DateTime.UtcNow,
                WorkspaceId = workspaceId.Value
            };

            _context.Feedbacks.Add(feedback);

            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetById),
                new { id = feedback.Id },
                feedback
            );
        }


        // =====================================================
        // PUT: api/Feedback/{id}
        // ANALYST and ADMIN can update feedback
        // =====================================================

        // =====================================================
        // PUT: api/Feedback/{id}
        // ANALYST and ADMIN can update feedback
        // =====================================================

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "ADMIN,ANALYST")]
        public async Task<IActionResult> Update(
    Guid id,
    [FromBody] UpdateFeedbackRequest request)
        {
            var workspaceId = GetWorkspaceId();

            if (workspaceId == null)
            {
                return Unauthorized();
            }

            var feedback = await _context.Feedbacks
                .FirstOrDefaultAsync(f =>
                    f.Id == id &&
                    f.WorkspaceId == workspaceId.Value);

            if (feedback == null)
            {
                return NotFound();
            }

            feedback.Content = request.Content;
            feedback.Channel = request.Channel;
            feedback.SourceRef = request.SourceRef;
            feedback.CustomerLabel = request.CustomerLabel;
            feedback.Sentiment = request.Sentiment;
            feedback.SentimentScore = request.SentimentScore;
            feedback.Status = request.Status;

            await _context.SaveChangesAsync();

            return Ok(feedback);
        }

        // =====================================================
        // DELETE: api/Feedback/{id}
        // ADMIN and ANALYST can delete feedback
        // =====================================================

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "ADMIN,ANALYST")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var workspaceId = GetWorkspaceId();

            if (workspaceId == null)
            {
                return Unauthorized();
            }

            var feedback = await _context.Feedbacks
                .FirstOrDefaultAsync(f =>
                    f.Id == id &&
                    f.WorkspaceId == workspaceId.Value);

            if (feedback == null)
            {
                return NotFound();
            }

            _context.Feedbacks.Remove(feedback);

            await _context.SaveChangesAsync();

            return NoContent();
        }


        // =====================================================
        // Get Workspace ID from JWT
        // =====================================================

        private Guid? GetWorkspaceId()
        {
            var workspaceClaim = User.FindFirst("workspaceId")?.Value;

            if (Guid.TryParse(workspaceClaim, out var workspaceId))
            {
                return workspaceId;
            }

            return null;
        }

        [HttpPost("import")]
        [Authorize(Roles = "ADMIN,ANALYST")]
        public async Task<IActionResult> ImportCsv(IFormFile file)
        {
            var workspaceId = GetWorkspaceId();

            if (workspaceId == null)
            {
                return Unauthorized();
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest(new
                {
                    message = "Please upload a CSV file."
                });
            }

            if (!Path.GetExtension(file.FileName)
                .Equals(".csv", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    message = "Only CSV files are allowed."
                });
            }

            var successful = 0;
            var failed = 0;
            var errors = new List<string>();

            using var stream = file.OpenReadStream();

            using var reader = new StreamReader(stream);

            using var csv = new CsvReader(
                reader,
                CultureInfo.InvariantCulture
            );

            var records = csv.GetRecords<CsvFeedbackRow>();

            foreach (var record in records)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(record.Content))
                    {
                        failed++;

                        errors.Add("Content is required.");

                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(record.Channel))
                    {
                        failed++;

                        errors.Add(
                            $"Channel is required for: {record.Content}"
                        );

                        continue;
                    }

                    var feedback = new Feedback
                    {
                        Id = Guid.NewGuid(),

                        Content = record.Content,

                        Channel = record.Channel,

                        CustomerLabel = record.CustomerLabel,

                        CreatedAt = record.CreatedAt.HasValue
    ? DateTime.SpecifyKind(
        record.CreatedAt.Value,
        DateTimeKind.Utc)
    : DateTime.UtcNow,

                        Sentiment = Sentiment.NEU,

                        SentimentScore = 0,

                        Status = FeedbackStatus.NEW,

                        WorkspaceId = workspaceId.Value
                    };

                    _context.Feedbacks.Add(feedback);

                    successful++;
                }
                catch (Exception ex)
                {
                    failed++;

                    errors.Add(ex.Message);
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "CSV import completed.",

                successful,

                failed,

                errors
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
    int page = 1,
int pageSize = 10,
string? search = null,
string? channel = null,
Sentiment? sentiment = null,
FeedbackStatus? status = null,
DateTime? fromDate = null,
    DateTime? toDate = null,
    Guid? themeId = null)
        {
            var workspaceId = GetWorkspaceId();

            if (workspaceId == null)
            {
                return Unauthorized();
            }

            if (page < 1)
            {
                page = 1;
            }

            if (pageSize < 1)
            {
                pageSize = 10;
            }

            if (pageSize > 100)
            {
                pageSize = 100;
            }

            var query = _context.Feedbacks
                .Where(f => f.WorkspaceId == workspaceId.Value);

            // Search
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(f =>
                    f.Content.Contains(search) ||
                    (f.CustomerLabel != null &&
                     f.CustomerLabel.Contains(search)));
            }

            // Channel filter
            if (!string.IsNullOrWhiteSpace(channel))
            {
                query = query.Where(f =>
                    f.Channel == channel);
            }

            // Sentiment filter
            if (sentiment.HasValue)
            {
                query = query.Where(f =>
                    f.Sentiment == sentiment.Value);
            }

            // Status filter
            if (status.HasValue)
            {
                query = query.Where(f =>
                    f.Status == status.Value);
            }
            // From date filter
            if (fromDate.HasValue)
            {
                query = query.Where(f =>
                    f.CreatedAt >= fromDate.Value);
            }

            // To date filter
            if (toDate.HasValue)
            {
                var endDate = toDate.Value.Date.AddDays(1);

                query = query.Where(f =>
                    f.CreatedAt < endDate);
            }

            // Theme filter
            if (themeId.HasValue)
            {
                query = query.Where(f =>
                    f.FeedbackThemes.Any(ft =>
                        ft.ThemeId == themeId.Value));
            }
            var totalItems = await query.CountAsync();

            var totalPages = (int)Math.Ceiling(
                totalItems / (double)pageSize
            );

            var feedback = await query
                .OrderByDescending(f => f.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(f => new FeedbackResponse
                {
                    Id = f.Id,
                    Content = f.Content,
                    Channel = f.Channel,
                    CustomerLabel = f.CustomerLabel,
                    Sentiment = f.Sentiment.ToString(),
                    SentimentScore = f.SentimentScore,
                    Status = f.Status.ToString(),
                    CreatedAt = f.CreatedAt
                })
                .ToListAsync();

            var response = new FeedbackListResponse
            {
                Items = feedback,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            };

            return Ok(response);
        }
    }
}