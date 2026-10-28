using Baymont.Application.Dtos;
using Baymont.Application.Interfaces;
using Baymont.Application.Services;
using Baymont.Domain.Entities;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Baymont.Infrastructure.Mail;

/// <summary>
/// Reads the permit mailbox. For every new email from an allowed sender it takes the .xlsx
/// attachments and sends them to the same importer as a manual upload: permits wait in the review queue.
/// Mail from other senders is recorded and left untouched (unread) in the mailbox.
/// </summary>
public class InboxPollingService(
    IOptions<MailOptions> options,
    InboxStatus status,
    IServiceScopeFactory scopes,
    ILogger<InboxPollingService> logger) : BackgroundService
{
    private const long MaxAttachmentBytes = 5 * 1024 * 1024;
    private readonly MailOptions _o = options.Value;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        status.PollSeconds = Math.Max(60, _o.PollSeconds);
        status.Mailbox = _o.User;

        if (!_o.IsConfigured)
        {
            logger.LogInformation("Email reader is off: set Mail__User and Mail__Password to turn it on.");
            return;
        }
        if (_o.Senders.Count == 0)
        {
            status.LastError = "لم يتم تحديد المرسلين المسموح بهم (Mail__AllowedSenders).";
            logger.LogWarning("Email reader is off: Mail__AllowedSenders is empty, so every email would be refused.");
            return;
        }

        status.Enabled = true;
        logger.LogInformation("Email reader on for {User}, every {Seconds}s, senders: {Senders}",
            _o.User, status.PollSeconds, string.Join(", ", _o.Senders));

        // Short delay so the app finishes starting (migrations, seeding) before the first check.
        await Task.Delay(TimeSpan.FromSeconds(15), ct).ConfigureAwait(false);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                status.Polling = true;
                await PollAsync(ct);
                status.LastError = null;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                // Never print the password; MailKit messages do not include it.
                status.LastError = ex.Message;
                logger.LogWarning(ex, "Email check failed; will retry.");
            }
            finally
            {
                status.Polling = false;
                status.LastPollAt = DateTimeOffset.UtcNow;
            }

            try { await status.WaitAsync(TimeSpan.FromSeconds(status.PollSeconds), ct); }
            catch (OperationCanceledException) { return; }
        }
    }

    private async Task PollAsync(CancellationToken ct)
    {
        using var client = new ImapClient();
        await client.ConnectAsync(_o.Host, _o.Port,
            _o.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.None, ct);
        await client.AuthenticateAsync(_o.User!, _o.Password!, ct);

        var inbox = client.Inbox;
        await inbox.OpenAsync(FolderAccess.ReadWrite, ct);

        var since = DateTime.UtcNow.AddDays(-Math.Max(1, _o.LookbackDays));
        var uids = await inbox.SearchAsync(SearchQuery.NotSeen.And(SearchQuery.DeliveredAfter(since)), ct);
        if (uids.Count == 0)
        {
            await client.DisconnectAsync(true, ct);
            return;
        }

        var summaries = await inbox.FetchAsync(uids, MessageSummaryItems.UniqueId | MessageSummaryItems.Envelope, ct);

        foreach (var item in summaries)
        {
            ct.ThrowIfCancellationRequested();
            var messageId = item.Envelope?.MessageId ?? $"uid:{item.UniqueId}@{_o.User}";

            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            if (await db.InboxMessages.AnyAsync(m => m.MessageId == messageId, ct)) continue;

            var from = item.Envelope?.From.Mailboxes.FirstOrDefault()?.Address;
            var subject = item.Envelope?.Subject;
            var received = item.Envelope?.Date ?? DateTimeOffset.UtcNow;

            async Task Record(string st, string? detail, int created)
            {
                db.InboxMessages.Add(new InboxMessage
                {
                    Id = Guid.NewGuid(),
                    MessageId = messageId,
                    FromAddress = Cut(from, 256),
                    Subject = Cut(subject, 512),
                    ReceivedAt = received,
                    ProcessedAt = DateTimeOffset.UtcNow,
                    Status = st,
                    Detail = Cut(detail, 1024),
                    PermitsCreated = created
                });
                await db.SaveChangesAsync(ct);
            }

            // Mail from anyone else is none of our business: log it, leave it unread, never download it.
            if (!_o.IsAllowed(from))
            {
                await Record("Ignored", "المرسل غير مسموح به", 0);
                continue;
            }

            try
            {
                var message = await inbox.GetMessageAsync(item.UniqueId, ct);

                if (_o.RequireAuthentication && !PassedDmarc(message))
                {
                    await Record("Ignored", "فشل التحقق من هوية المرسل (DMARC)، قد تكون رسالة مزورة", 0);
                    continue;
                }

                var files = await ReadWorkbooksAsync(message, ct);
                if (files.Count == 0)
                {
                    await Record("Ignored", "لا يوجد مرفق Excel (xlsx) في الرسالة", 0);
                    continue;
                }

                var reservations = scope.ServiceProvider.GetRequiredService<ReservationService>();
                var created = 0;
                var notes = new List<string>();
                foreach (var (name, bytes) in files)
                {
                    using var ms = new MemoryStream(bytes);
                    var result = await reservations.ImportAsync(
                        ms, new ReservationSource("Email", from, subject, name, messageId, received), ct);
                    created += result.Created;
                    notes.Add($"{name}: {result.Created} جديد" +
                              (result.Duplicates > 0 ? $"، {result.Duplicates} مكرر" : "") +
                              (result.Issues.Count > 0 ? $"، {result.Issues[0].Message}" : ""));
                }

                await Record(created > 0 ? "Imported" : "Ignored", string.Join(" | ", notes), created);
                await inbox.AddFlagsAsync(item.UniqueId, MessageFlags.Seen, true, ct);
                logger.LogInformation("Email from {From}: {Created} permit(s) added for review.", from, created);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not process email from {From}.", from);
                await Record("Failed", ex.Message, 0);
                await inbox.AddFlagsAsync(item.UniqueId, MessageFlags.Seen, true, ct);
            }
        }

        await client.DisconnectAsync(true, ct);
    }

    private static async Task<List<(string Name, byte[] Bytes)>> ReadWorkbooksAsync(MimeMessage message, CancellationToken ct)
    {
        var list = new List<(string, byte[])>();
        foreach (var attachment in message.Attachments)
        {
            if (attachment is not MimePart part) continue;
            var name = part.FileName ?? part.ContentType.Name ?? "";
            if (!name.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)) continue;

            using var ms = new MemoryStream();
            await part.Content.DecodeToAsync(ms, ct);
            if (ms.Length is 0 or > MaxAttachmentBytes) continue;
            list.Add((name, ms.ToArray()));
        }
        return list;
    }

    /// <summary>Gmail writes its own Authentication-Results line at the top of every message.</summary>
    private static bool PassedDmarc(MimeMessage message)
    {
        var header = message.Headers.FirstOrDefault(h => h.Id == HeaderId.AuthenticationResults)?.Value;
        return header is not null && header.Contains("dmarc=pass", StringComparison.OrdinalIgnoreCase);
    }

    private static string? Cut(string? s, int max) => s is null ? null : s.Length <= max ? s : s[..max];
}
