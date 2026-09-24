namespace LOOP.API.DTOs
{
    public class FeedbackListResponse
    {
        public List<FeedbackResponse> Items { get; set; } = new();

        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalItems { get; set; }

        public int TotalPages { get; set; }
    }

    public class FeedbackResponse
    {
        public Guid Id { get; set; }

        public string Content { get; set; } = string.Empty;

        public string Channel { get; set; } = string.Empty;

        public string? CustomerLabel { get; set; }

        public string Sentiment { get; set; } = string.Empty;

        public double SentimentScore { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}