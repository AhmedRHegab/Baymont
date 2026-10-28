using Baymont.Application.Common;
using Baymont.Application.Dtos;
using Baymont.Application.Interfaces;
using Baymont.Domain.Entities;
using Baymont.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Baymont.Application.Services;

public class ReservationService(IAppDbContext db, ICurrentUser currentUser)
{
    // ---------- list / get ----------

    public async Task<IReadOnlyList<ReservationDto>> ListAsync(
        ReservationStatus status, string? query, CancellationToken ct = default)
    {
        var q = (query ?? string.Empty).Trim().ToLowerInvariant();
        var rows = db.Reservations.AsNoTracking().Include(r => r.Movements).Where(r => r.Status == status);

        if (q.Length > 0)
            rows = rows.Where(r => r.UnitCode.ToLower().Contains(q) || r.GuestNames.ToLower().Contains(q));

        var list = await rows
            .OrderByDescending(r => r.CheckInDate)
            .ThenByDescending(r => r.CreatedAt)
            .Take(300)
            .ToListAsync(ct);
        return list.Select(Map).ToList();
    }

    public async Task<ReservationDto> GetAsync(Guid id, CancellationToken ct = default)
        => Map(await db.Reservations.AsNoTracking().Include(r => r.Movements).FirstOrDefaultAsync(r => r.Id == id, ct)
               ?? throw new NotFoundException());

    public Task<int> PendingCountAsync(CancellationToken ct = default)
        => db.Reservations.CountAsync(r => r.Status == ReservationStatus.PendingReview, ct);

    // ---------- create / update / review ----------

    public async Task<ReservationDto> CreateAsync(SaveReservationRequest request, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var r = new Reservation { Id = Guid.NewGuid(), Source = "Manual", SourceSentAt = now, CreatedAt = now };
        await ApplyAsync(r, request, ct);
        db.Reservations.Add(r);
        await db.SaveChangesAsync(ct);
        return Map(r);
    }

    public async Task<ReservationDto> UpdateAsync(Guid id, SaveReservationRequest request, CancellationToken ct = default)
    {
        var r = await LoadAsync(id, ct);
        if (r.Status == ReservationStatus.Rejected && !request.Approve)
            throw new ValidationException("الحجز مرفوض. اعتمده أولاً إن أردت تعديله.");
        await ApplyAsync(r, request, ct);
        await db.SaveChangesAsync(ct);
        return Map(r);
    }

    public async Task RejectAsync(Guid id, CancellationToken ct = default)
    {
        var r = await LoadAsync(id, ct);
        if (r.Movements.Count > 0)
            throw new ValidationException("لا يمكن رفض حجز سُجّل دخوله.");
        r.Status = ReservationStatus.Rejected;
        r.ReviewedBy = currentUser.UserName;
        r.ReviewedAt = DateTimeOffset.UtcNow;
        r.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var r = await LoadAsync(id, ct);
        db.Reservations.Remove(r);
        await db.SaveChangesAsync(ct);
    }

    private async Task ApplyAsync(Reservation r, SaveReservationRequest q, CancellationToken ct)
    {
        var code = (q.UnitCode ?? string.Empty).Trim();
        var names = (q.GuestNames ?? string.Empty).Trim();
        if (code.Length == 0) throw new ValidationException("كود الوحدة مطلوب.");
        if (names.Length == 0) throw new ValidationException("اسم الضيف مطلوب.");
        if (q.PersonsCount < 1 || q.PersonsCount > 100) throw new ValidationException("عدد الأشخاص غير صالح.");
        if (q.Type is not null && !Enum.IsDefined(q.Type.Value)) throw new ValidationException("نوع الزيارة غير صالح.");
        Check.Date(q.CheckInDate, "تاريخ بداية التصريح");
        Check.Date(q.CheckOutDate, "تاريخ نهاية التصريح");
        if (q.CheckInDate is not null && q.CheckOutDate is not null && q.CheckOutDate < q.CheckInDate)
            throw new ValidationException("تاريخ نهاية التصريح قبل تاريخ بدايته.");
        // Permits are weekly; a year or more is almost certainly a typo in the date.
        if (q.CheckInDate is { } from && q.CheckOutDate is { } to && to.DayNumber - from.DayNumber > 366)
            throw new ValidationException("فترة التصريح أطول من سنة. راجع التاريخين.");

        if (q.Approve)
        {
            if (q.Type is null) throw new ValidationException("اختر نوع الزيارة قبل الاعتماد.");
            if (q.CheckInDate is null || q.CheckOutDate is null)
                throw new ValidationException("أدخل تاريخ الوصول وتاريخ المغادرة قبل الاعتماد.");
        }

        r.UnitCode = code;
        r.UnitId = await FindUnitIdAsync(code, ct);
        r.GuestNames = names;
        r.PersonsCount = q.PersonsCount;
        r.Type = q.Type;
        r.CheckInDate = q.CheckInDate;
        r.CheckOutDate = q.CheckOutDate;
        r.PeriodEstimated = false; // a person has now seen and saved the dates
        r.Notes = string.IsNullOrWhiteSpace(q.Notes) ? null : q.Notes.Trim();
        r.UpdatedAt = DateTimeOffset.UtcNow;

        if (q.Approve && r.Status != ReservationStatus.Approved)
        {
            r.Status = ReservationStatus.Approved;
            r.ReviewedBy = currentUser.UserName;
            r.ReviewedAt = DateTimeOffset.UtcNow;
        }
    }

    private async Task<Guid?> FindUnitIdAsync(string code, CancellationToken ct)
    {
        var lower = code.ToLowerInvariant();
        return await db.Units.AsNoTracking()
            .Where(u => u.ConstructionNumber != null && u.ConstructionNumber.ToLower() == lower)
            .Select(u => (Guid?)u.Id)
            .FirstOrDefaultAsync(ct);
    }

    // ---------- gate movement: enter, exit, final leave ----------

    /// <summary>The guests go in. Not allowed once the final leave was recorded.</summary>
    public async Task EnterAsync(Guid id, CancellationToken ct = default)
    {
        var r = await LoadApprovedAsync(id, ct);
        if (r.FinalDepartureAt is not null)
            throw new ValidationException("سُجلت مغادرته النهائية، ولا يمكنه الدخول مرة أخرى إلا بتصريح جديد يصل بالبريد.");
        if (IsInside(r)) throw new ValidationException("مسجل داخل الموقع بالفعل.");

        db.ReservationMovements.Add(new ReservationMovement
        {
            Id = Guid.NewGuid(),
            ReservationId = r.Id,
            EnteredAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(ct);
    }

    /// <summary>The guests step out during the permit and may come back.</summary>
    public async Task ExitAsync(Guid id, CancellationToken ct = default)
    {
        var r = await LoadApprovedAsync(id, ct);
        var open = OpenMovement(r) ?? throw new ValidationException("غير مسجل داخل الموقع.");
        open.ExitedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>The guests leave for good. They cannot enter again without a new permit.</summary>
    public async Task FinalLeaveAsync(Guid id, CancellationToken ct = default)
    {
        var r = await LoadApprovedAsync(id, ct);
        if (r.FinalDepartureAt is not null) throw new ValidationException("تم تسجيل المغادرة النهائية بالفعل.");
        if (r.Movements.Count == 0) throw new ValidationException("لم يُسجل دخوله بعد.");

        var now = DateTimeOffset.UtcNow;
        var open = OpenMovement(r);
        if (open is not null)
        {
            open.ExitedAt = now;
            open.IsFinal = true;
        }
        r.FinalDepartureAt = now;
        r.UpdatedAt = now;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Undo the last thing recorded (a mistaken tap).</summary>
    public async Task UndoAsync(Guid id, CancellationToken ct = default)
    {
        var r = await LoadApprovedAsync(id, ct);
        var last = r.Movements.OrderByDescending(m => m.EnteredAt).FirstOrDefault()
            ?? throw new ValidationException("لا يوجد تسجيل للتراجع عنه.");

        if (r.FinalDepartureAt is not null)
        {
            r.FinalDepartureAt = null;
            if (last.IsFinal)
            {
                last.IsFinal = false;
                last.ExitedAt = null;
            }
        }
        else if (last.ExitedAt is null)
        {
            db.ReservationMovements.Remove(last);
        }
        else
        {
            last.ExitedAt = null;
        }
        r.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    private async Task<Reservation> LoadAsync(Guid id, CancellationToken ct)
        => await db.Reservations.Include(x => x.Movements).FirstOrDefaultAsync(x => x.Id == id, ct)
           ?? throw new NotFoundException();

    private async Task<Reservation> LoadApprovedAsync(Guid id, CancellationToken ct)
    {
        var r = await LoadAsync(id, ct);
        if (r.Status != ReservationStatus.Approved) throw new ValidationException("الحجز غير معتمد.");
        return r;
    }

    private static ReservationMovement? OpenMovement(Reservation r)
        => r.Movements.Where(m => m.ExitedAt is null).OrderByDescending(m => m.EnteredAt).FirstOrDefault();

    private static bool IsInside(Reservation r) => r.Movements.Any(m => m.ExitedAt is null);

    // ---------- import ----------

    public async Task<ReservationImportResultDto> ImportAsync(
        Stream file, ReservationSource source, CancellationToken ct = default)
    {
        var parsed = ReservationExcelParser.Parse(file);
        var issues = parsed.Issues.ToList();
        if (parsed.Rows.Count == 0)
            return new ReservationImportResultDto(0, 0, issues);

        var now = DateTimeOffset.UtcNow;
        var sentAt = source.SentAt ?? now;

        // The sheet has no dates: permits run Friday to Friday, so guess the period from the day the email was sent.
        // The reviewer sees that it is an estimate and confirms or corrects it.
        var estimatedStart = EgyptClock.NearestFriday(EgyptClock.DateOf(sentAt));
        var estimatedEnd = estimatedStart.AddDays(7);

        // The subject or file name may say what kind of visit this is (استضافة / تأجير / سيلز / مفوض).
        var typeFromMail = ReservationExcelParser.ParseType($"{source.Subject} {source.FileName}");

        // The same permit sent twice must not be created twice (same unit, same guests, same check-in) —
        // unless the earlier one already left for good: then this is a new permit.
        var codes = parsed.Rows.Select(p => p.UnitCode.ToLowerInvariant()).Distinct().ToList();
        var existing = await db.Reservations.AsNoTracking()
            .Where(r => codes.Contains(r.UnitCode.ToLower()) && r.FinalDepartureAt == null)
            .Select(r => new { r.UnitCode, r.GuestNames, r.CheckInDate })
            .ToListAsync(ct);
        var seen = existing.Select(e => Key(e.UnitCode, e.GuestNames, e.CheckInDate)).ToHashSet();

        var units = await db.Units.AsNoTracking()
            .Where(u => u.ConstructionNumber != null)
            .Select(u => new { u.Id, Code = u.ConstructionNumber! })
            .ToListAsync(ct);
        var unitByCode = units.GroupBy(u => u.Code.Trim().ToLowerInvariant()).ToDictionary(g => g.Key, g => g.First().Id);

        int created = 0, duplicates = 0;

        foreach (var p in parsed.Rows)
        {
            var noDates = p.CheckIn is null && p.CheckOut is null;
            var checkIn = noDates ? estimatedStart : p.CheckIn;
            var checkOut = noDates ? estimatedEnd : p.CheckOut;

            if (!seen.Add(Key(p.UnitCode, p.GuestNames, checkIn)))
            {
                duplicates++;
                continue;
            }

            db.Reservations.Add(new Reservation
            {
                Id = Guid.NewGuid(),
                UnitCode = p.UnitCode,
                UnitId = unitByCode.TryGetValue(p.UnitCode.ToLowerInvariant(), out var uid) ? uid : null,
                GuestNames = p.GuestNames,
                PersonsCount = p.PersonsCount,
                Type = p.Type ?? typeFromMail,
                CheckInDate = checkIn,
                CheckOutDate = checkOut,
                PeriodEstimated = noDates,
                Status = ReservationStatus.PendingReview,
                Source = source.Kind,
                SourceFrom = source.From,
                SourceSubject = source.Subject,
                SourceFileName = source.FileName,
                SourceMessageId = source.MessageId,
                SourceSentAt = sentAt,
                CreatedAt = now,
                UpdatedAt = now
            });
            created++;
        }

        if (created > 0) await db.SaveChangesAsync(ct);
        return new ReservationImportResultDto(created, duplicates, issues);
    }

    private static string Key(string unit, string names, DateOnly? checkIn)
        => $"{unit.Trim().ToLowerInvariant()}|{string.Join(' ', names.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant()}|{checkIn:yyyy-MM-dd}";

    // ---------- dashboard ----------

    public async Task<ReservationDashboardDto> DashboardAsync(DateOnly today, DateOnly? start, CancellationToken ct = default)
    {
        var approved = await db.Reservations.AsNoTracking()
            .Include(r => r.Movements)
            .Where(r => r.Status == ReservationStatus.Approved)
            .ToListAsync(ct);

        // Permit weeks run Friday to Friday.
        var weekStart = EgyptClock.FridayOnOrBefore(today);
        var weekEnd = weekStart.AddDays(7);

        bool InWeek(Reservation r, DateOnly s, DateOnly e) => r.CheckInDate is { } d && d >= s && d < e;
        bool Entered(Reservation r) => r.Movements.Count > 0;
        bool Left(Reservation r) => r.FinalDepartureAt is not null;
        // Late: the permit ended, the guest did come in, and no final leave was recorded.
        bool Late(Reservation r) => Entered(r) && !Left(r) && r.CheckOutDate is { } d && d < today;
        CountPair Pair(IEnumerable<Reservation> rs)
        {
            var l = rs.ToList();
            return new CountPair(l.Count, l.Sum(x => x.PersonsCount));
        }

        var thisWeek = approved.Where(r => InWeek(r, weekStart, weekEnd)).ToList();
        var late = approved.Where(Late).ToList();

        var first = EgyptClock.FridayOnOrBefore(start ?? weekStart);
        var weeks = new List<WeekRowDto>();
        for (var i = 0; i < 13; i++)
        {
            var s = first.AddDays(7 * i);
            var e = s.AddDays(7);
            var inWeek = approved.Where(r => InWeek(r, s, e)).ToList();
            weeks.Add(new WeekRowDto(
                i + 1, s, e,
                inWeek.Count,
                inWeek.Sum(r => r.PersonsCount),
                inWeek.Count(Entered),
                inWeek.Count(Left),
                inWeek.Count(IsInside),
                inWeek.Count(Late)));
        }

        return new ReservationDashboardDto(
            today,
            weekStart,
            await PendingCountAsync(ct),
            Pair(thisWeek),
            Pair(thisWeek.Where(Entered)),
            Pair(thisWeek.Where(Left)),
            Pair(approved.Where(IsInside)),
            Pair(late),
            late.OrderBy(r => r.CheckOutDate).Select(Map).ToList(),
            weeks);
    }

    private static ReservationDto Map(Reservation r) => new(
        r.Id, r.UnitCode, r.UnitId, r.GuestNames, r.PersonsCount, r.Type, r.CheckInDate, r.CheckOutDate,
        r.PeriodEstimated, r.Notes, r.Status, IsInside(r), r.FinalDepartureAt,
        r.Movements.OrderBy(m => m.EnteredAt).Select(m => new MovementDto(m.Id, m.EnteredAt, m.ExitedAt, m.IsFinal)).ToList(),
        r.Source, r.SourceFrom, r.SourceSubject, r.SourceFileName, r.SourceSentAt, r.CreatedAt);
}
