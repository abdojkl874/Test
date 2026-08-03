namespace HospitalQueue.Domain.Entities;

/// <summary>
/// Tracks which employee is actively working which counter, for which
/// service, over a time span — used both to route "call next" and to
/// build per-employee / per-counter reports.
/// </summary>
public class CounterSession
{
    public Guid Id { get; set; }

    public Guid EmployeeId { get; set; }
    public ApplicationUser Employee { get; set; } = null!;

    public Guid CounterId { get; set; }
    public Counter Counter { get; set; } = null!;

    public Guid ServiceId { get; set; }
    public Service Service { get; set; } = null!;

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }

    public bool IsActive => EndedAt == null;
}
