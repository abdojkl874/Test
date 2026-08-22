using HospitalQueue.Domain.Enums;

namespace HospitalQueue.Domain.Entities;

public class Ticket
{
    public Guid Id { get; set; }

    /// <summary>Human-readable ticket number, e.g. "A-014".</summary>
    public string Number { get; set; } = string.Empty;

    /// <summary>Raw sequence number within the service/day, e.g. 14.</summary>
    public int SequenceNumber { get; set; }

    public Guid ServiceId { get; set; }
    public Service Service { get; set; } = null!;

    public Guid? EmployeeId { get; set; }
    public ApplicationUser? Employee { get; set; }

    public TicketStatus Status { get; set; } = TicketStatus.Waiting;

    /// <summary>Queue day (local date) — numbering resets daily per service.</summary>
    public DateOnly QueueDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CalledAt { get; set; }
    public DateTime? ServiceStartedAt { get; set; }
    public DateTime? ServiceEndedAt { get; set; }

    public int RecallCount { get; set; }
    public string? Notes { get; set; }

    /// <summary>Set when this ticket was created by transferring a patient from another ticket.</summary>
    public Guid? TransferredFromTicketId { get; set; }
    public Ticket? TransferredFromTicket { get; set; }

    public ICollection<TicketStatusHistory> History { get; set; } = new List<TicketStatusHistory>();
}
