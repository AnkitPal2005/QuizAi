using Dapper;
using Npgsql;
using AIQuizPlatform.Models;
using AIQuizPlatform.Data;
using Microsoft.EntityFrameworkCore;

namespace AIQuizPlatform.Services;

public class QuizService
{
    private readonly ApplicationDbContext _db;
    private readonly GroqService _groq;
    private readonly string _connStr;

    public QuizService(ApplicationDbContext db, GroqService groq, IConfiguration config)
    {
        _db = db;
        _groq = groq;
        _connStr = config.GetConnectionString("DefaultConnection")!;
    }

    // Dapper-based leaderboard query (fast read)
    public async Task<List<LeaderboardEntry>> GetLeaderboardAsync(int top = 20)
    {
        await using var conn = new NpgsqlConnection(_connStr);
        var sql = """
            SELECT u."Id", u."FullName", u."XP", u."Level",
                   COUNT(qa."Id") AS "TotalAttempts",
                   COALESCE(AVG(
                       CASE WHEN qa."TotalQuestions" > 0
                            THEN (qa."CorrectAnswers"::float / qa."TotalQuestions"::float) * 100
                            ELSE 0 END
                   ), 0) AS "AvgScore"
            FROM "AspNetUsers" u
            LEFT JOIN "QuizAttempts" qa ON qa."UserId" = u."Id" AND qa."Status" = 1
            GROUP BY u."Id", u."FullName", u."XP", u."Level"
            ORDER BY u."XP" DESC
            LIMIT @top
            """;
        var result = await conn.QueryAsync<LeaderboardEntry>(sql, new { top });
        return result.ToList();
    }

    public async Task<Quiz> GenerateAndSaveQuizAsync(
        string topic, Difficulty difficulty, int questionCount,
        int categoryId, string userId, string? tags = null)
    {
        var questions = await _groq.GenerateQuestionsAsync(topic, difficulty, questionCount);

        var quiz = new Quiz
        {
            Title = $"{topic} - {difficulty} Quiz",
            Topic = topic,
            Difficulty = difficulty,
            CategoryId = categoryId,
            CreatedById = userId,
            IsAIGenerated = true,
            IsPublished = true,
            Tags = tags,
            TimeLimitSeconds = questionCount * 60,
            Questions = questions.Select((q, i) => new Question
            {
                Text = q.Text,
                OptionA = q.OptionA,
                OptionB = q.OptionB,
                OptionC = q.OptionC,
                OptionD = q.OptionD,
                CorrectOption = q.CorrectOption,
                Explanation = q.Explanation,
                OrderIndex = i
            }).ToList()
        };

        _db.Quizzes.Add(quiz);
        await _db.SaveChangesAsync();
        return quiz;
    }

    public async Task<QuizAttempt> SubmitAttemptAsync(
        int quizId, string userId, Dictionary<int, string> answers, int timeTaken)
    {
        var quiz = await _db.Quizzes
            .Include(q => q.Questions)
            .FirstOrDefaultAsync(q => q.Id == quizId)
            ?? throw new Exception("Quiz not found");

        var attempt = new QuizAttempt
        {
            QuizId = quizId,
            UserId = userId,
            TotalQuestions = quiz.Questions.Count,
            TimeTakenSeconds = timeTaken,
            Status = AttemptStatus.Completed,
            CompletedAt = DateTime.UtcNow
        };

        foreach (var question in quiz.Questions)
        {
            answers.TryGetValue(question.Id, out var selected);
            var isCorrect = selected == question.CorrectOption;
            if (isCorrect) attempt.CorrectAnswers++;
            else if (selected != null) attempt.WrongAnswers++;

            attempt.Answers.Add(new AttemptAnswer
            {
                QuestionId = question.Id,
                SelectedOption = selected,
                IsCorrect = isCorrect
            });
        }

        // Scoring: +4 correct, -1 wrong (if negative marking enabled)
        attempt.Score = quiz.NegativeMarking
            ? (attempt.CorrectAnswers * 4) - attempt.WrongAnswers
            : attempt.CorrectAnswers * 4;

        _db.QuizAttempts.Add(attempt);

        // Award XP + update streak
        var user = await _db.Users.FindAsync(userId);
        if (user != null)
        {
            var xpGained = attempt.CorrectAnswers * 10;

            // Streak logic
            var today = DateTime.UtcNow.Date;
            if (user.LastQuizDate?.Date == today.AddDays(-1))
            {
                user.DailyStreak++;
                if (user.DailyStreak > user.MaxStreak) user.MaxStreak = user.DailyStreak;
                // Streak bonus XP
                if (user.DailyStreak % 7 == 0) xpGained += 50; // weekly streak bonus
            }
            else if (user.LastQuizDate?.Date != today)
            {
                user.DailyStreak = 1;
            }
            user.LastQuizDate = DateTime.UtcNow;

            user.XP += xpGained;
            user.Level = CalculateLevel(user.XP);
            attempt.XPGained = xpGained;
            await AwardBadgesAsync(user);
        }

        await _db.SaveChangesAsync();
        return attempt;
    }

    private static int CalculateLevel(int xp) => xp switch
    {
        < 100 => 1,
        < 300 => 2,
        < 600 => 3,
        < 1000 => 4,
        < 2000 => 5,
        < 5000 => 6,
        _ => 7
    };

    private async Task AwardBadgesAsync(ApplicationUser user)
    {
        var badges = await _db.Badges.ToListAsync();
        var earned = await _db.UserBadges.Where(ub => ub.UserId == user.Id).Select(ub => ub.BadgeId).ToListAsync();

        foreach (var badge in badges.Where(b => b.XPRequired <= user.XP && !earned.Contains(b.Id)))
        {
            _db.UserBadges.Add(new UserBadge { UserId = user.Id, BadgeId = badge.Id });
        }
    }
}

public class LeaderboardEntry
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int XP { get; set; }
    public int Level { get; set; }
    public int TotalAttempts { get; set; }
    public double AvgScore { get; set; }
}
