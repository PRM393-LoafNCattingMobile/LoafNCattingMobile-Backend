using LoafNCatting.Service.Mail;

namespace LoafNCatting.Service.Interfaces;

public interface IMailService
{
    Task SendAsync(MailMessageRequest message);
}
