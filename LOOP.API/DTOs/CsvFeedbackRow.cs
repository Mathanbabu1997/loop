using LOOP.API.Models;
using CsvHelper.Configuration.Attributes;



namespace LOOP.API.DTOs
{
    public class CsvFeedbackRow
    {
        [Name("content")]
        public string Content { get; set; } = string.Empty;

        [Name("channel")]
        public string Channel { get; set; } = string.Empty;

        [Name("customer_label")]
        public string? CustomerLabel { get; set; }

        [Name("created_at")]
        public DateTime? CreatedAt { get; set; }
    }
}