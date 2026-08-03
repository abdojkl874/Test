namespace HospitalQueue.Domain.Entities;

/// <summary>Join entity: which services a given counter is allowed to serve.</summary>
public class CounterService
{
    public Guid CounterId { get; set; }
    public Counter Counter { get; set; } = null!;

    public Guid ServiceId { get; set; }
    public Service Service { get; set; } = null!;
}
