using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace DrugStoreWebsiteAuthen.Application.Services;
public interface IEmailSender
{
    Task SendEmailAsync(string toEmail, string subject, string body);
}

public class EmailSender: IEmailSender
{
    private readonly ILogger<EmailSender> _logger;
    private readonly IConfiguration _config;

    public EmailSender(ILogger<EmailSender> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        var smtpServer = _config["EmailSettings:MailServer"] ?? "smtp.gmail.com";
        var smtpPort = int.Parse(_config["EmailSettings:MailPort"] ?? "587");
        var smtpUser = _config["EmailSettings:SenderEmail"];
        var smtpPass = _config["EmailSettings:Password"];
        var senderName = _config["EmailSettings:SenderName"] ?? "DrugStore Support";

        var mail = new MailMessage();
        mail.From = new MailAddress(smtpUser, senderName);
        mail.To.Add(toEmail);
        mail.Subject = subject;
        mail.Body = body;
        mail.IsBodyHtml = true;

        using var smtp = new SmtpClient(smtpServer, smtpPort)
        {
            Credentials = new NetworkCredential(smtpUser, smtpPass),
            EnableSsl = true
        };

        try
        {
            _logger.LogInformation($"Sending email to {toEmail}");
            await smtp.SendMailAsync(mail);
            _logger.LogInformation($"Email sent successfully to {toEmail}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to send email to {toEmail}");
            throw new InvalidOperationException("Email cannot be sent at this time. Please try again later.");
        }
    }


}
