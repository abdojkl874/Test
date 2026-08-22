namespace HospitalQueue.Domain.Entities;

/// <summary>
/// Join entity: which clinics an employee is allowed to work. An employee with
/// no rows here is unrestricted and may pick any active clinic — assignments
/// are a narrowing, so upgrading an existing install doesn't lock anyone out.
/// </summary>
public class EmployeeService
{
    public Guid EmployeeId { get; set; }
    public ApplicationUser Employee { get; set; } = null!;

    public Guid ServiceId { get; set; }
    public Service Service { get; set; } = null!;
}
