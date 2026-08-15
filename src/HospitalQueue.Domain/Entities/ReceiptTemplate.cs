using HospitalQueue.Domain.Receipts;

namespace HospitalQueue.Domain.Entities;

/// <summary>
/// A printable ticket layout designed from the admin "مصمّم الوصل" screen.
/// Exactly one template is active at a time — the kiosk always prints using
/// whichever one has <see cref="IsActive"/> set.
/// </summary>
public class ReceiptTemplate
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    /// <summary>Serialized <see cref="List{ReceiptElement}"/> — the ordered layout blocks.</summary>
    public string ElementsJson { get; set; } = "[]";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
