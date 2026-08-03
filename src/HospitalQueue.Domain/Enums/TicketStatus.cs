namespace HospitalQueue.Domain.Enums;

public enum TicketStatus
{
    Waiting = 0,
    Called = 1,
    InService = 2,
    Done = 3,
    Skipped = 4,
    Transferred = 5,
    Cancelled = 6
}
