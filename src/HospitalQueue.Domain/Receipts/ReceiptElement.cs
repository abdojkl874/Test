using System.Text.Json.Serialization;

namespace HospitalQueue.Domain.Receipts;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReceiptAlign
{
    Start,
    Center,
    End,
}

/// <summary>
/// One block in a receipt template's layout. A <see cref="Entities.ReceiptTemplate"/>
/// stores an ordered list of these, serialized to JSON — there is no separate
/// table for them since the whole list is only ever read/written as a unit
/// from the designer screen.
/// </summary>
public class ReceiptElement
{
    public ReceiptElementType Type { get; set; }
    public string? Text { get; set; }
    public ReceiptAlign Align { get; set; } = ReceiptAlign.Center;
    public int FontSize { get; set; } = 12;
    public bool Bold { get; set; }
}
