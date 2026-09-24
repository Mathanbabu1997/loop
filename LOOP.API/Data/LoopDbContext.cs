using LOOP.API.Models;
using Microsoft.EntityFrameworkCore;

namespace LOOP.API.Data
{
    public class LoopDbContext : DbContext
    {
        public LoopDbContext(DbContextOptions<LoopDbContext> options)
            : base(options)
        {
        }

        public DbSet<Workspace> Workspaces { get; set; }

        public DbSet<User> Users { get; set; }

        public DbSet<Feedback> Feedbacks { get; set; }

        public DbSet<Theme> Themes { get; set; }

        public DbSet<FeedbackTheme> FeedbackThemes { get; set; }

        public DbSet<Embedding> Embeddings { get; set; }

        public DbSet<Report> Reports { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Workspace → Users
            modelBuilder.Entity<User>()
                .HasOne(u => u.Workspace)
                .WithMany(w => w.Users)
                .HasForeignKey(u => u.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);

            // Workspace → Feedback
            modelBuilder.Entity<Feedback>()
                .HasOne(f => f.Workspace)
                .WithMany(w => w.Feedbacks)
                .HasForeignKey(f => f.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);

            // Workspace → Themes
            modelBuilder.Entity<Theme>()
                .HasOne(t => t.Workspace)
                .WithMany(w => w.Themes)
                .HasForeignKey(t => t.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);

            // Feedback ↔ Theme
            modelBuilder.Entity<FeedbackTheme>()
                .HasKey(ft => new { ft.FeedbackId, ft.ThemeId });

            modelBuilder.Entity<FeedbackTheme>()
                .HasOne(ft => ft.Feedback)
                .WithMany(f => f.FeedbackThemes)
                .HasForeignKey(ft => ft.FeedbackId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FeedbackTheme>()
                .HasOne(ft => ft.Theme)
                .WithMany(t => t.FeedbackThemes)
                .HasForeignKey(ft => ft.ThemeId)
                .OnDelete(DeleteBehavior.Cascade);

            // Feedback → Embedding (one-to-one)
            modelBuilder.Entity<Embedding>()
                .HasOne(e => e.Feedback)
                .WithOne(f => f.Embedding)
                .HasForeignKey<Embedding>(e => e.FeedbackId)
                .OnDelete(DeleteBehavior.Cascade);

            // Workspace → Reports
            modelBuilder.Entity<Report>()
                .HasOne(r => r.Workspace)
                .WithMany(w => w.Reports)
                .HasForeignKey(r => r.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);

            // User → Reports
            modelBuilder.Entity<Report>()
                .HasOne(r => r.GeneratedByUser)
                .WithMany(u => u.Reports)
                .HasForeignKey(r => r.GeneratedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Unique email
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Sentiment score range
            modelBuilder.Entity<Feedback>()
                .Property(f => f.SentimentScore)
                .HasPrecision(5, 2);

            // Theme confidence
            modelBuilder.Entity<FeedbackTheme>()
                .Property(ft => ft.Confidence)
                .HasPrecision(5, 2);
        }
    }
}