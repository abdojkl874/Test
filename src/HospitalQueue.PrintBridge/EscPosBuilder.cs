using System.Drawing;
using System.Runtime.Versioning;

namespace HospitalQueue.PrintBridge;

[SupportedOSPlatform("windows")]
public static class EscPosBuilder
{
    public static byte[] BuildTicketReceipt(Bitmap bitmap)
    {
        using var stream = new MemoryStream();

        stream.Write(new byte[] { 0x1B, 0x40 }); // ESC @ : initialize printer

        WriteRasterImage(stream, bitmap);

        stream.Write(new byte[] { 0x1B, 0x64, 0x03 }); // feed 3 lines
        stream.Write(new byte[] { 0x1D, 0x56, 0x01 }); // partial cut

        return stream.ToArray();
    }

    private static void WriteRasterImage(Stream stream, Bitmap bitmap)
    {
        var width = bitmap.Width;
        var height = bitmap.Height;
        var bytesPerRow = (width + 7) / 8;
        var data = new byte[bytesPerRow * height];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var pixel = bitmap.GetPixel(x, y);
                var isDark = (pixel.R + pixel.G + pixel.B) / 3 < 200;
                if (isDark)
                {
                    data[(y * bytesPerRow) + (x / 8)] |= (byte)(0x80 >> (x % 8));
                }
            }
        }

        // GS v 0: print raster bit image (m=0 normal density)
        stream.Write(new byte[] { 0x1D, 0x76, 0x30, 0x00 });
        stream.WriteByte((byte)(bytesPerRow & 0xFF));
        stream.WriteByte((byte)((bytesPerRow >> 8) & 0xFF));
        stream.WriteByte((byte)(height & 0xFF));
        stream.WriteByte((byte)((height >> 8) & 0xFF));
        stream.Write(data);
    }
}
