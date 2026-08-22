using System.Security.Cryptography;

namespace HospitalQueue.Domain.Constants;

/// <summary>Bearer tokens that let a dedicated kiosk/display device skip the password prompt.</summary>
public static class DeviceKey
{
    /// <summary>
    /// 32 hex characters (128 bits). Long enough that guessing it is hopeless,
    /// short enough to retype on a device with no keyboard if copy/paste fails.
    /// </summary>
    public static string Generate() => Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();

    /// <summary>
    /// Compares in constant time — these are bearer tokens, so a plain string
    /// comparison would leak their contents through timing.
    /// </summary>
    public static bool Matches(string? stored, string? provided)
    {
        if (string.IsNullOrEmpty(stored) || string.IsNullOrEmpty(provided))
        {
            return false;
        }

        var a = System.Text.Encoding.UTF8.GetBytes(stored);
        var b = System.Text.Encoding.UTF8.GetBytes(provided);
        return CryptographicOperations.FixedTimeEquals(a, b);
    }
}
