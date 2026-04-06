using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AIQuizPlatform.Data;
using AIQuizPlatform.Models;
using AIQuizPlatform.Services;

namespace AIQuizPlatform.Controllers;

public class QuizController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly QuizService _quizService;
    private readonly UserManager<ApplicationUser> _userManager;

    public QuizController(ApplicationDbContext db, QuizService quizService, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _quizService = quizService;
        _userManager = userManager;
    }

    // ✅ Everyone can browse
    [AllowAnonymous]
    public async Task<IActionResult> Index(int? categoryId, string? search)
    {
        var query = _db.Quizzes
            .Include(q => q.Category)
            .Include(q => q.Questions)
            .Where(q => q.IsPublished);

        if (categoryId.HasValue) query = query.Where(q => q.CategoryId == categoryId);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(q => q.Title.Contains(search) || q.Topic.Contains(search));

        ViewBag.Categories = await _db.Categories.ToListAsync();
        ViewBag.SelectedCategory = categoryId;
        ViewBag.Search = search;
        return View(await query.OrderByDescending(q => q.CreatedAt).ToListAsync());
    }

    // ✅ Only Admin & Mentor can generate quizzes
    [Authorize(Roles = "Admin,Mentor")]
    public IActionResult Generate()
    {
        ViewBag.Categories = _db.Categories.ToList();
        return View();
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Mentor")]
    public async Task<IActionResult> Generate(string topic, Difficulty difficulty, int questionCount, int categoryId, string? tags)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        try
        {
            var quiz = await _quizService.GenerateAndSaveQuizAsync(topic, difficulty, questionCount, categoryId, user.Id, tags);
            TempData["Success"] = $"Quiz '{quiz.Title}' generated with {quiz.Questions.Count} questions!";
            return RedirectToAction(nameof(Take), new { id = quiz.Id });
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"AI generation failed: {ex.Message}";
            ViewBag.Categories = _db.Categories.ToList();
            return View();
        }
    }

    // ✅ Must be logged in to take a quiz
    [Authorize(Roles = "Admin,Mentor,Student")]
    public async Task<IActionResult> Take(int id)
    {
        var quiz = await _db.Quizzes
            .Include(q => q.Questions.OrderBy(q => q.OrderIndex))
            .Include(q => q.Category)
            .FirstOrDefaultAsync(q => q.Id == id && q.IsPublished);

        if (quiz == null) return NotFound();

        var shuffled = quiz.Questions.OrderBy(_ => Guid.NewGuid()).ToList();
        ViewBag.ShuffledQuestions = shuffled;
        return View(quiz);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Mentor,Student")]
    public async Task<IActionResult> Submit(int quizId, IFormCollection form)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var answers = new Dictionary<int, string>();
        foreach (var key in form.Keys.Where(k => k.StartsWith("q_")))
        {
            if (int.TryParse(key[2..], out var qId))
                answers[qId] = form[key].ToString();
        }

        int.TryParse(form["timeTaken"], out var timeTaken);
        var attempt = await _quizService.SubmitAttemptAsync(quizId, user.Id, answers, timeTaken);
        return RedirectToAction(nameof(Result), new { id = attempt.Id });
    }

    // ✅ Only own results, or Admin can see all
    [Authorize(Roles = "Admin,Mentor,Student")]
    public async Task<IActionResult> Result(int id)
    {
        var attempt = await _db.QuizAttempts
            .Include(a => a.Quiz).ThenInclude(q => q.Questions)
            .Include(a => a.Answers).ThenInclude(a => a.Question)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (attempt == null) return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (attempt.UserId != user?.Id && !User.IsInRole("Admin")) return Forbid();

        return View(attempt);
    }

    // ✅ Everyone can see leaderboard
    [AllowAnonymous]
    public async Task<IActionResult> Leaderboard()
    {
        var entries = await _quizService.GetLeaderboardAsync(20);
        return View(entries);
    }
}
