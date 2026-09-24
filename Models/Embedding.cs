namespace LOOP.Models
{
    public class Embedding
    {
        public int Id { get; set; }

        public int FeedbackId { get; set; }

        public string Vector { get; set; } = string.Empty;

        // Relationship
        public Feedback Feedback { get; set; } = null!;
    }
}