using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AIQuizPlatform.Data;
using AIQuizPlatform.Models;

namespace AIQuizPlatform.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var attempts = await _db.QuizAttempts
            .Include(a => a.Quiz).ThenInclude(q => q.Category)
            .Where(a => a.UserId == user.Id && a.Status == AttemptStatus.Completed)
            .OrderByDescending(a => a.CompletedAt)
            .Take(10)
            .ToListAsync();

        var badges = await _db.UserBadges
            .Include(ub => ub.Badge)
            .Where(ub => ub.UserId == user.Id)
            .ToListAsync();

        var totalAttempts = await _db.QuizAttempts.CountAsync(a => a.UserId == user.Id && a.Status == AttemptStatus.Completed);
        var avgScore = totalAttempts > 0
            ? await _db.QuizAttempts.Where(a => a.UserId == user.Id && a.Status == AttemptStatus.Completed)
                .AverageAsync(a => (double)a.CorrectAnswers / a.TotalQuestions * 100)
            : 0;

        // Weak topics: categories with lowest avg score
        var weakTopics = await _db.QuizAttempts
            .Include(a => a.Quiz).ThenInclude(q => q.Category)
            .Where(a => a.UserId == user.Id && a.Status == AttemptStatus.Completed)
            .GroupBy(a => a.Quiz.Category.Name)
            .Select(g => new WeakTopicDto
            {
                Category = g.Key,
                AvgAccuracy = g.Average(a => (double)a.CorrectAnswers / a.TotalQuestions * 100)
            })
            .OrderBy(x => x.AvgAccuracy)
            .Take(3)
            .ToListAsync();

        ViewBag.User = user;
        ViewBag.TotalAttempts = totalAttempts;
        ViewBag.AvgScore = Math.Round(avgScore, 1);
        ViewBag.Badges = badges;
        ViewBag.WeakTopics = weakTopics;
        return View(attempts);
    }
}

public class WeakTopicDto
{
    public string Category { get; set; } = string.Empty;
    public double AvgAccuracy { get; set; }
}
