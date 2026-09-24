namespace LOOP.Models
{
    public class FeedbackTheme
    {
        public int FeedbackId { get; set; }

        public int ThemeId { get; set; }

        public decimal Confidence { get; set; }

        // Relationships
        public Feedback Feedback { get; set; } = null!;

        public Theme Theme { get; set; } = null!;
    }
}