namespace HospitalQueue.Application.DTOs;

public record ServiceStatsDto(
    Guid ServiceId,
    string ServiceName,
    int TicketCount,
    int CompletedCount,
    int SkippedCount,
    double AverageWaitMinutes,
    double AverageServiceMinutes);

public record EmployeeStatsDto(
    Guid EmployeeId,
    string EmployeeName,
    int TicketsHandled,
    double AverageServiceMinutes);

public record CounterStatsDto(
    Guid CounterId,
    string CounterName,
    int TicketsHandled,
    double AverageServiceMinutes);

public record DashboardSummaryDto(
    IReadOnlyList<ServiceStatsDto> ServiceStats,
    IReadOnlyList<EmployeeStatsDto> EmployeeStats,
    IReadOnlyList<CounterStatsDto> CounterStats,
    int WaitingNow,
    int InServiceNow);
