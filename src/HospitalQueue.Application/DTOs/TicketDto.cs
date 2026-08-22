using HospitalQueue.Domain.Enums;

namespace HospitalQueue.Application.DTOs;

public record TicketDto(
    Guid Id,
    string Number,
    Guid ServiceId,
    string ServiceName,
    TicketStatus Status,
    DateTime CreatedAt,
    DateTime? CalledAt,
    int RecallCount);

/// <summary>Broadcast over SignalR whenever a ticket is called (or re-called) — drives the public display board.</summary>
public record TicketCalledEvent(
    string Number,
    string ServiceName,
    Guid ServiceId,
    DateTime CalledAt,
    bool IsRecall,
    TicketStatus Status);

/// <summary>
/// Broadcast whenever a previously-called ticket's status moves on (service
/// started/finished, patient skipped, transferred) so the display board can
/// update that ticket's row without waiting for another call/recall.
/// </summary>
public record TicketStatusChangedEvent(
    string Number,
    Guid ServiceId,
    TicketStatus Status);
