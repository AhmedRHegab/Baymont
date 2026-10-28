namespace Baymont.Infrastructure.Mail;

/// <summary>"Mail" section of the configuration. The reader is off until User and Password are set.</summary>
public class MailOptions
{
    public string Host { get; set; } = "imap.gmail.com";
    public int Port { get; set; } = 993;

    /// <summary>False only for a local test server. Gmail needs true.</summary>
    public bool UseSsl { get; set; } = true;
    public string? User { get; set; }
    public string? Password { get; set; }

    /// <summary>Comma or semicolon separated. Full addresses, or whole domains written as "@example.com".</summary>
    public string? AllowedSenders { get; set; }

    public int PollSeconds { get; set; } = 300;

    /// <summary>Only accept mail whose sender domain passed DMARC (Gmail records this), so forged "From" is refused.</summary>
    public bool RequireAuthentication { get; set; } = true;

    public int LookbackDays { get; set; } = 14;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(User) && !string.IsNullOrWhiteSpace(Password);

    public IReadOnlyList<string> Senders => (AllowedSenders ?? string.Empty)
        .Split([',', ';', ' ', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(s => s.ToLowerInvariant())
        .ToList();

    public bool IsAllowed(string? address)
    {
        if (string.IsNullOrWhiteSpace(address)) return false;
        var a = address.Trim().ToLowerInvariant();
        return Senders.Any(s => s.StartsWith('@') ? a.EndsWith(s) : a == s);
    }
}
