using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

public interface IEmailSender
{
    Task SendEmailAsync(string toEmail, string subject, string body);
}

public class EmailSender: IEmailSender
{
    private readonly string _smtpServer = "smtp.gmail.com";
    private readonly int _smtpPort = 587;
    private readonly string _smtpUser = "phuongnhu308@gmail.com";
    private readonly string _smtpPass = "nseh mvct uudh stci";
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(ILogger<EmailSender> logger)
    {
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        var mail = new MailMessage();
        mail.From = new MailAddress(_smtpUser);
        mail.To.Add(toEmail);
        mail.Subject = subject;
        mail.Body = body;
        mail.IsBodyHtml = true;

        using var smtp = new SmtpClient(_smtpServer, _smtpPort)
        {
            Credentials = new NetworkCredential(_smtpUser, _smtpPass),
            EnableSsl = true
        };

        try
        {
            _logger.LogInformation($"Sending email to {toEmail}");
            await smtp.SendMailAsync(mail);
            _logger.LogInformation($"Email sent to {toEmail}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to send email to {toEmail}");
        }
    }


}
