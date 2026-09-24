namespace LOOP.API.Models
{
    public class User
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public UserRole Role { get; set; }

        public Guid WorkspaceId { get; set; }

        // Navigation property
        public Workspace Workspace { get; set; } = null!;

        public ICollection<Report> Reports { get; set; } = new List<Report>();
    }

    public enum UserRole
    {
        ADMIN,
        ANALYST,
        VIEWER
    }
}