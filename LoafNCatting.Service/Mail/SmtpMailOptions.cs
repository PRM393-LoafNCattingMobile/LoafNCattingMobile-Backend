using System.ComponentModel.DataAnnotations;

namespace LoafNCatting.Service.Mail;

public sealed class SmtpMailOptions
{
    public const string SectionName = "Mail:Smtp";

    [Required]
    public string Host { get; init; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; init; } = 587;

    public bool EnableSsl { get; init; } = true;

    public string? Username { get; init; }

    public string? Password { get; init; }

    [Required]
    [EmailAddress]
    public string FromEmail { get; init; } = string.Empty;

    public string? FromName { get; init; }
}
