using HospitalQueue.Application.DTOs;
using HospitalQueue.Application.Interfaces;
using HospitalQueue.Domain.Entities;
using HospitalQueue.Domain.Enums;
using HospitalQueue.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HospitalQueue.Application.Services;

public class QueueService : IQueueService
{
    private readonly ApplicationDbContext _db;
    private readonly IQueueNotifier _notifier;

    public QueueService(ApplicationDbContext db, IQueueNotifier notifier)
    {
        _db = db;
        _notifier = notifier;
    }

    public async Task<TicketDto> CreateTicketAsync(Guid serviceId, CancellationToken ct = default)
    {
        var service = await _db.Services.FirstOrDefaultAsync(s => s.Id == serviceId, ct)
            ?? throw new QueueOperationException("الخدمة المطلوبة غير موجودة");
        if (!service.IsActive)
        {
            throw new QueueOperationException("هذه الخدمة غير متاحة حالياً");
        }

        var ticket = await CreateTicketCoreAsync(service, transferredFromTicketId: null, ct);
        await _notifier.QueueChangedAsync(serviceId, ct);

        return ToDto(ticket, service.NameAr, counterName: null);
    }

    public async Task<CounterSession> StartSessionAsync(Guid employeeId, Guid counterId, Guid serviceId, CancellationToken ct = default)
    {
        var allowed = await _db.CounterServices.AnyAsync(cs => cs.CounterId == counterId && cs.ServiceId == serviceId, ct);
        if (!allowed)
        {
            throw new QueueOperationException("هذا الشباك غير مخصص لهذه الخدمة");
        }

        var openSessions = await _db.CounterSessions
            .Where(s => s.EmployeeId == employeeId && s.EndedAt == null)
            .ToListAsync(ct);
        foreach (var open in openSessions)
        {
            open.EndedAt = DateTime.UtcNow;
        }

        var session = new CounterSession
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            CounterId = counterId,
            ServiceId = serviceId,
            StartedAt = DateTime.UtcNow,
        };
        _db.CounterSessions.Add(session);
        await _db.SaveChangesAsync(ct);
        return session;
    }

    public async Task EndSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        var session = await _db.CounterSessions.FirstOrDefaultAsync(s => s.Id == sessionId, ct);
        if (session is not null && session.EndedAt is null)
        {
            session.EndedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<CounterSession?> GetActiveSessionAsync(Guid employeeId, CancellationToken ct = default)
    {
        return await _db.CounterSessions
            .Include(s => s.Counter)
            .Include(s => s.Service)
            .Where(s => s.EmployeeId == employeeId && s.EndedAt == null)
            .OrderByDescending(s => s.StartedAt)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<TicketDto?> CallNextAsync(Guid counterSessionId, CancellationToken ct = default)
    {
        var session = await _db.CounterSessions
            .Include(s => s.Counter)
            .Include(s => s.Service)
            .FirstOrDefaultAsync(s => s.Id == counterSessionId && s.EndedAt == null, ct)
            ?? throw new QueueOperationException("لا توجد جلسة عمل نشطة على هذا الشباك");

        var today = DateOnly.FromDateTime(DateTime.Now);
        await ExpireStaleWaitingTicketsAsync(session.ServiceId, today, ct);

        // Scoped to today deliberately: ticket numbering restarts every day
        // (see NextSequenceNumberAsync), so serving a leftover from an earlier
        // day would announce a number that belongs to today's sequence too.
        var next = await _db.Tickets
            .Where(t => t.ServiceId == session.ServiceId && t.Status == TicketStatus.Waiting && t.QueueDate == today)
            .OrderBy(t => t.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (next is null)
        {
            return null;
        }

        next.Status = TicketStatus.Called;
        next.CounterId = session.CounterId;
        next.EmployeeId = session.EmployeeId;
        next.CalledAt = DateTime.UtcNow;

        _db.TicketStatusHistories.Add(new TicketStatusHistory
        {
            Id = Guid.NewGuid(),
            TicketId = next.Id,
            Status = TicketStatus.Called,
            OccurredAt = next.CalledAt.Value,
            EmployeeId = session.EmployeeId,
            CounterId = session.CounterId,
        });

        await _db.SaveChangesAsync(ct);

        var evt = new TicketCalledEvent(next.Number, session.Service.NameAr, session.Counter.Name,
            session.ServiceId, session.CounterId, next.CalledAt.Value, IsRecall: false, Status: next.Status);
        await _notifier.TicketCalledAsync(evt, ct);
        await _notifier.QueueChangedAsync(session.ServiceId, ct);

        return ToDto(next, session.Service.NameAr, session.Counter.Name);
    }

    public async Task<TicketDto> RecallAsync(Guid ticketId, CancellationToken ct = default)
    {
        var ticket = await LoadTicketAsync(ticketId, ct);
        if (ticket.Status is not (TicketStatus.Called or TicketStatus.InService) || ticket.CounterId is null)
        {
            throw new QueueOperationException("لا يمكن إعادة نداء تذكرة لم يتم استدعاؤها بعد");
        }

        ticket.RecallCount++;
        ticket.CalledAt = DateTime.UtcNow;

        _db.TicketStatusHistories.Add(new TicketStatusHistory
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            Status = ticket.Status,
            OccurredAt = ticket.CalledAt.Value,
            EmployeeId = ticket.EmployeeId,
            CounterId = ticket.CounterId,
            Note = "إعادة نداء",
        });

        await _db.SaveChangesAsync(ct);

        var evt = new TicketCalledEvent(ticket.Number, ticket.Service.NameAr, ticket.Counter!.Name,
            ticket.ServiceId, ticket.CounterId.Value, ticket.CalledAt.Value, IsRecall: true, Status: ticket.Status);
        await _notifier.TicketCalledAsync(evt, ct);

        return ToDto(ticket, ticket.Service.NameAr, ticket.Counter.Name);
    }

    public async Task<TicketDto> StartServiceAsync(Guid ticketId, CancellationToken ct = default)
    {
        var ticket = await LoadTicketAsync(ticketId, ct);
        if (ticket.Status != TicketStatus.Called)
        {
            throw new QueueOperationException("لا يمكن بدء الخدمة إلا لتذكرة تم استدعاؤها");
        }

        ticket.Status = TicketStatus.InService;
        ticket.ServiceStartedAt = DateTime.UtcNow;

        _db.TicketStatusHistories.Add(new TicketStatusHistory
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            Status = TicketStatus.InService,
            OccurredAt = ticket.ServiceStartedAt.Value,
            EmployeeId = ticket.EmployeeId,
            CounterId = ticket.CounterId,
        });

        await _db.SaveChangesAsync(ct);
        await _notifier.TicketStatusChangedAsync(new TicketStatusChangedEvent(ticket.Number, ticket.ServiceId, ticket.Status), ct);
        return ToDto(ticket, ticket.Service.NameAr, ticket.Counter?.Name);
    }

    public async Task<TicketDto> CompleteAsync(Guid ticketId, string? notes, CancellationToken ct = default)
    {
        var ticket = await LoadTicketAsync(ticketId, ct);
        if (ticket.Status is not (TicketStatus.Called or TicketStatus.InService))
        {
            throw new QueueOperationException("لا يمكن إنهاء تذكرة لم تكن قيد الخدمة");
        }

        ticket.ServiceStartedAt ??= ticket.CalledAt ?? DateTime.UtcNow;
        ticket.Status = TicketStatus.Done;
        ticket.ServiceEndedAt = DateTime.UtcNow;
        ticket.Notes = notes;

        _db.TicketStatusHistories.Add(new TicketStatusHistory
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            Status = TicketStatus.Done,
            OccurredAt = ticket.ServiceEndedAt.Value,
            EmployeeId = ticket.EmployeeId,
            CounterId = ticket.CounterId,
            Note = notes,
        });

        await _db.SaveChangesAsync(ct);
        await _notifier.TicketStatusChangedAsync(new TicketStatusChangedEvent(ticket.Number, ticket.ServiceId, ticket.Status), ct);
        await _notifier.QueueChangedAsync(ticket.ServiceId, ct);

        return ToDto(ticket, ticket.Service.NameAr, ticket.Counter?.Name);
    }

    public async Task<TicketDto> SkipAsync(Guid ticketId, CancellationToken ct = default)
    {
        var ticket = await LoadTicketAsync(ticketId, ct);
        if (ticket.Status != TicketStatus.Called)
        {
            throw new QueueOperationException("لا يمكن تخطي تذكرة لم يتم استدعاؤها");
        }

        ticket.Status = TicketStatus.Skipped;
        ticket.ServiceEndedAt = DateTime.UtcNow;

        _db.TicketStatusHistories.Add(new TicketStatusHistory
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            Status = TicketStatus.Skipped,
            OccurredAt = ticket.ServiceEndedAt.Value,
            EmployeeId = ticket.EmployeeId,
            CounterId = ticket.CounterId,
            Note = "لم يحضر المريض",
        });

        await _db.SaveChangesAsync(ct);
        await _notifier.TicketStatusChangedAsync(new TicketStatusChangedEvent(ticket.Number, ticket.ServiceId, ticket.Status), ct);
        await _notifier.QueueChangedAsync(ticket.ServiceId, ct);

        return ToDto(ticket, ticket.Service.NameAr, ticket.Counter?.Name);
    }

    public async Task<TicketDto> TransferAsync(Guid ticketId, Guid newServiceId, CancellationToken ct = default)
    {
        var ticket = await LoadTicketAsync(ticketId, ct);
        if (ticket.Status is not (TicketStatus.Called or TicketStatus.InService))
        {
            throw new QueueOperationException("لا يمكن تحويل تذكرة لم يتم استدعاؤها بعد");
        }

        if (newServiceId == ticket.ServiceId)
        {
            throw new QueueOperationException("لا يمكن التحويل لنفس الخدمة");
        }

        var newService = await _db.Services.FirstOrDefaultAsync(s => s.Id == newServiceId, ct)
            ?? throw new QueueOperationException("الخدمة المطلوب التحويل إليها غير موجودة");
        if (!newService.IsActive)
        {
            throw new QueueOperationException("الخدمة المطلوب التحويل إليها غير متاحة حالياً");
        }

        ticket.Status = TicketStatus.Transferred;
        ticket.ServiceEndedAt = DateTime.UtcNow;

        _db.TicketStatusHistories.Add(new TicketStatusHistory
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            Status = TicketStatus.Transferred,
            OccurredAt = ticket.ServiceEndedAt.Value,
            EmployeeId = ticket.EmployeeId,
            CounterId = ticket.CounterId,
            Note = $"تحويل إلى {newService.NameAr}",
        });

        var newTicket = await CreateTicketCoreAsync(newService, transferredFromTicketId: ticket.Id, ct);

        await _db.SaveChangesAsync(ct);
        await _notifier.TicketStatusChangedAsync(new TicketStatusChangedEvent(ticket.Number, ticket.ServiceId, ticket.Status), ct);
        await _notifier.QueueChangedAsync(ticket.ServiceId, ct);
        await _notifier.QueueChangedAsync(newServiceId, ct);

        return ToDto(newTicket, newService.NameAr, counterName: null);
    }

    public async Task<int> GetWaitingCountAsync(Guid serviceId, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        return await _db.Tickets.CountAsync(
            t => t.ServiceId == serviceId && t.Status == TicketStatus.Waiting && t.QueueDate == today, ct);
    }

    public async Task<IReadOnlyList<TicketCalledEvent>> GetRecentlyCalledAsync(int take = 12, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        return await _db.Tickets
            .Where(t => t.QueueDate == today && t.CalledAt != null && t.CounterId != null)
            .OrderByDescending(t => t.CalledAt)
            .Take(take)
            .Select(t => new TicketCalledEvent(
                t.Number, t.Service.NameAr, t.Counter!.Name, t.ServiceId, t.CounterId!.Value,
                t.CalledAt!.Value, t.RecallCount > 0, t.Status))
            .ToListAsync(ct);
    }

    /// <summary>
    /// Closes off tickets left waiting from an earlier day. Without this they
    /// stay Waiting forever: excluded from today's queue but still counted as
    /// "waiting now" on the dashboard.
    /// </summary>
    private async Task ExpireStaleWaitingTicketsAsync(Guid serviceId, DateOnly today, CancellationToken ct)
    {
        var stale = await _db.Tickets
            .Where(t => t.ServiceId == serviceId && t.Status == TicketStatus.Waiting && t.QueueDate < today)
            .ToListAsync(ct);

        if (stale.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var ticket in stale)
        {
            ticket.Status = TicketStatus.Cancelled;
            ticket.ServiceEndedAt = now;

            _db.TicketStatusHistories.Add(new TicketStatusHistory
            {
                Id = Guid.NewGuid(),
                TicketId = ticket.Id,
                Status = TicketStatus.Cancelled,
                OccurredAt = now,
                Note = "إلغاء تلقائي — تذكرة من يوم سابق",
            });
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task<Ticket> CreateTicketCoreAsync(Service service, Guid? transferredFromTicketId, CancellationToken ct)
    {
        var queueDate = DateOnly.FromDateTime(DateTime.Now);
        var nextNumber = await NextSequenceNumberAsync(service.Id, queueDate, ct);

        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            ServiceId = service.Id,
            SequenceNumber = nextNumber,
            Number = $"{service.Code}-{nextNumber:D3}",
            QueueDate = queueDate,
            Status = TicketStatus.Waiting,
            CreatedAt = DateTime.UtcNow,
            TransferredFromTicketId = transferredFromTicketId,
        };
        _db.Tickets.Add(ticket);

        _db.TicketStatusHistories.Add(new TicketStatusHistory
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            Status = TicketStatus.Waiting,
            OccurredAt = ticket.CreatedAt,
            Note = transferredFromTicketId is null ? null : "دخول عبر التحويل",
        });

        await _db.SaveChangesAsync(ct);
        return ticket;
    }

    private async Task<int> NextSequenceNumberAsync(Guid serviceId, DateOnly queueDate, CancellationToken ct)
    {
        // SingleAsync() would make EF Core try to compose extra SQL around this
        // (e.g. to enforce "exactly one row"), which fails because an
        // INSERT ... RETURNING statement isn't a composable SELECT. ToListAsync()
        // reads the raw result set as-is, so it works with non-composable SQL.
        var results = await _db.Database.SqlQueryRaw<int>(
            """
            INSERT INTO "DailySequences" ("ServiceId", "QueueDate", "LastNumber")
            VALUES ({0}, {1}, 1)
            ON CONFLICT ("ServiceId", "QueueDate")
            DO UPDATE SET "LastNumber" = "DailySequences"."LastNumber" + 1
            RETURNING "LastNumber"
            """, serviceId, queueDate).ToListAsync(ct);
        return results.Single();
    }

    private async Task<Ticket> LoadTicketAsync(Guid ticketId, CancellationToken ct)
    {
        return await _db.Tickets
            .Include(t => t.Service)
            .Include(t => t.Counter)
            .FirstOrDefaultAsync(t => t.Id == ticketId, ct)
            ?? throw new QueueOperationException("التذكرة غير موجودة");
    }

    private static TicketDto ToDto(Ticket t, string serviceName, string? counterName) => new(
        t.Id, t.Number, t.ServiceId, serviceName, t.CounterId, counterName, t.Status, t.CreatedAt, t.CalledAt, t.RecallCount);
}
