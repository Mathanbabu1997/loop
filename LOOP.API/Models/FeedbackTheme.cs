namespace LOOP.API.Models
{
    public class FeedbackTheme
    {
        public Guid FeedbackId { get; set; }

        public Guid ThemeId { get; set; }

        public double Confidence { get; set; }

        // Navigation properties
        public Feedback Feedback { get; set; } = null!;

        public Theme Theme { get; set; } = null!;
    }
}