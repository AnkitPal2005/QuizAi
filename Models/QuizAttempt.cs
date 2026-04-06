namespace AIQuizPlatform.Models;

public enum AttemptStatus { InProgress, Completed, TimedOut }

public class QuizAttempt
{
    public int Id { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public int Score { get; set; }
    public int TotalQuestions { get; set; }
    public int CorrectAnswers { get; set; }
    public int WrongAnswers { get; set; }
    public AttemptStatus Status { get; set; } = AttemptStatus.InProgress;
    public int TimeTakenSeconds { get; set; }
    public int XPGained { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public int QuizId { get; set; }
    public Quiz Quiz { get; set; } = null!;

    public ICollection<AttemptAnswer> Answers { get; set; } = new List<AttemptAnswer>();
}
