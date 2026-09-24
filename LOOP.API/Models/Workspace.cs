namespace LOOP.API.Models
{
    public class Workspace
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public ICollection<User> Users { get; set; } = new List<User>();

        public ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

        public ICollection<Theme> Themes { get; set; } = new List<Theme>();

        public ICollection<Report> Reports { get; set; } = new List<Report>();
    }
}