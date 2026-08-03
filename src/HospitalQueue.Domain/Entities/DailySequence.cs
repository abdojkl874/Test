namespace HospitalQueue.Domain.Entities;

/// <summary>
/// Holds the last ticket number issued per service per day. Composite key
/// (ServiceId, QueueDate); incremented atomically when a kiosk issues a ticket.
/// </summary>
public class DailySequence
{
    public Guid ServiceId { get; set; }
    public Service Service { get; set; } = null!;

    public DateOnly QueueDate { get; set; }
    public int LastNumber { get; set; }
}
