namespace HospitalQueue.Domain.Entities;

/// <summary>
/// A physical service window / desk. Each counter can serve one or more
/// services; the employee working it picks the active service at session start.
/// </summary>
public class Counter
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<CounterService> CounterServices { get; set; } = new List<CounterService>();
    public ICollection<CounterSession> Sessions { get; set; } = new List<CounterSession>();
}
