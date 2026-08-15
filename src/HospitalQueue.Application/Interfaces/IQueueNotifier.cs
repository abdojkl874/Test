using HospitalQueue.Application.DTOs;

namespace HospitalQueue.Application.Interfaces;

/// <summary>
/// Implemented in the Web layer with SignalR (IHubContext&lt;QueueHub&gt;). Kept
/// as an interface here so the Application layer stays free of presentation concerns.
/// </summary>
public interface IQueueNotifier
{
    Task TicketCalledAsync(TicketCalledEvent evt, CancellationToken ct = default);

    /// <summary>Fired whenever a called ticket's status moves on (service started/finished, skipped, transferred) without a fresh call/recall.</summary>
    Task TicketStatusChangedAsync(TicketStatusChangedEvent evt, CancellationToken ct = default);

    /// <summary>Fired whenever waiting counts for a service change (new ticket, skip, transfer, complete) so kiosk/admin screens can refresh live counts.</summary>
    Task QueueChangedAsync(Guid serviceId, CancellationToken ct = default);
}
