namespace LOOP.API.Models
{
    public class Feedback
    {
        public Guid Id { get; set; }

        public string Content { get; set; } = string.Empty;

        public string Channel { get; set; } = string.Empty;

        public string? SourceRef { get; set; }

        public string? CustomerLabel { get; set; }

        public Sentiment Sentiment { get; set; }

        public double SentimentScore { get; set; }

        public FeedbackStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }

        // Workspace relationship
        public Guid WorkspaceId { get; set; }

        public Workspace Workspace { get; set; } = null!;

        // Theme relationship
        public ICollection<FeedbackTheme> FeedbackThemes { get; set; }
            = new List<FeedbackTheme>();

        // Embedding relationship
        public Embedding? Embedding { get; set; }
    }

    public enum Sentiment
    {
        POS,
        NEU,
        NEG
    }

    public enum FeedbackStatus
    {
        NEW,
        REVIEWED,
        ACTIONED
    }
}