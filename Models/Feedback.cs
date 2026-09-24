namespace LOOP.Models
{
    public class Feedback
    {
        public int Id { get; set; }

        public string Content { get; set; } = string.Empty;

        public string Channel { get; set; } = string.Empty;

        public string? SourceRef { get; set; }

        public string? CustomerLabel { get; set; }

        public string Sentiment { get; set; } = "NEU";

        public decimal? SentimentScore { get; set; }

        public string Status { get; set; } = "NEW";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Tenant key
        public int WorkspaceId { get; set; }

        public Workspace Workspace { get; set; } = null!;

        // Relationships
        public ICollection<FeedbackTheme> FeedbackThemes { get; set; }
            = new List<FeedbackTheme>();

        public Embedding? Embedding { get; set; }
    }
}