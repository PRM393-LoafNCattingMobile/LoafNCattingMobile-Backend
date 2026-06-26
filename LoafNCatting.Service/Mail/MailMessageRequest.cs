namespace LoafNCatting.Service.Mail;

public sealed record MailMessageRequest(
    MailRecipient To,
    string Subject,
    string PlainTextBody,
    string? HtmlBody = null);
