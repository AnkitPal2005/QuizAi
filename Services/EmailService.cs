using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace AIQuizPlatform.Services;

public class EmailService : IEmailSender
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        try
        {
            var mail = new MimeMessage();
            mail.From.Add(new MailboxAddress(
                _config["Email:SenderName"] ?? "QuizAI",
                _config["Email:SenderEmail"]!));
            mail.To.Add(MailboxAddress.Parse(email));
            mail.Subject = subject;

            var body = new BodyBuilder { HtmlBody = WrapInTemplate(subject, htmlMessage) };
            mail.Body = body.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(
                _config["Email:Host"]!,
                int.Parse(_config["Email:Port"] ?? "587"),
                SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(
                _config["Email:Username"]!,
                _config["Email:Password"]!);
            await smtp.SendAsync(mail);
            await smtp.DisconnectAsync(true);

            _logger.LogInformation("Email sent to {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError("Email send failed: {Error}", ex.Message);
            throw;
        }
    }

    private static string WrapInTemplate(string subject, string content)
    {
        return $@"<!DOCTYPE html>
<html>
<head>
  <meta charset='utf-8'>
  <style>
    body {{ font-family: 'Segoe UI', sans-serif; background: #f1f5f9; margin: 0; padding: 20px; }}
    .container {{ max-width: 560px; margin: 0 auto; background: #fff; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 24px rgba(0,0,0,0.08); }}
    .header {{ background: linear-gradient(135deg, #0f172a, #312e81); padding: 32px; text-align: center; }}
    .header h1 {{ color: #fff; margin: 0; font-size: 1.5rem; font-weight: 800; }}
    .header p {{ color: rgba(255,255,255,0.6); margin: 4px 0 0; font-size: 0.85rem; }}
    .body {{ padding: 32px; color: #1e293b; line-height: 1.6; }}
    .btn {{ display: inline-block; background: #6366f1; color: #fff !important; padding: 12px 32px; border-radius: 10px; text-decoration: none; font-weight: 700; margin: 16px 0; }}
    .footer {{ background: #f8fafc; padding: 20px 32px; text-align: center; color: #94a3b8; font-size: 0.78rem; border-top: 1px solid #e2e8f0; }}
  </style>
</head>
<body>
  <div class='container'>
    <div class='header'>
      <h1>⚡ QuizAI</h1>
      <p>Powered by Groq AI</p>
    </div>
    <div class='body'>
      <h2 style='margin-top:0'>{subject}</h2>
      {content}
    </div>
    <div class='footer'>
      &copy; {DateTime.Now.Year} QuizAI. If you didn't request this, ignore this email.
    </div>
  </div>
</body>
</html>";
    }
}
