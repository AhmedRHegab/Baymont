using Baymont.Application.Common;
using Baymont.Domain.Entities;
using Baymont.Domain.Enums;
using Baymont.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Baymont.Infrastructure.Seed;

/// <summary>
/// Sample data for training and demos (all names are invented). Created once, only into an empty database,
/// and tracked in demo_records so an admin can hard-delete it with one button.
/// </summary>
public static class DemoDataSeeder
{
    /// <summary>
    /// Creates the sample data in one save and records every new row so it can be deleted again.
    /// Used by the startup seeding and by the admin "load sample data" button.
    /// </summary>
    public static async Task<int> CreateAsync(AppDbContext db, CancellationToken ct = default)
    {
        Build(db);

        // Remember every record we just added (children included) before saving.
        var added = db.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added)
            .Select(e => (Type: e.Entity.GetType().Name, Id: (Guid)e.Property("Id").CurrentValue!))
            .ToList();
        foreach (var (type, id) in added)
            db.DemoRecords.Add(new DemoRecord { Id = Guid.NewGuid(), EntityType = type, EntityId = id });

        // The marker stops the startup seeding from running again, whatever happens to the sample data.
        if (!await db.DemoRecords.AnyAsync(d => d.EntityType == DemoRecord.MarkerType, ct))
            db.DemoRecords.Add(new DemoRecord { Id = Guid.NewGuid(), EntityType = DemoRecord.MarkerType, EntityId = Guid.Empty });

        await db.SaveChangesAsync(ct);
        return added.Count;
    }

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DemoDataSeeder");
        if (!config.GetValue<bool>("Seed:DemoData")) return;

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Already seeded once (even if deleted since): never again.
        if (await db.DemoRecords.AnyAsync(d => d.EntityType == DemoRecord.MarkerType)) return;

        // Never mix sample data into a database that already holds real records.
        var hasData =
            await db.Owners.IgnoreQueryFilters().AnyAsync() ||
            await db.Units.IgnoreQueryFilters().AnyAsync() ||
            await db.Contractors.IgnoreQueryFilters().AnyAsync() ||
            await db.Deliveries.IgnoreQueryFilters().AnyAsync() ||
            await db.Reservations.AnyAsync() ||
            await db.StaffMembers.AnyAsync();
        if (hasData)
        {
            logger.LogInformation("Sample data skipped: the database already has records.");
            return;
        }

        var count = await CreateAsync(db);
        logger.LogInformation("Sample data created ({Count} records). An admin can delete it from the banner.", count);
    }

    private static void Build(AppDbContext db)
    {
        var now = DateTimeOffset.UtcNow;
        var today = EgyptClock.Today;
        var friday = EgyptClock.FridayOnOrBefore(today); // permit weeks run Friday to Friday

        // ---------- owners and units ----------
        (string Name, string Code, string Terrace, string Building, string Floor, string Apt, UnitStatus Status)[] people =
        [
            ("سامي حسن إبراهيم", "18-C5-1-1", "1", "4", "1", "1", UnitStatus.Delivered),
            ("منى عبد الرحمن", "25B-C12-0-2", "1", "2", "2", "2", UnitStatus.Delivered),
            ("خالد مصطفى سعيد", "9-C2-3-1", "1", "3", "الأرضي", "1", UnitStatus.Delivered),
            ("هالة فتحي يوسف", "41-C7-2-1", "1", "35", "3", "1", UnitStatus.Active),
            ("ياسر محمود علي", "12B-C3-1-2", "2", "10", "3", "4", UnitStatus.Delivered),
            ("نهى إسماعيل حسين", "7-C1-0-1", "2", "44", "2", "1", UnitStatus.Reserved),
            ("رامي عادل نصر", "20-C8-2-1", "2", "43", "1", "1", UnitStatus.Delivered),
            ("دعاء سليمان فؤاد", "30-C6-1-2", "2", "11", "1", "2", UnitStatus.Active)
        ];

        var owners = new List<Owner>();
        var units = new List<Unit>();
        for (var i = 0; i < people.Length; i++)
        {
            var p = people[i];
            var owner = new Owner
            {
                Id = Guid.NewGuid(),
                InternalCode = $"DEMO-{i + 1:000}",
                FullName = p.Name,
                Phone = $"0100000{i + 1:0000}",
                CreatedAt = now,
                UpdatedAt = now
            };
            owners.Add(owner);
            units.Add(new Unit
            {
                Id = Guid.NewGuid(),
                ConstructionNumber = p.Code,
                Terrace = p.Terrace,
                Building = p.Building,
                Floor = p.Floor,
                Apartment = p.Apt,
                OwnerId = owner.Id,
                Status = p.Status,
                CreatedAt = now,
                UpdatedAt = now
            });
        }
        db.Owners.AddRange(owners);
        db.Units.AddRange(units);

        // ---------- vehicles ----------
        (string Plate, string Make, string Model, string Color, int Who)[] cars =
        [
            ("أ ب ج 1234", "تويوتا", "كورولا", "أبيض", 0),
            ("س ص ع 5678", "هيونداي", "إلنترا", "أسود", 1),
            ("م ن ل 4321", "نيسان", "صني", "فضي", 4),
            ("ق ر ش 8765", "كيا", "سيراتو", "رمادي", 6)
        ];
        foreach (var c in cars)
            db.Vehicles.Add(new Vehicle
            {
                Id = Guid.NewGuid(),
                PlateNumber = c.Plate,
                PlateNumberNormalized = PlateNormalizer.Normalize(c.Plate),
                Make = c.Make,
                Model = c.Model,
                Color = c.Color,
                OwnerId = owners[c.Who].Id,
                UnitId = units[c.Who].Id,
                CreatedAt = now,
                UpdatedAt = now
            });

        // ---------- contractors ----------
        db.Contractors.Add(new Contractor
        {
            Id = Guid.NewGuid(),
            CompanyName = "شركة الأمل للمقاولات",
            ContractorName = "أحمد المقاول",
            Phone = "0100000100",
            Assignment = "تشطيب فيلات المصطبة 1",
            WorkStartDate = today.AddDays(-5),
            CreatedAt = now,
            UpdatedAt = now,
            Workers =
            [
                new ContractorWorker { Id = Guid.NewGuid(), Name = "محمود سعد", JobTitle = "سباك", Housing = "سكن العمال 2 - غرفة 4" },
                new ContractorWorker { Id = Guid.NewGuid(), Name = "إبراهيم حسن", JobTitle = "كهربائي", Housing = "سكن العمال 2 - غرفة 4" },
                new ContractorWorker { Id = Guid.NewGuid(), Name = "عصام فؤاد", JobTitle = "نقاش", Housing = "سكن العمال 1 - غرفة 2" },
                new ContractorWorker { Id = Guid.NewGuid(), Name = "هاني رمضان", JobTitle = "عامل", Housing = "سكن العمال 1 - غرفة 2" }
            ],
            Vehicles =
            [
                new ContractorVehicle { Id = Guid.NewGuid(), PlateNumber = "ن ح م 3344", PlateNumberNormalized = PlateNormalizer.Normalize("ن ح م 3344"), IsSiteServiceCar = true, Notes = "سيارة خدمة الموقع" },
                new ContractorVehicle { Id = Guid.NewGuid(), PlateNumber = "ر ك ب 7788", PlateNumberNormalized = PlateNormalizer.Normalize("ر ك ب 7788"), IsSiteServiceCar = false }
            ],
            Equipment =
            [
                new ContractorEquipment { Id = Guid.NewGuid(), Type = "حفار", Number = "EQ-101", Category = EquipmentCategory.Heavy, MovementPermit = "تصريح حركة 55" },
                new ContractorEquipment { Id = Guid.NewGuid(), Type = "مولد كهرباء", Number = "EQ-102", Category = EquipmentCategory.Light },
                new ContractorEquipment { Id = Guid.NewGuid(), Type = "خلاطة أسمنت", Number = "EQ-103", Category = EquipmentCategory.Light }
            ],
            Materials =
            [
                new ContractorMaterial { Id = Guid.NewGuid(), Description = "أسمنت - 200 كيس", StorageLocation = "مخزن 3 - المصطبة 1" },
                new ContractorMaterial { Id = Guid.NewGuid(), Description = "سيراميك أرضيات", StorageLocation = "مخزن 1" }
            ],
            Entries =
            [
                new ContractorEntry { Id = Guid.NewGuid(), EntryDate = today.AddDays(-5), EntryReason = "تشطيب وحدات المصطبة 1", EntryPermit = "تصريح دخول 112" }
            ]
        });
        db.Contractors.Add(new Contractor
        {
            Id = Guid.NewGuid(),
            CompanyName = "شركة النور للكهرباء",
            ContractorName = "سعيد النور",
            Phone = "0100000200",
            Assignment = "صيانة إنارة الطرق",
            WorkStartDate = today.AddDays(-12),
            CreatedAt = now,
            UpdatedAt = now,
            Workers =
            [
                new ContractorWorker { Id = Guid.NewGuid(), Name = "وائل مجدي", JobTitle = "كهربائي", Housing = "سكن العمال 3" },
                new ContractorWorker { Id = Guid.NewGuid(), Name = "تامر لطفي", JobTitle = "فني", Housing = "سكن العمال 3" }
            ],
            Entries =
            [
                new ContractorEntry
                {
                    Id = Guid.NewGuid(), EntryDate = today.AddDays(-12), EntryReason = "صيانة إنارة", EntryPermit = "تصريح دخول 98",
                    ExitDate = today.AddDays(-9), ExitReason = "انتهاء العمل", ExitPermit = "تصريح خروج 98"
                }
            ]
        });

        // ---------- deliveries ----------
        (int Who, string Eng, bool Received, string? Reason, bool Furnished, string? Comments, string? Complaints, int DaysAgo)[] deliveries =
        [
            (0, "م. هاني فتحي", true, null, true, "تم التسليم بحضور المالك", null, 0),
            (1, "م. هاني فتحي", true, null, false, null, "تسريب بسيط في الحمام الرئيسي", 0),
            (2, "م. نادر سعيد", false, "عيوب في التشطيب", false, "طلب إصلاح الدهان قبل الاستلام", "تشققات في الجدار", 1),
            (4, "م. نادر سعيد", true, null, true, null, null, 1),
            (6, "م. هاني فتحي", false, "المالك غير متواجد", false, "إعادة الموعد الأسبوع القادم", null, 2),
            (7, "م. نادر سعيد", true, null, true, "مفروشة بالكامل", null, 3)
        ];
        foreach (var d in deliveries)
            db.Deliveries.Add(new Delivery
            {
                Id = Guid.NewGuid(),
                DeliveryDate = today.AddDays(-d.DaysAgo),
                UnitId = units[d.Who].Id,
                UnitNumber = units[d.Who].ConstructionNumber!,
                CustomerName = owners[d.Who].FullName,
                HandoverEngineerName = d.Eng,
                IsReceived = d.Received,
                NotReceivedReason = d.Reason,
                IsFurnished = d.Furnished,
                Comments = d.Comments,
                Complaints = d.Complaints,
                CreatedAt = now,
                UpdatedAt = now
            });

        // ---------- owners staying overnight (today, morning shift) ----------
        foreach (var i in new[] { 0, 1, 2, 3, 4, 6 })
            db.OvernightStays.Add(new OvernightStay
            {
                Id = Guid.NewGuid(), StayDate = today, Shift = ShiftPeriod.Morning,
                OwnerId = owners[i].Id, UnitId = units[i].Id, CreatedAt = now
            });

        // ---------- our staff ----------
        var s1 = new StaffMember { Id = Guid.NewGuid(), Name = "عمرو صبحي", Company = "باي مونت", JobTitle = "مشرف أمن", PlateNumber = "ع م ر 1111", Phone = "0100000301", CreatedAt = now };
        var s2 = new StaffMember { Id = Guid.NewGuid(), Name = "ماجد رشاد", Company = "باي مونت", JobTitle = "مهندس موقع", PlateNumber = "م ج د 2222", Phone = "0100000302", CreatedAt = now };
        var s3 = new StaffMember { Id = Guid.NewGuid(), Name = "إيهاب نبيل", Company = "باي مونت", JobTitle = "مسؤول صيانة", Phone = "0100000303", CreatedAt = now };
        var s4 = new StaffMember { Id = Guid.NewGuid(), Name = "شيماء لطفي", Company = "باي مونت", JobTitle = "علاقات الملاك", Phone = "0100000304", CreatedAt = now };
        db.StaffMembers.AddRange(s1, s2, s3, s4);
        db.StaffEntries.AddRange(
            new StaffEntry { Id = Guid.NewGuid(), StaffMemberId = s1.Id, EntryAt = now.AddHours(-6) },
            new StaffEntry { Id = Guid.NewGuid(), StaffMemberId = s2.Id, EntryAt = now.AddHours(-5) },
            new StaffEntry { Id = Guid.NewGuid(), StaffMemberId = s3.Id, EntryAt = now.AddHours(-7), ExitAt = now.AddHours(-1) });

        // ---------- today's outside shift and night patrol ----------
        void Shift(ShiftKind kind, string name, string job, string work, string phone, string? notes = null)
            => db.ShiftAssignments.Add(new ShiftAssignment
            {
                Id = Guid.NewGuid(), ShiftDate = today, Kind = kind, Name = name, JobTitle = job,
                AssignedWork = work, Phone = phone, Notes = notes, CreatedAt = now
            });
        Shift(ShiftKind.Outside, "حسام عبد الله", "أمن", "بوابة الدخول الرئيسية", "0100000401");
        Shift(ShiftKind.Outside, "مصطفى كمال", "أمن", "جولة المصطبة 1", "0100000402", "متابعة أعمال المقاول");
        Shift(ShiftKind.NightPatrol, "جمال فوزي", "دورية", "سور الشمال", "0100000403");
        Shift(ShiftKind.NightPatrol, "أيمن شحاتة", "دورية", "المصطبة 2", "0100000404");

        // ---------- permit reservations: one of every state ----------
        Reservation Res(string code, string names, int persons, VisitorType? type, ReservationStatus status,
            DateOnly from, DateOnly to, bool estimated = false, string? subject = null, DateTimeOffset? sent = null)
        {
            var r = new Reservation
            {
                Id = Guid.NewGuid(),
                UnitCode = code,
                UnitId = units.FirstOrDefault(u => u.ConstructionNumber == code)?.Id,
                GuestNames = names,
                PersonsCount = persons,
                Type = type,
                CheckInDate = from,
                CheckOutDate = to,
                PeriodEstimated = estimated,
                Status = status,
                ReviewedBy = status == ReservationStatus.PendingReview ? null : "admin",
                ReviewedAt = status == ReservationStatus.PendingReview ? null : now,
                Source = "Email",
                SourceFrom = "permits@hq.example.com",
                SourceSubject = subject ?? "تصاريح الأسبوع",
                SourceFileName = "permits.xlsx",
                SourceMessageId = $"demo-{Guid.NewGuid():N}@example.com",
                SourceSentAt = sent ?? now.AddDays(-1),
                CreatedAt = now,
                UpdatedAt = now
            };
            db.Reservations.Add(r);
            return r;
        }

        void Move(Reservation r, DateTimeOffset entered, DateTimeOffset? exited = null, bool isFinal = false)
        {
            db.ReservationMovements.Add(new ReservationMovement
            {
                Id = Guid.NewGuid(), ReservationId = r.Id, EnteredAt = entered, ExitedAt = exited, IsFinal = isFinal
            });
            if (isFinal) r.FinalDepartureAt = exited;
        }

        var week = friday.AddDays(7);

        // waiting for review: dates guessed from the day the email was sent
        Res("12B-C3-1-2", "Omar Khaled Mostafa / Laila Hassan Ibrahim", 2, VisitorType.Hosting, ReservationStatus.PendingReview,
            EgyptClock.NearestFriday(today), EgyptClock.NearestFriday(today).AddDays(7), true, "تصاريح استضافة", now.AddHours(-3));
        Res("7-C1-0-1", "Karim Adel Samir", 1, VisitorType.Rental, ReservationStatus.PendingReview,
            EgyptClock.NearestFriday(today), EgyptClock.NearestFriday(today).AddDays(7), true, "تصاريح تأجير", now.AddHours(-3));
        Res("33A-C9-2-2", "Dina Wael Fathy", 1, null, ReservationStatus.PendingReview,
            EgyptClock.NearestFriday(today), EgyptClock.NearestFriday(today).AddDays(7), true, "تصاريح الأسبوع", now.AddHours(-3));

        // approved, has not arrived yet
        Res("18-C5-1-1", "Tarek Samy Nabil / Hana Tarek Samy / Yara Tarek Samy", 3, VisitorType.Hosting,
            ReservationStatus.Approved, friday, week);

        // inside now (came and went yesterday, back in this morning)
        var inside = Res("25B-C12-0-2", "Mostafa Reda Salem", 1, VisitorType.Sales, ReservationStatus.Approved, friday, week);
        Move(inside, now.AddHours(-30), now.AddHours(-24));
        Move(inside, now.AddHours(-4));

        // stepped out for a while, may come back
        var outside = Res("9-C2-3-1", "Nour Eldin Ashraf Hegazy", 1, VisitorType.Authorized, ReservationStatus.Approved, friday, week);
        Move(outside, now.AddHours(-6), now.AddHours(-2));

        // left for good: cannot come in again without a new email
        var left = Res("41-C7-2-1", "Ahmed Fouad Lotfy / Mariam Ahmed Fouad", 2, VisitorType.Hosting, ReservationStatus.Approved, friday, week);
        Move(left, now.AddHours(-30), now.AddHours(-20), isFinal: true);

        // late: the permit ended last week and he is still inside
        var late = Res("20-C8-2-1", "Hossam Ramy Salah / Salma Hossam Ramy", 2, VisitorType.Rental,
            ReservationStatus.Approved, friday.AddDays(-14), friday.AddDays(-7));
        Move(late, now.AddDays(-8));

        // last week: a finished visit
        var finished = Res("30-C6-1-2", "Walid Gamal Anwar", 1, VisitorType.Hosting,
            ReservationStatus.Approved, friday.AddDays(-7), friday);
        Move(finished, now.AddDays(-5), now.AddDays(-3), isFinal: true);

        // ---------- what the mailbox reader would have logged ----------
        db.InboxMessages.AddRange(
            new InboxMessage
            {
                Id = Guid.NewGuid(), MessageId = $"demo-{Guid.NewGuid():N}@example.com", FromAddress = "permits@hq.example.com",
                Subject = "تصاريح الأسبوع", ReceivedAt = now.AddHours(-3), ProcessedAt = now.AddHours(-3),
                Status = "Imported", Detail = "permits.xlsx: 3 جديد", PermitsCreated = 3
            },
            new InboxMessage
            {
                Id = Guid.NewGuid(), MessageId = $"demo-{Guid.NewGuid():N}@example.com", FromAddress = "someone@other.example",
                Subject = "عرض أسعار", ReceivedAt = now.AddHours(-9), ProcessedAt = now.AddHours(-9),
                Status = "Ignored", Detail = "المرسل غير مسموح به", PermitsCreated = 0
            },
            new InboxMessage
            {
                Id = Guid.NewGuid(), MessageId = $"demo-{Guid.NewGuid():N}@example.com", FromAddress = "permits@hq.example.com",
                Subject = "تذكير", ReceivedAt = now.AddDays(-1), ProcessedAt = now.AddDays(-1),
                Status = "Ignored", Detail = "لا يوجد مرفق Excel (xlsx) في الرسالة", PermitsCreated = 0
            });
    }
}
