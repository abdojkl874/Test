using HospitalQueue.Domain.Enums;

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

public record DashboardSummaryDto(
    IReadOnlyList<ServiceStatsDto> ServiceStats,
    IReadOnlyList<EmployeeStatsDto> EmployeeStats,
    int WaitingNow,
    int InServiceNow);

/// <summary>One row of the admin "سجل التذاكر" ticket log table.</summary>
public record TicketLogRowDto(
    string Number,
    string ServiceName,
    DateOnly Date,
    TicketStatus Status,
    string? ServedByName,
    double? WaitMinutes,
    double? ServiceMinutes);
