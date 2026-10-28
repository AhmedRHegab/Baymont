namespace Baymont.Application.Services;

/// <summary>Shared state of the email reader: shown to admins and used to ask for an immediate check.</summary>
public class InboxStatus
{
    private readonly SemaphoreSlim _wake = new(0, 1);

    public bool Enabled { get; set; }
    public string? Mailbox { get; set; }
    public int PollSeconds { get; set; }
    public DateTimeOffset? LastPollAt { get; set; }
    public string? LastError { get; set; }
    public bool Polling { get; set; }

    /// <summary>Wakes the reader before its next scheduled check.</summary>
    public void RequestCheck()
    {
        try { _wake.Release(); }
        catch (SemaphoreFullException) { /* a check is already queued */ }
    }

    public Task<bool> WaitAsync(TimeSpan timeout, CancellationToken ct) => _wake.WaitAsync(timeout, ct);
}
