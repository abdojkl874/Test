using HospitalQueue.Domain.Enums;

namespace HospitalQueue.Application.DTOs;

public record TicketDto(
    Guid Id,
    string Number,
    Guid ServiceId,
    string ServiceName,
    Guid? CounterId,
    string? CounterName,
    TicketStatus Status,
    DateTime CreatedAt,
    DateTime? CalledAt,
    int RecallCount);

/// <summary>Broadcast over SignalR whenever a ticket is called (or re-called) — drives the public display board.</summary>
public record TicketCalledEvent(
    string Number,
    string ServiceName,
    string CounterName,
    Guid ServiceId,
    Guid CounterId,
    DateTime CalledAt,
    bool IsRecall);
