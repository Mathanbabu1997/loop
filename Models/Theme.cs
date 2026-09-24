namespace LOOP.Models
{
    public class Theme
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? Color { get; set; }

        // Tenant key
        public int WorkspaceId { get; set; }

        public Workspace Workspace { get; set; } = null!;

        public ICollection<FeedbackTheme> FeedbackThemes { get; set; }
            = new List<FeedbackTheme>();
    }
}