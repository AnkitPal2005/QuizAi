using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AIQuizPlatform.Services;

namespace AIQuizPlatform.Controllers;

[Authorize]
public class DoubtController : Controller
{
    private readonly GroqService _groq;
    public DoubtController(GroqService groq) => _groq = groq;

    [HttpPost]
    public async Task<IActionResult> Ask([FromBody] DoubtRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Question))
            return BadRequest("Question is required");

        var answer = await _groq.AskDoubtAsync(req.Question, req.Context ?? "");
        return Json(new { answer });
    }
}

public class DoubtRequest
{
    public string Question { get; set; } = string.Empty;
    public string? Context { get; set; }
}
