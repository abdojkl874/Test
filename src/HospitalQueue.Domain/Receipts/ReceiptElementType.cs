using System.Text.Json.Serialization;

namespace HospitalQueue.Domain.Receipts;

/// <summary>The kinds of blocks a receipt template can be built from.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReceiptElementType
{
    /// <summary>Organization name, from admin settings.</summary>
    OrgName,

    /// <summary>The clinic/service name the ticket was issued for.</summary>
    ServiceName,

    /// <summary>The ticket number itself, e.g. "A-014".</summary>
    TicketNumber,

    /// <summary>Issue date and time.</summary>
    DateTimeStamp,

    /// <summary>Fixed text the admin types in (e.g. "يرجى الانتظار...").</summary>
    StaticText,

    /// <summary>A horizontal divider line.</summary>
    Divider,

    /// <summary>Empty vertical space.</summary>
    Spacer,
}
