namespace HospitalQueue.Domain.Entities;

/// <summary>
/// A clinic / service that patients can queue for (e.g. "الباطنية", "الأطفال").
/// </summary>
public class Service
{
    public Guid Id { get; set; }
    public string NameAr { get; set; } = string.Empty;

    /// <summary>Short prefix used to build ticket numbers, e.g. "A" -> "A-014".</summary>
    public string Code { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }

    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
