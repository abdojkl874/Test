using HospitalQueue.Application.DTOs;

namespace HospitalQueue.Application.Interfaces;

public interface IReportService
{
    Task<DashboardSummaryDto> GetDashboardAsync(DateOnly from, DateOnly to, CancellationToken ct = default);
}
