namespace LOOP.API.Models
{
    public class Embedding
    {
        public Guid Id { get; set; }

        public Guid FeedbackId { get; set; }

        public Feedback Feedback { get; set; } = null!;

        public string Vector { get; set; } = string.Empty;
    }
}