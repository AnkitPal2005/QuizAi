using Microsoft.AspNetCore.Identity;

namespace AIQuizPlatform.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public int XP { get; set; } = 0;
    public int Level { get; set; } = 1;
    public int DailyStreak { get; set; } = 0;
    public int MaxStreak { get; set; } = 0;
    public DateTime? LastQuizDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<QuizAttempt> Attempts { get; set; } = new List<QuizAttempt>();
    public ICollection<UserBadge> Badges { get; set; } = new List<UserBadge>();
}
