namespace AIQuizPlatform.Models;

public enum Difficulty { Easy, Medium, Hard }

public class Quiz
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Difficulty Difficulty { get; set; }
    public int TimeLimitSeconds { get; set; } = 600;
    public bool IsAIGenerated { get; set; }
    public bool IsPublished { get; set; }
    public bool NegativeMarking { get; set; }
    public int Version { get; set; } = 1;
    public string? Tags { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public string CreatedById { get; set; } = string.Empty;
    public ApplicationUser CreatedBy { get; set; } = null!;

    public ICollection<Question> Questions { get; set; } = new List<Question>();
    public ICollection<QuizAttempt> Attempts { get; set; } = new List<QuizAttempt>();
}
