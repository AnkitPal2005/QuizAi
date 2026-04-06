namespace AIQuizPlatform.Models;

public class Question
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public string OptionA { get; set; } = string.Empty;
    public string OptionB { get; set; } = string.Empty;
    public string OptionC { get; set; } = string.Empty;
    public string OptionD { get; set; } = string.Empty;
    public string CorrectOption { get; set; } = "A"; // A, B, C, D
    public string? Explanation { get; set; }
    public int OrderIndex { get; set; }

    public int QuizId { get; set; }
    public Quiz Quiz { get; set; } = null!;

    public ICollection<AttemptAnswer> Answers { get; set; } = new List<AttemptAnswer>();
}
