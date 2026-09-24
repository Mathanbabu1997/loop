using LOOP.API.Models;

namespace LOOP.API.DTOs
{
    public class CreateFeedbackRequest
    {
        public string Content { get; set; } = string.Empty;

        public string Channel { get; set; } = string.Empty;

        public string? SourceRef { get; set; }

        public string? CustomerLabel { get; set; }

        public Sentiment Sentiment { get; set; }

        public double SentimentScore { get; set; }
    }
}