namespace AIQuizPlatform.Models;

public class AttemptAnswer
{
    public int Id { get; set; }
    public string? SelectedOption { get; set; } // A, B, C, D or null (skipped)
    public bool IsCorrect { get; set; }

    public int AttemptId { get; set; }
    public QuizAttempt Attempt { get; set; } = null!;

    public int QuestionId { get; set; }
    public Question Question { get; set; } = null!;
}
