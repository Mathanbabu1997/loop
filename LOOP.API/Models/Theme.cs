namespace LOOP.API.Models
{
    public class Theme
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? Color { get; set; }

        public Guid WorkspaceId { get; set; }

        public Workspace Workspace { get; set; } = null!;

        public ICollection<FeedbackTheme> FeedbackThemes { get; set; }
            = new List<FeedbackTheme>();
    }
}