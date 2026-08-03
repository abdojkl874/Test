using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace HospitalQueue.PrintBridge;

/// <summary>
/// Sends a raw byte stream straight to a Windows-installed printer via the
/// spooler's RAW data type, bypassing GDI — the standard way to talk ESC/POS
/// to a thermal receipt printer from .NET. The printer must already be
/// installed in Windows (any driver that accepts raw/text data works; the
/// vendor's own driver or the generic "Generic / Text Only" driver both do).
/// Windows-only: relies on winspool.drv.
/// </summary>
[SupportedOSPlatform("windows")]
public static class RawPrinterHelper
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DOCINFOA
    {
        [MarshalAs(UnmanagedType.LPStr)] public string pDocName;
        [MarshalAs(UnmanagedType.LPStr)] public string? pOutputFile;
        [MarshalAs(UnmanagedType.LPStr)] public string pDataType;
    }

    [DllImport("winspool.drv", EntryPoint = "OpenPrinterA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true)]
    private static extern bool OpenPrinter(string szPrinter, out IntPtr hPrinter, IntPtr pd);

    [DllImport("winspool.drv", EntryPoint = "ClosePrinter", SetLastError = true, ExactSpelling = true)]
    private static extern bool ClosePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", EntryPoint = "StartDocPrinterA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true)]
    private static extern bool StartDocPrinter(IntPtr hPrinter, int level, [In] ref DOCINFOA di);

    [DllImport("winspool.drv", EntryPoint = "EndDocPrinter", SetLastError = true, ExactSpelling = true)]
    private static extern bool EndDocPrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", EntryPoint = "StartPagePrinter", SetLastError = true, ExactSpelling = true)]
    private static extern bool StartPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", EntryPoint = "EndPagePrinter", SetLastError = true, ExactSpelling = true)]
    private static extern bool EndPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", EntryPoint = "WritePrinter", SetLastError = true, ExactSpelling = true)]
    private static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

    public static void SendBytesToPrinter(string printerName, byte[] bytes)
    {
        if (!OpenPrinter(printerName, out var hPrinter, IntPtr.Zero))
        {
            throw new InvalidOperationException(
                $"تعذر فتح الطابعة '{printerName}'. تأكد أن اسم الطابعة مطابق تماماً لاسمها في Windows.");
        }

        try
        {
            var docInfo = new DOCINFOA
            {
                pDocName = "Hospital Queue Ticket",
                pOutputFile = null,
                pDataType = "RAW",
            };

            if (!StartDocPrinter(hPrinter, 1, ref docInfo))
            {
                throw new InvalidOperationException("تعذر بدء مهمة الطباعة (StartDocPrinter).");
            }

            try
            {
                if (!StartPagePrinter(hPrinter))
                {
                    throw new InvalidOperationException("تعذر بدء صفحة الطباعة (StartPagePrinter).");
                }

                var unmanagedBytes = Marshal.AllocCoTaskMem(bytes.Length);
                try
                {
                    Marshal.Copy(bytes, 0, unmanagedBytes, bytes.Length);
                    if (!WritePrinter(hPrinter, unmanagedBytes, bytes.Length, out var written) || written != bytes.Length)
                    {
                        throw new InvalidOperationException("تعذر إرسال البيانات إلى الطابعة (WritePrinter).");
                    }
                }
                finally
                {
                    Marshal.FreeCoTaskMem(unmanagedBytes);
                }

                EndPagePrinter(hPrinter);
            }
            finally
            {
                EndDocPrinter(hPrinter);
            }
        }
        finally
        {
            ClosePrinter(hPrinter);
        }
    }
}
