using System.Net;
using LoafNCatting.Service.Interfaces;
using LoafNCatting.Service.Mail;

namespace LoafNCatting.Service.Implementations;

public class VerificationEmailComposer : IVerificationEmailComposer
{
    private const string Subject = "Verify your LoafNCatting account";

    public MailMessageRequest Compose(
        string recipientEmail,
        string? recipientName,
        string verificationCode,
        TimeSpan expiresIn)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recipientEmail);
        ArgumentException.ThrowIfNullOrWhiteSpace(verificationCode);

        if (expiresIn <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(expiresIn), "Verification code expiry must be greater than zero.");
        }

        var trimmedName = string.IsNullOrWhiteSpace(recipientName) ? "there" : recipientName.Trim();
        var trimmedCode = verificationCode.Trim();
        var expiryText = FormatExpiry(expiresIn);

        var plainTextBody = $@"Hi {trimmedName},

Thanks for joining LoafNCatting.
Your verification code is: {trimmedCode}

This code expires in {expiryText}.

If you did not request this email, you can safely ignore it.

- LoafNCatting";

        var htmlBody =
$$"""
<!DOCTYPE html>
<html lang="en">
<body style="margin:0;padding:24px;background:#f7f7f7;font-family:Arial,Helvetica,sans-serif;color:#1f2937;">
  <div style="max-width:560px;margin:0 auto;background:#ffffff;border:1px solid #e5e7eb;border-radius:16px;padding:32px;">
    <p style="margin:0 0 16px;">Hi {{WebUtility.HtmlEncode(trimmedName)}},</p>
    <p style="margin:0 0 16px;">Thanks for joining <strong>LoafNCatting</strong>.</p>
    <p style="margin:0 0 12px;">Use this verification code to confirm your account:</p>
    <div style="margin:0 0 20px;padding:16px;border-radius:12px;background:#111827;color:#ffffff;text-align:center;font-size:32px;font-weight:700;letter-spacing:8px;">
      {{WebUtility.HtmlEncode(trimmedCode)}}
    </div>
    <p style="margin:0 0 16px;">This code expires in <strong>{{WebUtility.HtmlEncode(expiryText)}}</strong>.</p>
    <p style="margin:0;color:#6b7280;font-size:14px;">If you did not request this email, you can safely ignore it.</p>
  </div>
</body>
</html>
""";

        return new MailMessageRequest(
            new MailRecipient(recipientEmail.Trim(), string.IsNullOrWhiteSpace(recipientName) ? null : recipientName.Trim()),
            Subject,
            plainTextBody,
            htmlBody);
    }

    private static string FormatExpiry(TimeSpan expiresIn)
    {
        var totalMinutes = Math.Max(1, (int)Math.Ceiling(expiresIn.TotalMinutes));
        if (totalMinutes % 60 == 0)
        {
            var hours = totalMinutes / 60;
            return hours == 1 ? "1 hour" : $"{hours} hours";
        }

        return totalMinutes == 1 ? "1 minute" : $"{totalMinutes} minutes";
    }
}
