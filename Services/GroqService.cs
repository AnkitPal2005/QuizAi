using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AIQuizPlatform.Models;

namespace AIQuizPlatform.Services;

public class GeneratedQuestion
{
    public string Text { get; set; } = string.Empty;
    public string OptionA { get; set; } = string.Empty;
    public string OptionB { get; set; } = string.Empty;
    public string OptionC { get; set; } = string.Empty;
    public string OptionD { get; set; } = string.Empty;
    public string CorrectOption { get; set; } = "A";
    public string Explanation { get; set; } = string.Empty;
}

public class GroqService
{
    private readonly HttpClient _http;
    private readonly ILogger<GroqService> _logger;
    private const string ApiUrl = "https://api.groq.com/openai/v1/chat/completions";

    public GroqService(IHttpClientFactory factory, IConfiguration config, ILogger<GroqService> logger)
    {
        _http = factory.CreateClient("Groq");
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", config["Groq:ApiKey"]);
        _logger = logger;
    }

    public async Task<List<GeneratedQuestion>> GenerateQuestionsAsync(
        string topic, Difficulty difficulty, int count, int retries = 2)
    {
        var prompt = BuildPrompt(topic, difficulty, count);

        for (int attempt = 0; attempt <= retries; attempt++)
        {
            try
            {
                var body = new
                {
                    model = "llama-3.3-70b-versatile",
                    messages = new[]
                    {
                        new { role = "system", content = "You are a quiz generator. Always respond with valid JSON only, no markdown, no explanation outside JSON." },
                        new { role = "user", content = prompt }
                    },
                    temperature = 0.7,
                    max_tokens = 4000
                };

                var json = JsonSerializer.Serialize(body);
                var response = await _http.PostAsync(ApiUrl,
                    new StringContent(json, Encoding.UTF8, "application/json"));

                response.EnsureSuccessStatusCode();
                var raw = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(raw);
                var content = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? "[]";

                // Strip markdown code fences if present
                content = content.Trim();
                if (content.StartsWith("```")) content = content[(content.IndexOf('\n') + 1)..];
                if (content.EndsWith("```")) content = content[..content.LastIndexOf("```")];

                var questions = JsonSerializer.Deserialize<List<GeneratedQuestion>>(content.Trim(),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return questions ?? new List<GeneratedQuestion>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Groq attempt {Attempt} failed: {Error}", attempt + 1, ex.Message);
                if (attempt == retries) throw;
                await Task.Delay(1000 * (attempt + 1));
            }
        }

        return new List<GeneratedQuestion>();
    }

    public async Task<string> GetExplanationAsync(string questionText, string correctAnswer)
    {
        var prompt = $"Explain in 2-3 sentences why the answer to this question is '{correctAnswer}':\n{questionText}";
        try
        {
            var body = new
            {
                model = "llama-3.3-70b-versatile",
                messages = new[] { new { role = "user", content = prompt } },
                temperature = 0.5,
                max_tokens = 300
            };
            var response = await _http.PostAsync(ApiUrl,
                new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();
            var raw = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(raw);
            return doc.RootElement.GetProperty("choices")[0]
                .GetProperty("message").GetProperty("content").GetString() ?? "";
        }
        catch { return "Explanation not available."; }
    }

    public async Task<string> AskDoubtAsync(string question, string context)
    {
        var prompt = $"You are a helpful tutor. Answer this student's doubt clearly and concisely.\n\nContext: {context}\n\nStudent's question: {question}";
        try
        {
            var body = new
            {
                model = "llama-3.3-70b-versatile",
                messages = new[] { new { role = "user", content = prompt } },
                temperature = 0.6,
                max_tokens = 500
            };
            var response = await _http.PostAsync(ApiUrl,
                new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();
            var raw = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(raw);
            return doc.RootElement.GetProperty("choices")[0]
                .GetProperty("message").GetProperty("content").GetString() ?? "";
        }
        catch { return "Could not get answer. Please try again."; }
    }

    private static string BuildPrompt(string topic, Difficulty difficulty, int count) =>
        $"Generate exactly {count} multiple choice questions about \"{topic}\" at {difficulty} difficulty level.\n" +
        "Return ONLY a JSON array with this exact structure, no other text:\n" +
        "[\n  {\n    \"text\": \"Question text here?\",\n    \"optionA\": \"First option\",\n    \"optionB\": \"Second option\",\n    \"optionC\": \"Third option\",\n    \"optionD\": \"Fourth option\",\n    \"correctOption\": \"A\",\n    \"explanation\": \"Brief explanation of why this is correct\"\n  }\n]\n" +
        "Rules:\n- correctOption must be exactly \"A\", \"B\", \"C\", or \"D\"\n- All options must be distinct and plausible\n- Questions must be clear and unambiguous\n" +
        $"- Difficulty: {difficulty} (Easy=basic concepts, Medium=applied knowledge, Hard=advanced/tricky)";
}
