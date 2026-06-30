using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using LoafNCatting.Service.Interfaces;
using LoafNCatting.Service.Mail;
using Microsoft.Extensions.Options;

namespace LoafNCatting.Service.Implementations;

public class SmtpMailService(IOptions<SmtpMailOptions> optionsAccessor) : IMailService
{
    private readonly SmtpMailOptions _options = optionsAccessor.Value;

    public async Task SendAsync(MailMessageRequest message)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(message.To);
        ArgumentException.ThrowIfNullOrWhiteSpace(message.To.Email);
        ArgumentException.ThrowIfNullOrWhiteSpace(message.Subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(message.PlainTextBody);
        EnsureConfigured();

        using var mailMessage = new MailMessage
        {
            From = CreateAddress(_options.FromEmail, _options.FromName),
            Subject = message.Subject.Trim(),
            SubjectEncoding = Encoding.UTF8,
            Body = message.PlainTextBody,
            BodyEncoding = Encoding.UTF8,
            IsBodyHtml = false
        };

        mailMessage.To.Add(CreateAddress(message.To.Email, message.To.DisplayName));

        if (!string.IsNullOrWhiteSpace(message.HtmlBody))
        {
            mailMessage.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(
                message.HtmlBody,
                Encoding.UTF8,
                MediaTypeNames.Text.Html));
        }

        using var smtpClient = CreateClient();
        await smtpClient.SendMailAsync(mailMessage);
    }

    private SmtpClient CreateClient()
    {
        var client = new SmtpClient(_options.Host, _options.Port)
        {
            DeliveryMethod = SmtpDeliveryMethod.Network,
            EnableSsl = _options.EnableSsl,
            UseDefaultCredentials = false
        };

        if (!string.IsNullOrWhiteSpace(_options.Username) || !string.IsNullOrWhiteSpace(_options.Password))
        {
            client.Credentials = new NetworkCredential(_options.Username, _options.Password);
        }

        return client;
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.Host))
        {
            throw new InvalidOperationException("Mail:Smtp:Host is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_options.FromEmail))
        {
            throw new InvalidOperationException("Mail:Smtp:FromEmail is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_options.Username) != string.IsNullOrWhiteSpace(_options.Password))
        {
            throw new InvalidOperationException("Mail:Smtp username and password must either both be set or both be empty.");
        }
    }

    private static MailAddress CreateAddress(string email, string? displayName)
    {
        return string.IsNullOrWhiteSpace(displayName)
            ? new MailAddress(email.Trim())
            : new MailAddress(email.Trim(), displayName.Trim(), Encoding.UTF8);
    }
}
