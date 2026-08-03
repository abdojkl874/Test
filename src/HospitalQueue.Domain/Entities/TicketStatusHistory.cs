using HospitalQueue.Domain.Enums;

namespace HospitalQueue.Domain.Entities;

/// <summary>Audit trail of every status change a ticket goes through — backs the reporting screens.</summary>
public class TicketStatusHistory
{
    public Guid Id { get; set; }

    public Guid TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;

    public TicketStatus Status { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    public Guid? EmployeeId { get; set; }
    public Guid? CounterId { get; set; }
    public string? Note { get; set; }
}
