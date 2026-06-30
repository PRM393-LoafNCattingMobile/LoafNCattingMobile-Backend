using LoafNCatting.Service.Mail;

namespace LoafNCatting.Service.Interfaces;

public interface IVerificationEmailComposer
{
    MailMessageRequest Compose(
        string recipientEmail,
        string? recipientName,
        string verificationCode,
        TimeSpan expiresIn);
}
