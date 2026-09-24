using LOOP.API.Models;
using Microsoft.EntityFrameworkCore;

namespace LOOP.API.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(LoopDbContext context)
        {
            // Create workspace and users only if they don't exist
            var workspace = await context.Workspaces.FirstOrDefaultAsync();

            if (workspace == null)
            {
                workspace = new Workspace
                {
                    Id = Guid.NewGuid(),
                    Name = "LOOP Demo Workspace",
                    CreatedAt = DateTime.UtcNow
                };

                context.Workspaces.Add(workspace);

                var admin = new User
                {
                    Id = Guid.NewGuid(),
                    Name = "Demo Admin",
                    Email = "admin@loopdemo.com",
                    PasswordHash = "TEMP_PASSWORD_HASH",
                    Role = UserRole.ADMIN,
                    WorkspaceId = workspace.Id
                };

                var analyst = new User
                {
                    Id = Guid.NewGuid(),
                    Name = "Demo Analyst",
                    Email = "analyst@loopdemo.com",
                    PasswordHash = "TEMP_PASSWORD_HASH",
                    Role = UserRole.ANALYST,
                    WorkspaceId = workspace.Id
                };

                var viewer = new User
                {
                    Id = Guid.NewGuid(),
                    Name = "Demo Viewer",
                    Email = "viewer@loopdemo.com",
                    PasswordHash = "TEMP_PASSWORD_HASH",
                    Role = UserRole.VIEWER,
                    WorkspaceId = workspace.Id
                };

                context.Users.AddRange(admin, analyst, viewer);

                await context.SaveChangesAsync();
            }

            // Create demo themes for this workspace
            if (!await context.Themes.AnyAsync(t =>
                t.WorkspaceId == workspace.Id))
            {
                var themes = new List<Theme>
                {
                    new Theme
                    {
                        Id = Guid.NewGuid(),
                        Name = "Onboarding",
                        Description = "Feedback about signup, registration and getting started",
                        Color = "#4F46E5",
                        WorkspaceId = workspace.Id
                    },

                    new Theme
                    {
                        Id = Guid.NewGuid(),
                        Name = "Performance",
                        Description = "Feedback about speed, reliability and application performance",
                        Color = "#0891B2",
                        WorkspaceId = workspace.Id
                    },

                    new Theme
                    {
                        Id = Guid.NewGuid(),
                        Name = "Billing",
                        Description = "Feedback about payments, invoices, charges and refunds",
                        Color = "#16A34A",
                        WorkspaceId = workspace.Id
                    },

                    new Theme
                    {
                        Id = Guid.NewGuid(),
                        Name = "Customer Support",
                        Description = "Feedback about support response and issue resolution",
                        Color = "#EA580C",
                        WorkspaceId = workspace.Id
                    },

                    new Theme
                    {
                        Id = Guid.NewGuid(),
                        Name = "Mobile App",
                        Description = "Feedback about the mobile application experience",
                        Color = "#9333EA",
                        WorkspaceId = workspace.Id
                    }
                };

                context.Themes.AddRange(themes);

                await context.SaveChangesAsync();
            }

            // Create sample feedback if none exists
            if (!await context.Feedbacks.AnyAsync(f =>
                f.WorkspaceId == workspace.Id))
            {
                var feedbacks = new List<Feedback>
                {
                    new Feedback
                    {
                        Id = Guid.NewGuid(),
                        Content = "The signup process was very easy and quick.",
                        Channel = "Support Ticket",
                        SourceRef = "SUP-1001",
                        CustomerLabel = "Customer 1",
                        Sentiment = Sentiment.POS,
                        SentimentScore = 0.85,
                        Status = FeedbackStatus.NEW,
                        CreatedAt = DateTime.UtcNow.AddDays(-10),
                        WorkspaceId = workspace.Id
                    },

                    new Feedback
                    {
                        Id = Guid.NewGuid(),
                        Content = "The application takes too long to load.",
                        Channel = "App Store Review",
                        SourceRef = "APP-1002",
                        CustomerLabel = "Customer 2",
                        Sentiment = Sentiment.NEG,
                        SentimentScore = -0.75,
                        Status = FeedbackStatus.REVIEWED,
                        CreatedAt = DateTime.UtcNow.AddDays(-8),
                        WorkspaceId = workspace.Id
                    },

                    new Feedback
                    {
                        Id = Guid.NewGuid(),
                        Content = "I was charged twice for the same order.",
                        Channel = "NPS Survey",
                        SourceRef = "NPS-1003",
                        CustomerLabel = "Customer 3",
                        Sentiment = Sentiment.NEG,
                        SentimentScore = -0.90,
                        Status = FeedbackStatus.NEW,
                        CreatedAt = DateTime.UtcNow.AddDays(-6),
                        WorkspaceId = workspace.Id
                    },

                    new Feedback
                    {
                        Id = Guid.NewGuid(),
                        Content = "The mobile app crashes when I open notifications.",
                        Channel = "App Store Review",
                        SourceRef = "APP-1004",
                        CustomerLabel = "Customer 4",
                        Sentiment = Sentiment.NEG,
                        SentimentScore = -0.80,
                        Status = FeedbackStatus.NEW,
                        CreatedAt = DateTime.UtcNow.AddDays(-5),
                        WorkspaceId = workspace.Id
                    },

                    new Feedback
                    {
                        Id = Guid.NewGuid(),
                        Content = "Customer support resolved my issue quickly.",
                        Channel = "Support Ticket",
                        SourceRef = "SUP-1005",
                        CustomerLabel = "Customer 5",
                        Sentiment = Sentiment.POS,
                        SentimentScore = 0.90,
                        Status = FeedbackStatus.ACTIONED,
                        CreatedAt = DateTime.UtcNow.AddDays(-4),
                        WorkspaceId = workspace.Id
                    },

                    new Feedback
                    {
                        Id = Guid.NewGuid(),
                        Content = "The registration instructions were confusing.",
                        Channel = "NPS Survey",
                        SourceRef = "NPS-1006",
                        CustomerLabel = "Customer 6",
                        Sentiment = Sentiment.NEG,
                        SentimentScore = -0.60,
                        Status = FeedbackStatus.REVIEWED,
                        CreatedAt = DateTime.UtcNow.AddDays(-3),
                        WorkspaceId = workspace.Id
                    },

                    new Feedback
                    {
                        Id = Guid.NewGuid(),
                        Content = "The invoice information is clear and easy to understand.",
                        Channel = "Sales Call Note",
                        SourceRef = "CALL-1007",
                        CustomerLabel = "Customer 7",
                        Sentiment = Sentiment.POS,
                        SentimentScore = 0.70,
                        Status = FeedbackStatus.ACTIONED,
                        CreatedAt = DateTime.UtcNow.AddDays(-2),
                        WorkspaceId = workspace.Id
                    },

                    new Feedback
                    {
                        Id = Guid.NewGuid(),
                        Content = "The app performance has improved recently.",
                        Channel = "Community Post",
                        SourceRef = "COMM-1008",
                        CustomerLabel = "Customer 8",
                        Sentiment = Sentiment.POS,
                        SentimentScore = 0.65,
                        Status = FeedbackStatus.REVIEWED,
                        CreatedAt = DateTime.UtcNow.AddDays(-1),
                        WorkspaceId = workspace.Id
                    },

                    new Feedback
                    {
                        Id = Guid.NewGuid(),
                        Content = "I had to wait too long for a response from support.",
                        Channel = "Support Ticket",
                        SourceRef = "SUP-1009",
                        CustomerLabel = "Customer 9",
                        Sentiment = Sentiment.NEG,
                        SentimentScore = -0.70,
                        Status = FeedbackStatus.NEW,
                        CreatedAt = DateTime.UtcNow,
                        WorkspaceId = workspace.Id
                    },

                    new Feedback
                    {
                        Id = Guid.NewGuid(),
                        Content = "The mobile experience is simple and convenient.",
                        Channel = "App Store Review",
                        SourceRef = "APP-1010",
                        CustomerLabel = "Customer 10",
                        Sentiment = Sentiment.POS,
                        SentimentScore = 0.80,
                        Status = FeedbackStatus.NEW,
                        CreatedAt = DateTime.UtcNow,
                        WorkspaceId = workspace.Id
                    }
                };

                context.Feedbacks.AddRange(feedbacks);

                await context.SaveChangesAsync();
            }

            // Connect feedback to themes
            if (!await context.FeedbackThemes.AnyAsync())
            {
                var onboardingTheme = await context.Themes
                    .FirstAsync(t =>
                        t.WorkspaceId == workspace.Id &&
                        t.Name == "Onboarding");

                var performanceTheme = await context.Themes
                    .FirstAsync(t =>
                        t.WorkspaceId == workspace.Id &&
                        t.Name == "Performance");

                var billingTheme = await context.Themes
                    .FirstAsync(t =>
                        t.WorkspaceId == workspace.Id &&
                        t.Name == "Billing");

                var supportTheme = await context.Themes
                    .FirstAsync(t =>
                        t.WorkspaceId == workspace.Id &&
                        t.Name == "Customer Support");

                var mobileTheme = await context.Themes
                    .FirstAsync(t =>
                        t.WorkspaceId == workspace.Id &&
                        t.Name == "Mobile App");

                var feedbacks = await context.Feedbacks
                    .Where(f => f.WorkspaceId == workspace.Id)
                    .OrderBy(f => f.CreatedAt)
                    .ToListAsync();

                var feedbackThemes = new List<FeedbackTheme>();

                foreach (var feedback in feedbacks)
                {
                    Theme? selectedTheme = null;

                    if (feedback.Content.Contains(
                        "signup",
                        StringComparison.OrdinalIgnoreCase) ||
                        feedback.Content.Contains(
                        "registration",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        selectedTheme = onboardingTheme;
                    }
                    else if (feedback.Content.Contains(
                        "load",
                        StringComparison.OrdinalIgnoreCase) ||
                        feedback.Content.Contains(
                        "performance",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        selectedTheme = performanceTheme;
                    }
                    else if (feedback.Content.Contains(
                        "charged",
                        StringComparison.OrdinalIgnoreCase) ||
                        feedback.Content.Contains(
                        "invoice",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        selectedTheme = billingTheme;
                    }
                    else if (feedback.Content.Contains(
                        "mobile",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        selectedTheme = mobileTheme;
                    }
                    else if (feedback.Content.Contains(
                        "support",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        selectedTheme = supportTheme;
                    }

                    if (selectedTheme != null)
                    {
                        feedbackThemes.Add(new FeedbackTheme
                        {
                            FeedbackId = feedback.Id,
                            ThemeId = selectedTheme.Id,
                            Confidence = 0.95
                        });
                    }
                }

                if (feedbackThemes.Count > 0)
                {
                    context.FeedbackThemes.AddRange(feedbackThemes);

                    await context.SaveChangesAsync();
                }
            }
        }
    }
}