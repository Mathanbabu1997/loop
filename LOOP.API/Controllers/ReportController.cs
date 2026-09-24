using LOOP.API.Data;
using LOOP.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace LOOP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReportController : ControllerBase
    {
        private readonly LoopDbContext _context;
        private readonly IConfiguration _configuration;

        public ReportController(
            LoopDbContext context,
            IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // =========================================================
        // GET: api/Report/saved
        // Get previously saved reports
        // =========================================================

        [HttpGet("saved")]
        public async Task<IActionResult> GetSavedReports()
        {
            try
            {
                var workspaceId = GetWorkspaceId();

                if (workspaceId == null)
                {
                    return Unauthorized(new
                    {
                        message = "Workspace not found."
                    });
                }

                var reports = await _context.Reports
                    .AsNoTracking()
                    .Where(r =>
                        r.WorkspaceId == workspaceId.Value)
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => new
                    {
                        id = r.Id,
                        title = r.Title,
                        periodStart = r.PeriodStart,
                        periodEnd = r.PeriodEnd,
                        createdAt = r.CreatedAt
                    })
                    .ToListAsync();

                return Ok(reports);
            }
            catch (Exception ex)
            {
                Console.WriteLine("GET SAVED REPORTS ERROR");
                Console.WriteLine(ex.ToString());

                return StatusCode(500, new
                {
                    message = "Unable to load saved reports.",
                    error = ex.Message
                });
            }
        }

        // =========================================================
        // GET: api/Report/{id}
        // Get one saved report
        // =========================================================

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetReport(Guid id)
        {
            try
            {
                var workspaceId = GetWorkspaceId();

                if (workspaceId == null)
                {
                    return Unauthorized(new
                    {
                        message = "Workspace not found."
                    });
                }

                var report = await _context.Reports
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r =>
                        r.Id == id &&
                        r.WorkspaceId == workspaceId.Value);

                if (report == null)
                {
                    return NotFound(new
                    {
                        message = "Report not found."
                    });
                }

                ReportContent? content = null;

                try
                {
                    content =
                        JsonSerializer.Deserialize<ReportContent>(
                            report.ContentJson);
                }
                catch
                {
                    content = new ReportContent();
                }

                return Ok(new
                {
                    id = report.Id,
                    title = report.Title,
                    periodStart = report.PeriodStart,
                    periodEnd = report.PeriodEnd,
                    createdAt = report.CreatedAt,
                    content
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("GET REPORT ERROR");
                Console.WriteLine(ex.ToString());

                return StatusCode(500, new
                {
                    message = "Unable to load report.",
                    error = ex.Message
                });
            }
        }

        // =========================================================
        // POST: api/Report/generate
        // Generate + save report
        // =========================================================

        [HttpPost("generate")]
        public async Task<IActionResult> GenerateReport(
            [FromBody] GenerateReportRequest request)
        {
            try
            {
                // =================================================
                // 1. WORKSPACE
                // =================================================

                var workspaceId = GetWorkspaceId();

                if (workspaceId == null)
                {
                    return Unauthorized(new
                    {
                        message = "Workspace not found."
                    });
                }

                // =================================================
                // 2. USER
                // =================================================

                var userIdClaim =
                    User.FindFirst(
                        ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("sub")?.Value;

                if (!Guid.TryParse(
                    userIdClaim,
                    out var userId))
                {
                    return Unauthorized(new
                    {
                        message = "User not found."
                    });
                }

                // =================================================
                // 3. VALIDATE REQUEST
                // =================================================

                if (request.StartDate == default ||
                    request.EndDate == default)
                {
                    return BadRequest(new
                    {
                        message =
                            "Start date and end date are required."
                    });
                }

                if (request.EndDate < request.StartDate)
                {
                    return BadRequest(new
                    {
                        message =
                            "End date cannot be before start date."
                    });
                }

                // =================================================
                // 4. NORMALIZE DATES AS UTC
                //
                // IMPORTANT:
                // PostgreSQL timestamp with time zone requires UTC.
                // DateTime from JSON can arrive as Kind=Unspecified.
                // =================================================

                var requestStartUtc =
                    DateTime.SpecifyKind(
                        request.StartDate,
                        DateTimeKind.Utc);

                var requestEndUtc =
                    DateTime.SpecifyKind(
                        request.EndDate,
                        DateTimeKind.Utc);

                var periodStart =
                    DateTime.SpecifyKind(
                        requestStartUtc.Date,
                        DateTimeKind.Utc);

                // Exclusive end date for database query
                var periodEndExclusive =
                    DateTime.SpecifyKind(
                        requestEndUtc.Date.AddDays(1),
                        DateTimeKind.Utc);

                // Date shown/saved in the report
                var reportPeriodEnd =
                    DateTime.SpecifyKind(
                        requestEndUtc.Date,
                        DateTimeKind.Utc);

                // =================================================
                // 5. GET FEEDBACK FOR SELECTED PERIOD
                // =================================================

                var feedback =
                    await _context.Feedbacks
                        .AsNoTracking()
                        .Where(f =>
                            f.WorkspaceId ==
                                workspaceId.Value &&
                            f.CreatedAt >= periodStart &&
                            f.CreatedAt < periodEndExclusive)
                        .Select(f =>
                            new FeedbackReportItem
                            {
                                Id = f.Id,

                                Content =
                                    f.Content,

                                Channel =
                                    f.Channel,

                                CustomerLabel =
                                    f.CustomerLabel,

                                Sentiment =
                                    f.Sentiment,

                                SentimentScore =
                                    f.SentimentScore,

                                CreatedAt =
                                    f.CreatedAt,

                                Themes =
                                    f.FeedbackThemes
                                        .Select(ft =>
                                            new ThemeReportItem
                                            {
                                                Name =
                                                    ft.Theme.Name,

                                                Confidence =
                                                    ft.Confidence
                                            })
                                        .ToList()
                            })
                        .ToListAsync();

                // =================================================
                // 6. NO FEEDBACK
                // =================================================

                if (feedback.Count == 0)
                {
                    return BadRequest(new
                    {
                        message =
                            "There is no feedback in the selected period."
                    });
                }

                // =================================================
                // 7. PREVIOUS PERIOD
                // =================================================

                var days =
                    (periodEndExclusive -
                     periodStart).Days;

                var previousEnd =
                    periodStart;

                var previousStart =
                    DateTime.SpecifyKind(
                        periodStart.AddDays(-days),
                        DateTimeKind.Utc);

                var previousFeedback =
                    await _context.Feedbacks
                        .AsNoTracking()
                        .Where(f =>
                            f.WorkspaceId ==
                                workspaceId.Value &&
                            f.CreatedAt >= previousStart &&
                            f.CreatedAt < previousEnd)
                        .Select(f => f.Sentiment)
                        .ToListAsync();

                // =================================================
                // 8. CURRENT SENTIMENT COUNTS
                // =================================================

                var positive =
                    feedback.Count(f =>
                        f.Sentiment ==
                            Sentiment.POS);

                var neutral =
                    feedback.Count(f =>
                        f.Sentiment ==
                            Sentiment.NEU);

                var negative =
                    feedback.Count(f =>
                        f.Sentiment ==
                            Sentiment.NEG);

                var total =
                    feedback.Count;

                // =================================================
                // 9. CURRENT SENTIMENT PERCENTAGES
                // =================================================

                var positivePercentage =
                    Percentage(
                        positive,
                        total);

                var neutralPercentage =
                    Percentage(
                        neutral,
                        total);

                var negativePercentage =
                    Percentage(
                        negative,
                        total);

                // =================================================
                // 10. PREVIOUS SENTIMENT
                // =================================================

                var previousTotal =
                    previousFeedback.Count;

                var previousPositive =
                    previousFeedback.Count(s =>
                        s == Sentiment.POS);

                var previousNeutral =
                    previousFeedback.Count(s =>
                        s == Sentiment.NEU);

                var previousNegative =
                    previousFeedback.Count(s =>
                        s == Sentiment.NEG);

                var previousPositivePercentage =
                    Percentage(
                        previousPositive,
                        previousTotal);

                var previousNeutralPercentage =
                    Percentage(
                        previousNeutral,
                        previousTotal);

                var previousNegativePercentage =
                    Percentage(
                        previousNegative,
                        previousTotal);

                // =================================================
                // 11. SENTIMENT SHIFT
                // =================================================

                var positiveShift =
                    Math.Round(
                        positivePercentage -
                        previousPositivePercentage,
                        1);

                var neutralShift =
                    Math.Round(
                        neutralPercentage -
                        previousNeutralPercentage,
                        1);

                var negativeShift =
                    Math.Round(
                        negativePercentage -
                        previousNegativePercentage,
                        1);

                // =================================================
                // 12. TOP THEMES
                // =================================================

                var topThemes =
                    feedback
                        .SelectMany(f =>
                            f.Themes
                                .Where(t =>
                                    !string.IsNullOrWhiteSpace(
                                        t.Name))
                                .Select(t =>
                                    t.Name))
                        .GroupBy(
                            name => name,
                            StringComparer
                                .OrdinalIgnoreCase)
                        .Select(g =>
                            new ThemeSummary
                            {
                                Name =
                                    g.First(),

                                Count =
                                    g.Count(),

                                Percentage =
                                    Math.Round(
                                        g.Count() *
                                        100.0 /
                                        total,
                                        1)
                            })
                        .OrderByDescending(t =>
                            t.Count)
                        .Take(10)
                        .ToList();

                // =================================================
                // 13. CHANNELS
                // =================================================

                var channels =
                    feedback
                        .GroupBy(f =>
                            string.IsNullOrWhiteSpace(
                                f.Channel)
                                ? "Unknown"
                                : f.Channel)
                        .Select(g =>
                            new ChannelSummary
                            {
                                Name =
                                    g.Key,

                                Count =
                                    g.Count(),

                                Percentage =
                                    Math.Round(
                                        g.Count() *
                                        100.0 /
                                        total,
                                        1)
                            })
                        .OrderByDescending(c =>
                            c.Count)
                        .ToList();

                // =================================================
                // 14. NOTABLE CUSTOMER QUOTES
                // =================================================

                var quotes =
    feedback
        .Where(f =>
            !string.IsNullOrWhiteSpace(
                f.Content))
        .OrderByDescending(f =>
            f.CreatedAt)
        .Select(f => new CustomerQuote
        {
            Content =
                f.Content,

            Sentiment =
                GetSentimentName(
                    f.Sentiment),

            Customer =
                string.IsNullOrWhiteSpace(
                    f.CustomerLabel)
                    ? "Customer"
                    : f.CustomerLabel,

            Channel =
                string.IsNullOrWhiteSpace(
                    f.Channel)
                    ? "Unknown"
                    : f.Channel,

            Date =
                f.CreatedAt
                    .ToString(
                        "yyyy-MM-dd")
        })
        .ToList();
                // =================================================
                // 15. RECOMMENDED ACTIONS
                // =================================================

                var actions =
                    BuildRecommendedActions(
                        topThemes,
                        feedback);

                // =================================================
                // 16. BASIC EXECUTIVE SUMMARY
                // =================================================

                var topTheme =
                    topThemes
                        .FirstOrDefault()
                        ?.Name
                    ?? "No dominant theme";

                var summary =
                    $"During the selected period, LOOP analyzed " +
                    $"{total} customer feedback items. " +
                    $"{positivePercentage}% were positive, " +
                    $"{neutralPercentage}% were neutral, and " +
                    $"{negativePercentage}% were negative. " +
                    $"The most frequent theme was {topTheme}.";

                // =================================================
                // 17. OPTIONAL GROQ AI NARRATIVE
                // =================================================

                var aiNarrative =
                    await GenerateAiNarrative(
                        total,
                        positivePercentage,
                        neutralPercentage,
                        negativePercentage,
                        positiveShift,
                        neutralShift,
                        negativeShift,
                        topThemes,
                        quotes,
                        actions);

                if (!string.IsNullOrWhiteSpace(
                    aiNarrative))
                {
                    summary =
                        aiNarrative;
                }

                // =================================================
                // 18. BUILD REPORT CONTENT
                // =================================================

                var content =
                    new ReportContent
                    {
                        Summary =
                            summary,

                        TotalFeedback =
                            total,

                        Sentiment =
                            new SentimentSummary
                            {
                                Positive =
                                    positive,

                                Neutral =
                                    neutral,

                                Negative =
                                    negative,

                                PositivePercentage =
                                    positivePercentage,

                                NeutralPercentage =
                                    neutralPercentage,

                                NegativePercentage =
                                    negativePercentage
                            },

                        SentimentShift =
                            new SentimentShift
                            {
                                Positive =
                                    positiveShift,

                                Neutral =
                                    neutralShift,

                                Negative =
                                    negativeShift
                            },

                        TopThemes =
                            topThemes,

                        Channels =
                            channels,

                        Quotes =
                            quotes,

                        RecommendedActions =
                            actions
                    };

                // =================================================
                // 19. SAVE REPORT
                // =================================================

                var report =
                    new Report
                    {
                        Id =
                            Guid.NewGuid(),

                        Title =
                            $"Voice of Customer Report - " +
                            $"{requestStartUtc:yyyy-MM-dd} to " +
                            $"{requestEndUtc:yyyy-MM-dd}",

                        // IMPORTANT:
                        // Use UTC DateTimes here.
                        PeriodStart =
                            periodStart,

                        PeriodEnd =
                            reportPeriodEnd,

                        ContentJson =
                            JsonSerializer.Serialize(
                                content),

                        CreatedAt =
                            DateTime.UtcNow,

                        WorkspaceId =
                            workspaceId.Value,

                        GeneratedByUserId =
                            userId
                    };

                _context.Reports.Add(
                    report);

                await _context.SaveChangesAsync();

                // =================================================
                // 20. RETURN REPORT
                // =================================================

                return Ok(new
                {
                    id =
                        report.Id,

                    title =
                        report.Title,

                    periodStart =
                        report.PeriodStart,

                    periodEnd =
                        report.PeriodEnd,

                    createdAt =
                        report.CreatedAt,

                    content
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "================================");

                Console.WriteLine(
                    "REPORT GENERATION ERROR");

                Console.WriteLine(
                    ex.ToString());

                Console.WriteLine(
                    "================================");

                return StatusCode(
                    500,
                    new
                    {
                        message =
                            "Unable to generate report.",

                        error =
                            ex.Message
                    });
            }
        }

        // =========================================================
        // GET WORKSPACE ID
        // =========================================================

        private Guid? GetWorkspaceId()
        {
            var claim =
                User.FindFirst(
                    "workspaceId")?.Value;

            if (Guid.TryParse(
                claim,
                out var workspaceId))
            {
                return workspaceId;
            }

            return null;
        }

        // =========================================================
        // PERCENTAGE
        // =========================================================

        private static double Percentage(
            int value,
            int total)
        {
            if (total == 0)
            {
                return 0;
            }

            return Math.Round(
                value * 100.0 / total,
                1);
        }

        // =========================================================
        // SENTIMENT NAME
        // =========================================================

        private static string GetSentimentName(
            Sentiment sentiment)
        {
            return sentiment switch
            {
                Sentiment.POS =>
                    "Positive",

                Sentiment.NEG =>
                    "Negative",

                _ =>
                    "Neutral"
            };
        }

        // =========================================================
        // RECOMMENDED ACTIONS
        // =========================================================

        private static List<string>
            BuildRecommendedActions(
                List<ThemeSummary> themes,
                List<FeedbackReportItem> feedback)
        {
            var actions =
                new List<string>();

            // -----------------------------------------------------
            // Theme-based actions
            // -----------------------------------------------------

            foreach (var theme in themes.Take(3))
            {
                var negativeForTheme =
                    feedback.Count(f =>
                        f.Sentiment ==
                            Sentiment.NEG &&
                        f.Themes.Any(t =>
                            string.Equals(
                                t.Name,
                                theme.Name,
                                StringComparison
                                    .OrdinalIgnoreCase)));

                if (negativeForTheme > 0)
                {
                    actions.Add(
                        $"Review and prioritize customer issues related to " +
                        $"{theme.Name}; {negativeForTheme} negative " +
                        $"feedback item(s) were associated with this theme.");
                }
                else
                {
                    actions.Add(
                        $"Review customer feedback related to " +
                        $"{theme.Name} and identify opportunities for improvement.");
                }
            }

            // -----------------------------------------------------
            // General negative feedback action
            // -----------------------------------------------------

            var negativeCount =
                feedback.Count(f =>
                    f.Sentiment ==
                        Sentiment.NEG);

            if (negativeCount > 0)
            {
                actions.Add(
                    $"Review the {negativeCount} negative feedback " +
                    $"item(s) and assign the highest-impact issues for follow-up.");
            }

            return actions
                .Distinct()
                .Take(5)
                .ToList();
        }

        // =========================================================
        // GROQ AI NARRATIVE
        // =========================================================

        private async Task<string?>
            GenerateAiNarrative(
                int total,
                double positive,
                double neutral,
                double negative,
                double positiveShift,
                double neutralShift,
                double negativeShift,
                List<ThemeSummary> themes,
                List<CustomerQuote> quotes,
                List<string> actions)
        {
            try
            {
                var apiKey =
                    _configuration[
                        "Groq:ApiKey"];

                // -------------------------------------------------
                // No API key = continue without AI
                // -------------------------------------------------

                if (string.IsNullOrWhiteSpace(
                    apiKey))
                {
                    return null;
                }

                var model =
                    _configuration[
                        "Groq:Model"]
                    ?? "openai/gpt-oss-120b";

                // -------------------------------------------------
                // Theme text
                // -------------------------------------------------

                var themeText =
                    string.Join(
                        "\n",
                        themes.Select(t =>
                            $"- {t.Name}: {t.Count} feedback"));

                // -------------------------------------------------
                // Quote text
                // -------------------------------------------------

                var quoteText =
                    string.Join(
                        "\n",
                        quotes.Select(q =>
                            $"- [{q.Sentiment}] {q.Content}"));

                // -------------------------------------------------
                // Action text
                // -------------------------------------------------

                var actionText =
                    string.Join(
                        "\n",
                        actions.Select(a =>
                            $"- {a}"));

                // -------------------------------------------------
                // AI prompt
                // -------------------------------------------------

                var prompt =
                    $"""
                    You are LOOP, a Voice-of-Customer reporting assistant.

                    Write a concise executive summary for a customer feedback report.

                    IMPORTANT:
                    - Use ONLY the statistics and evidence provided below.
                    - Do not invent numbers.
                    - Do not invent customer statements.
                    - Do not change the supplied statistics.
                    - Do not use markdown tables.
                    - Write 2 short paragraphs.
                    - Focus on what a product manager should understand.

                    TOTAL FEEDBACK:
                    {total}

                    CURRENT SENTIMENT:
                    Positive: {positive}%
                    Neutral: {neutral}%
                    Negative: {negative}%

                    SENTIMENT CHANGE FROM PREVIOUS PERIOD:
                    Positive: {positiveShift:+0.0;-0.0;0.0} percentage points
                    Neutral: {neutralShift:+0.0;-0.0;0.0} percentage points
                    Negative: {negativeShift:+0.0;-0.0;0.0} percentage points

                    TOP THEMES:
                    {themeText}

                    CUSTOMER QUOTES:
                    {quoteText}

                    RECOMMENDED ACTIONS:
                    {actionText}
                    """;

                // -------------------------------------------------
                // Groq request body
                // -------------------------------------------------

                var requestBody =
                    new
                    {
                        model,

                        messages =
                            new[]
                            {
                                new
                                {
                                    role =
                                        "system",

                                    content =
                                        "You write factual business summaries " +
                                        "grounded only in supplied data."
                                },

                                new
                                {
                                    role =
                                        "user",

                                    content =
                                        prompt
                                }
                            },

                        temperature =
                            0.2,

                        max_tokens =
                            350
                    };

                var json =
                    JsonSerializer.Serialize(
                        requestBody);

                // -------------------------------------------------
                // HTTP client
                // -------------------------------------------------

                using var client =
                    new HttpClient();

                client.Timeout =
                    TimeSpan.FromSeconds(
                        45);

                client.DefaultRequestHeaders
                    .Authorization =
                    new System.Net.Http.Headers
                        .AuthenticationHeaderValue(
                            "Bearer",
                            apiKey);

                using var httpContent =
                    new StringContent(
                        json,
                        Encoding.UTF8,
                        "application/json");

                // -------------------------------------------------
                // Call Groq
                // -------------------------------------------------

                var response =
                    await client.PostAsync(
                        "https://api.groq.com/openai/v1/chat/completions",
                        httpContent);

                var responseText =
                    await response.Content
                        .ReadAsStringAsync();

                // -------------------------------------------------
                // Groq error
                // -------------------------------------------------

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine(
                        $"Groq report error: " +
                        $"{(int)response.StatusCode}");

                    Console.WriteLine(
                        responseText);

                    return null;
                }

                // -------------------------------------------------
                // Parse response
                // -------------------------------------------------

                using var document =
                    JsonDocument.Parse(
                        responseText);

                var answer =
                    document.RootElement
                        .GetProperty("choices")[0]
                        .GetProperty("message")
                        .GetProperty("content")
                        .GetString();

                return answer?.Trim();
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "AI report narrative failed:");

                Console.WriteLine(
                    ex.ToString());

                // AI failure should not stop
                // report generation.

                return null;
            }
        }
    }


    // =============================================================
    // REQUEST MODEL
    // =============================================================

    public class GenerateReportRequest
    {
        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }
    }


    // =============================================================
    // FEEDBACK REPORT ITEM
    // =============================================================

    public class FeedbackReportItem
    {
        public Guid Id { get; set; }

        public string Content { get; set; }
            = string.Empty;

        public string Channel { get; set; }
            = string.Empty;

        public string? CustomerLabel { get; set; }

        public Sentiment Sentiment { get; set; }

        public double SentimentScore { get; set; }

        public DateTime CreatedAt { get; set; }

        public List<ThemeReportItem> Themes { get; set; }
            = new();
    }


    // =============================================================
    // THEME ITEM
    // =============================================================

    public class ThemeReportItem
    {
        public string Name { get; set; }
            = string.Empty;

        public double Confidence { get; set; }
    }


    // =============================================================
    // REPORT CONTENT
    // =============================================================

    public class ReportContent
    {
        public string Summary { get; set; }
            = string.Empty;

        public int TotalFeedback { get; set; }

        public SentimentSummary Sentiment { get; set; }
            = new();

        public SentimentShift SentimentShift { get; set; }
            = new();

        public List<ThemeSummary> TopThemes { get; set; }
            = new();

        public List<ChannelSummary> Channels { get; set; }
            = new();

        public List<CustomerQuote> Quotes { get; set; }
            = new();

        public List<string> RecommendedActions { get; set; }
            = new();
    }


    // =============================================================
    // SENTIMENT SUMMARY
    // =============================================================

    public class SentimentSummary
    {
        public int Positive { get; set; }

        public int Neutral { get; set; }

        public int Negative { get; set; }

        public double PositivePercentage { get; set; }

        public double NeutralPercentage { get; set; }

        public double NegativePercentage { get; set; }
    }


    // =============================================================
    // SENTIMENT SHIFT
    // =============================================================

    public class SentimentShift
    {
        public double Positive { get; set; }

        public double Neutral { get; set; }

        public double Negative { get; set; }
    }


    // =============================================================
    // THEME SUMMARY
    // =============================================================

    public class ThemeSummary
    {
        public string Name { get; set; }
            = string.Empty;

        public int Count { get; set; }

        public double Percentage { get; set; }
    }


    // =============================================================
    // CHANNEL SUMMARY
    // =============================================================

    public class ChannelSummary
    {
        public string Name { get; set; }
            = string.Empty;

        public int Count { get; set; }

        public double Percentage { get; set; }
    }


    // =============================================================
    // CUSTOMER QUOTE
    // =============================================================

    public class CustomerQuote
    {
        public string Content { get; set; }
            = string.Empty;

        public string Sentiment { get; set; }
            = string.Empty;

        public string Customer { get; set; }
            = string.Empty;

        public string Channel { get; set; }
            = string.Empty;

        public string Date { get; set; }
            = string.Empty;
    }
}