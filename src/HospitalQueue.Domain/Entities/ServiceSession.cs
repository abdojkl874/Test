namespace HospitalQueue.Domain.Entities;

/// <summary>
/// Tracks which employee is actively serving which clinic, over a time span —
/// used both to route "call next" and to build per-employee reports.
/// </summary>
public class ServiceSession
{
    public Guid Id { get; set; }

    public Guid EmployeeId { get; set; }
    public ApplicationUser Employee { get; set; } = null!;

    public Guid ServiceId { get; set; }
    public Service Service { get; set; } = null!;

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }

    public bool IsActive => EndedAt == null;
}
