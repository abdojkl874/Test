using HospitalQueue.Application.DTOs;
using HospitalQueue.Application.Interfaces;
using HospitalQueue.Domain.Enums;
using HospitalQueue.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HospitalQueue.Application.Services;

/// <summary>
/// Aggregates dashboard stats in-memory after a single filtered query. Fine for the
/// per-day/per-week ranges the admin dashboard uses; if reporting grows to large
/// multi-month ranges, move the grouping into SQL instead.
/// </summary>
public class ReportService : IReportService
{
    private readonly ApplicationDbContext _db;

    public ReportService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardSummaryDto> GetDashboardAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var rows = await _db.Tickets
            .Where(t => t.QueueDate >= from && t.QueueDate <= to)
            .Select(t => new
            {
                t.ServiceId,
                ServiceName = t.Service.NameAr,
                t.EmployeeId,
                EmployeeName = t.Employee != null ? t.Employee.FullName : null,
                t.CounterId,
                CounterName = t.Counter != null ? t.Counter.Name : null,
                t.Status,
                t.CreatedAt,
                t.CalledAt,
                t.ServiceStartedAt,
                t.ServiceEndedAt,
            })
            .ToListAsync(ct);

        var serviceStats = rows
            .GroupBy(r => new { r.ServiceId, r.ServiceName })
            .Select(g => new ServiceStatsDto(
                g.Key.ServiceId,
                g.Key.ServiceName,
                g.Count(),
                g.Count(x => x.Status == TicketStatus.Done),
                g.Count(x => x.Status == TicketStatus.Skipped),
                AverageMinutes(g.Where(x => x.CalledAt != null).Select(x => x.CalledAt!.Value - x.CreatedAt)),
                AverageMinutes(g.Where(x => x.ServiceEndedAt != null)
                    .Select(x => x.ServiceEndedAt!.Value - (x.ServiceStartedAt ?? x.CalledAt ?? x.CreatedAt)))))
            .OrderByDescending(s => s.TicketCount)
            .ToList();

        var employeeStats = rows
            .Where(r => r.EmployeeId != null && r.ServiceEndedAt != null)
            .GroupBy(r => new { r.EmployeeId, r.EmployeeName })
            .Select(g => new EmployeeStatsDto(
                g.Key.EmployeeId!.Value,
                g.Key.EmployeeName ?? "-",
                g.Count(),
                AverageMinutes(g.Select(x => x.ServiceEndedAt!.Value - (x.ServiceStartedAt ?? x.CalledAt ?? x.CreatedAt)))))
            .OrderByDescending(e => e.TicketsHandled)
            .ToList();

        var counterStats = rows
            .Where(r => r.CounterId != null && r.ServiceEndedAt != null)
            .GroupBy(r => new { r.CounterId, r.CounterName })
            .Select(g => new CounterStatsDto(
                g.Key.CounterId!.Value,
                g.Key.CounterName ?? "-",
                g.Count(),
                AverageMinutes(g.Select(x => x.ServiceEndedAt!.Value - (x.ServiceStartedAt ?? x.CalledAt ?? x.CreatedAt)))))
            .OrderByDescending(c => c.TicketsHandled)
            .ToList();

        var waitingNow = await _db.Tickets.CountAsync(t => t.Status == TicketStatus.Waiting, ct);
        var inServiceNow = await _db.Tickets.CountAsync(t => t.Status == TicketStatus.InService, ct);

        return new DashboardSummaryDto(serviceStats, employeeStats, counterStats, waitingNow, inServiceNow);
    }

    private static double AverageMinutes(IEnumerable<TimeSpan> spans)
    {
        var list = spans.ToList();
        return list.Count == 0 ? 0 : Math.Round(list.Average(s => s.TotalMinutes), 1);
    }
}
