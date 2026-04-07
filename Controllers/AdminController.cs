using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AIQuizPlatform.Data;
using AIQuizPlatform.Models;
namespace AIQuizPlatform.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.TotalUsers = await _db.Users.CountAsync();
        ViewBag.TotalQuizzes = await _db.Quizzes.CountAsync();
        ViewBag.TotalAttempts = await _db.QuizAttempts.CountAsync();
        ViewBag.AIGenerated = await _db.Quizzes.CountAsync(q => q.IsAIGenerated);
        ViewBag.TotalMentors = (await _userManager.GetUsersInRoleAsync("Mentor")).Count;
        ViewBag.TotalStudents = (await _userManager.GetUsersInRoleAsync("Student")).Count;
        return View();
    }

    // ─── MENTORS ────────────────────────────────────────────────

    public async Task<IActionResult> Mentors()
    {
        var mentors = await _userManager.GetUsersInRoleAsync("Mentor");
        return View(mentors.ToList());
    }

    [HttpGet]
    public IActionResult AddMentor() => View(new UserFormModel());

    [HttpPost]
    public async Task<IActionResult> AddMentor(UserFormModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            EmailConfirmed = true
        };
        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
            return View(model);
        }
        await _userManager.AddToRoleAsync(user, "Mentor");
        TempData["Success"] = $"Mentor '{model.FullName}' added successfully.";
        return RedirectToAction(nameof(Mentors));
    }

    [HttpGet]
    public async Task<IActionResult> EditMentor(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();
        return View(new EditUserModel { Id = user.Id, FullName = user.FullName, Email = user.Email! });
    }

    [HttpPost]
    public async Task<IActionResult> EditMentor(EditUserModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await _userManager.FindByIdAsync(model.Id);
        if (user == null) return NotFound();

        user.FullName = model.FullName;
        user.Email = model.Email;
        user.UserName = model.Email;

        if (!string.IsNullOrWhiteSpace(model.NewPassword))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
        }

        await _userManager.UpdateAsync(user);
        TempData["Success"] = "Mentor updated.";
        return RedirectToAction(nameof(Mentors));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteMentor(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user != null) await _userManager.DeleteAsync(user);
        TempData["Success"] = "Mentor deleted.";
        return RedirectToAction(nameof(Mentors));
    }

    // ─── STUDENTS ───────────────────────────────────────────────

    public async Task<IActionResult> Students()
    {
        var students = await _userManager.GetUsersInRoleAsync("Student");
        return View(students.ToList());
    }

    [HttpGet]
    public IActionResult AddStudent() => View(new UserFormModel());

    [HttpPost]
    public async Task<IActionResult> AddStudent(UserFormModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            EmailConfirmed = true
        };
        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
            return View(model);
        }
        await _userManager.AddToRoleAsync(user, "Student");
        TempData["Success"] = $"Student '{model.FullName}' added successfully.";
        return RedirectToAction(nameof(Students));
    }

    [HttpGet]
    public async Task<IActionResult> EditStudent(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();
        return View(new EditUserModel { Id = user.Id, FullName = user.FullName, Email = user.Email! });
    }

    [HttpPost]
    public async Task<IActionResult> EditStudent(EditUserModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await _userManager.FindByIdAsync(model.Id);
        if (user == null) return NotFound();

        user.FullName = model.FullName;
        user.Email = model.Email;
        user.UserName = model.Email;

        if (!string.IsNullOrWhiteSpace(model.NewPassword))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
        }

        await _userManager.UpdateAsync(user);
        TempData["Success"] = "Student updated.";
        return RedirectToAction(nameof(Students));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteStudent(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user != null) await _userManager.DeleteAsync(user);
        TempData["Success"] = "Student deleted.";
        return RedirectToAction(nameof(Students));
    }

    // ─── QUIZZES ────────────────────────────────────────────────

    public async Task<IActionResult> Quizzes()
    {
        var quizzes = await _db.Quizzes
            .Include(q => q.Category)
            .Include(q => q.CreatedBy)
            .Include(q => q.Questions)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();
        return View(quizzes);
    }

    [HttpPost]
    public async Task<IActionResult> ToggleQuiz(int id)
    {
        var quiz = await _db.Quizzes.FindAsync(id);
        if (quiz == null) return NotFound();
        quiz.IsPublished = !quiz.IsPublished;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Quizzes));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteQuiz(int id)
    {
        var quiz = await _db.Quizzes.FindAsync(id);
        if (quiz != null) { _db.Quizzes.Remove(quiz); await _db.SaveChangesAsync(); }
        return RedirectToAction(nameof(Quizzes));
    }
}
