using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;
using MailKit.Security;

public class EmailService
{
    private readonly SmtpSettings _smtpSettings;

    public EmailService(IConfiguration configuration)
    {
        // Menyuntikkan konfigurasi dari appsettings.json
        _smtpSettings = configuration.GetSection("SmtpSettings").Get<SmtpSettings>();
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Kantong Pintar", _smtpSettings.FromEmail));  // From email
        message.To.Add(new MailboxAddress("", toEmail));  // To email
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = body  // Email body in HTML format
        };

        message.Body = bodyBuilder.ToMessageBody();

        using (var client = new SmtpClient())
        {
            // Gunakan STARTTLS untuk port 587
          // Di EmailService.cs
await client.ConnectAsync(_smtpSettings.Host, 465, SecureSocketOptions.SslOnConnect);
            await client.AuthenticateAsync(_smtpSettings.Username, _smtpSettings.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}

public class SmtpSettings
{
    public string Host { get; set; }
    public int Port { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string FromEmail { get; set; }
    public bool EnableSsl { get; set; }  // Ini tidak digunakan lagi karena kita menggunakan STARTTLS
}
