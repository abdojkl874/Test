using HospitalQueue.Application.DTOs;
using HospitalQueue.Domain.Entities;

namespace HospitalQueue.Application.Interfaces;

public interface IQueueService
{
    Task<TicketDto> CreateTicketAsync(Guid serviceId, CancellationToken ct = default);

    Task<ServiceSession> StartSessionAsync(Guid employeeId, Guid serviceId, CancellationToken ct = default);
    Task EndSessionAsync(Guid sessionId, CancellationToken ct = default);
    Task<ServiceSession?> GetActiveSessionAsync(Guid employeeId, CancellationToken ct = default);

    Task<TicketDto?> CallNextAsync(Guid sessionId, CancellationToken ct = default);
    Task<TicketDto> RecallAsync(Guid ticketId, CancellationToken ct = default);
    Task<TicketDto> StartServiceAsync(Guid ticketId, CancellationToken ct = default);
    Task<TicketDto> CompleteAsync(Guid ticketId, string? notes, CancellationToken ct = default);
    Task<TicketDto> SkipAsync(Guid ticketId, CancellationToken ct = default);
    Task<TicketDto> TransferAsync(Guid ticketId, Guid newServiceId, CancellationToken ct = default);

    Task<int> GetWaitingCountAsync(Guid serviceId, CancellationToken ct = default);
    Task<IReadOnlyList<TicketCalledEvent>> GetRecentlyCalledAsync(int take = 12, CancellationToken ct = default);
}
