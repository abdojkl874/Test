using HospitalQueue.Application.DTOs;
using HospitalQueue.Application.Interfaces;

namespace HospitalQueue.Web.Services;

/// <summary>
/// In-process pub/sub shared by every Blazor Server circuit (registered as a
/// singleton). Blazor Server already keeps a persistent connection to each
/// browser, so components get live updates simply by subscribing to these
/// events and marshalling back with InvokeAsync(StateHasChanged) — no separate
/// SignalR hub/WebSocket is needed on top of it.
///
/// Publishing fires every subscriber "and forgets": a slow or failing display
/// screen on one circuit must never delay or break the employee/kiosk action
/// (ticket call, complete, etc.) that raised the event on another circuit.
/// </summary>
public class QueueEventBus : IQueueNotifier
{
    public event Func<TicketCalledEvent, Task>? TicketCalled;
    public event Func<TicketStatusChangedEvent, Task>? TicketStatusChanged;
    public event Func<Guid, Task>? QueueChanged;

    public Task TicketCalledAsync(TicketCalledEvent evt, CancellationToken ct = default)
    {
        Publish(TicketCalled, evt);
        return Task.CompletedTask;
    }

    public Task TicketStatusChangedAsync(TicketStatusChangedEvent evt, CancellationToken ct = default)
    {
        Publish(TicketStatusChanged, evt);
        return Task.CompletedTask;
    }

    public Task QueueChangedAsync(Guid serviceId, CancellationToken ct = default)
    {
        Publish(QueueChanged, serviceId);
        return Task.CompletedTask;
    }

    private static void Publish<T>(Func<T, Task>? handlers, T arg)
    {
        if (handlers is null)
        {
            return;
        }

        foreach (var handler in handlers.GetInvocationList().Cast<Func<T, Task>>())
        {
            _ = InvokeSafelyAsync(handler, arg);
        }
    }

    private static async Task InvokeSafelyAsync<T>(Func<T, Task> handler, T arg)
    {
        try
        {
            await handler(arg);
        }
        catch
        {
            // A subscriber (one browser's screen) failing to handle a live update
            // must never bubble up to whoever published the event.
        }
    }
}
