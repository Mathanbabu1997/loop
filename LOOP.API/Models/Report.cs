namespace LOOP.API.Models
{
    public class Report
    {
        public Guid Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public DateTime PeriodStart { get; set; }

        public DateTime PeriodEnd { get; set; }

        public string ContentJson { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        // Workspace relationship
        public Guid WorkspaceId { get; set; }

        public Workspace Workspace { get; set; } = null!;

        // User who generated the report
        public Guid GeneratedByUserId { get; set; }

        public User GeneratedByUser { get; set; } = null!;
    }
}