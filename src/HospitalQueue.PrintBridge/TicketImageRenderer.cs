using System.Drawing;
using System.Drawing.Text;
using System.Runtime.Versioning;

namespace HospitalQueue.PrintBridge;

/// <summary>
/// Renders the ticket as a bitmap instead of sending raw Arabic text to the
/// printer's own font. Most ESC/POS thermal printers cannot shape Arabic
/// (joined letter forms) or handle right-to-left order themselves — GDI+ on
/// Windows does that shaping correctly for us, and we print the result as a
/// dot-for-dot image, so it looks right regardless of what the printer's
/// firmware supports.
/// </summary>
[SupportedOSPlatform("windows")]
public static class TicketImageRenderer
{
    public static Bitmap Render(string ticketNumber, string serviceName, string issuedAt, int widthDots)
    {
        var bitmap = new Bitmap(widthDots, 340);
        using var g = Graphics.FromImage(bitmap);
        g.Clear(Color.White);
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

        using var titleFont = new Font("Tahoma", 13, FontStyle.Bold);
        using var numberFont = new Font("Tahoma", 46, FontStyle.Bold);
        using var infoFont = new Font("Tahoma", 12, FontStyle.Regular);

        var centered = new StringFormat
        {
            Alignment = StringAlignment.Center,
            FormatFlags = StringFormatFlags.DirectionRightToLeft,
        };

        float y = 8;
        g.DrawString("نظام إدارة طابور المشفى", titleFont, Brushes.Black, new RectangleF(0, y, widthDots, 28), centered);
        y += 36;
        g.DrawLine(Pens.Black, 10, y, widthDots - 10, y);
        y += 12;
        g.DrawString(serviceName, infoFont, Brushes.Black, new RectangleF(0, y, widthDots, 24), centered);
        y += 32;
        g.DrawString(ticketNumber, numberFont, Brushes.Black, new RectangleF(0, y, widthDots, 64), centered);
        y += 74;
        g.DrawString(issuedAt, infoFont, Brushes.Black, new RectangleF(0, y, widthDots, 24), centered);
        y += 30;
        g.DrawString("الرجاء الانتظار حتى يتم مناداة رقمك", infoFont, Brushes.Black, new RectangleF(0, y, widthDots, 24), centered);

        return bitmap;
    }
}
