using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AIQuizPlatform.Models;

namespace AIQuizPlatform.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<QuizAttempt> QuizAttempts => Set<QuizAttempt>();
    public DbSet<AttemptAnswer> AttemptAnswers => Set<AttemptAnswer>();
    public DbSet<Badge> Badges => Set<Badge>();
    public DbSet<UserBadge> UserBadges => Set<UserBadge>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Quiz>()
            .HasOne(q => q.CreatedBy)
            .WithMany()
            .HasForeignKey(q => q.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<QuizAttempt>()
            .HasOne(a => a.User)
            .WithMany(u => u.Attempts)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<UserBadge>()
            .HasOne(ub => ub.User)
            .WithMany(u => u.Badges)
            .HasForeignKey(ub => ub.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Seed categories
        builder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Data Structures & Algorithms", IconClass = "bi-diagram-3", ColorHex = "#6366f1" },
            new Category { Id = 2, Name = "Computer Networks", IconClass = "bi-hdd-network", ColorHex = "#0ea5e9" },
            new Category { Id = 3, Name = "Operating Systems", IconClass = "bi-cpu", ColorHex = "#10b981" },
            new Category { Id = 4, Name = "Database Management", IconClass = "bi-database", ColorHex = "#f59e0b" },
            new Category { Id = 5, Name = "Object Oriented Programming", IconClass = "bi-boxes", ColorHex = "#ef4444" },
            new Category { Id = 6, Name = "System Design", IconClass = "bi-layers", ColorHex = "#8b5cf6" }
        );

        // Seed badges
        builder.Entity<Badge>().HasData(
            new Badge { Id = 1, Name = "First Step", Description = "Complete your first quiz", IconClass = "bi-star", ColorHex = "#f59e0b", XPRequired = 0 },
            new Badge { Id = 2, Name = "Rising Star", Description = "Earn 100 XP", IconClass = "bi-star-fill", ColorHex = "#f59e0b", XPRequired = 100 },
            new Badge { Id = 3, Name = "Scholar", Description = "Earn 500 XP", IconClass = "bi-mortarboard", ColorHex = "#6366f1", XPRequired = 500 },
            new Badge { Id = 4, Name = "Expert", Description = "Earn 1000 XP", IconClass = "bi-trophy", ColorHex = "#10b981", XPRequired = 1000 },
            new Badge { Id = 5, Name = "Legend", Description = "Earn 5000 XP", IconClass = "bi-trophy-fill", ColorHex = "#ef4444", XPRequired = 5000 }
        );
    }
}
