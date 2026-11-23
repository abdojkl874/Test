using Microsoft.AspNetCore.Identity;

namespace HospitalQueue.Domain.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>UTC time of the last successful sign-in through the login page; null if never.</summary>
    public DateTime? LastLoginAt { get; set; }
}
