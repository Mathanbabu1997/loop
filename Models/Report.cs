namespace LOOP.Models
{
    public class Report
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public DateTime PeriodStart { get; set; }

        public DateTime PeriodEnd { get; set; }

        public string ContentJson { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Tenant key
        public int WorkspaceId { get; set; }

        public Workspace Workspace { get; set; } = null!;

        // User who generated the report
        public int GeneratedBy { get; set; }

        public User GeneratedByUser { get; set; } = null!;
    }
}