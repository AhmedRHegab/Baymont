using Baymont.Application.Common;
using Baymont.Application.Dtos;
using Baymont.Application.Interfaces;
using Baymont.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Baymont.Application.Services;

public class DeliveryService(IAppDbContext db, ICurrentUser currentUser)
{
    public async Task<DeliveryListDto> ListAsync(
        string? query, DateOnly? from, DateOnly? to, bool? received, CancellationToken ct = default)
    {
        var q = (query ?? string.Empty).Trim().ToLowerInvariant();
        var rows = db.Deliveries.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            rows = rows.Where(d =>
                d.UnitNumber.ToLower().Contains(q) ||
                d.CustomerName.ToLower().Contains(q) ||
                d.HandoverEngineerName.ToLower().Contains(q));
        }
        if (from is not null) rows = rows.Where(d => d.DeliveryDate >= from);
        if (to is not null) rows = rows.Where(d => d.DeliveryDate <= to);
        if (received is not null) rows = rows.Where(d => d.IsReceived == received);

        var items = await rows
            .OrderByDescending(d => d.DeliveryDate)
            .ThenByDescending(d => d.CreatedAt)
            .Take(300)
            .ToListAsync(ct);

        // Totals describe the whole filtered set, not just the page shown.
        var total = await rows.CountAsync(ct);
        var receivedCount = await rows.CountAsync(d => d.IsReceived, ct);
        var furnished = await rows.CountAsync(d => d.IsFurnished, ct);

        return new DeliveryListDto(
            items.Select(Map).ToList(), total, receivedCount, total - receivedCount, furnished);
    }

    public async Task<DeliveryDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var d = await db.Deliveries.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException();
        return Map(d);
    }

    public async Task<DeliveryDto> CreateAsync(SaveDeliveryRequest request, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var delivery = new Delivery { Id = Guid.NewGuid(), CreatedAt = now, UpdatedAt = now };
        await Apply(delivery, request, ct);

        db.Deliveries.Add(delivery);
        await SaveAsync(ct);
        return await GetAsync(delivery.Id, ct);
    }

    public async Task<DeliveryDto> UpdateAsync(Guid id, SaveDeliveryRequest request, CancellationToken ct = default)
    {
        var delivery = await db.Deliveries.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException();

        if (delivery.RowVersion != request.RowVersion)
            throw new ConcurrencyException();

        await Apply(delivery, request, ct);
        delivery.UpdatedAt = DateTimeOffset.UtcNow;

        await SaveAsync(ct);
        return await GetAsync(delivery.Id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var delivery = await db.Deliveries.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException();
        SoftDelete.Apply(delivery, currentUser.UserId);
        delivery.UpdatedAt = DateTimeOffset.UtcNow;
        await SaveAsync(ct);
    }

    private async Task Apply(Delivery d, SaveDeliveryRequest r, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(r.UnitNumber)) throw new ArgumentException("رقم الوحدة مطلوب.");
        if (string.IsNullOrWhiteSpace(r.CustomerName)) throw new ArgumentException("اسم العميل مطلوب.");
        if (string.IsNullOrWhiteSpace(r.HandoverEngineerName)) throw new ArgumentException("اسم مهندس التسليم مطلوب.");
        if (!r.IsReceived && string.IsNullOrWhiteSpace(r.NotReceivedReason))
            throw new ArgumentException("اذكر سبب عدم الاستلام.");
        if (r.UnitId is not null && !await db.Units.AnyAsync(u => u.Id == r.UnitId, ct))
            throw new NotFoundException("الوحدة غير موجودة.");

        d.DeliveryDate = r.DeliveryDate;
        d.UnitId = r.UnitId;
        d.UnitNumber = r.UnitNumber.Trim();
        d.CustomerName = r.CustomerName.Trim();
        d.HandoverEngineerName = r.HandoverEngineerName.Trim();
        d.IsReceived = r.IsReceived;
        d.NotReceivedReason = r.IsReceived ? null : r.NotReceivedReason!.Trim();
        d.IsFurnished = r.IsFurnished;
        d.Comments = NullIfWhiteSpace(r.Comments);
        d.Complaints = NullIfWhiteSpace(r.Complaints);
    }

    private async Task SaveAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyException();
        }
    }

    private static DeliveryDto Map(Delivery d) => new(
        d.Id, d.DeliveryDate, d.UnitId, d.UnitNumber, d.CustomerName, d.HandoverEngineerName,
        d.IsReceived, d.NotReceivedReason, d.IsFurnished, d.Comments, d.Complaints, d.RowVersion);

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
