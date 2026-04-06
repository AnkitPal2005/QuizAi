namespace AIQuizPlatform.Models;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconClass { get; set; }
    public string ColorHex { get; set; } = "#6366f1";

    public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
}
