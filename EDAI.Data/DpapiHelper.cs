using System.Security.Cryptography;
using System.Text;

namespace EDAI.Data;

// Thin wrapper around Windows DPAPI (DataProtectionScope.CurrentUser).
// Encrypted values are stored as Base64 strings in the database.
// Both methods return null on failure rather than throwing — a failed decrypt
// is treated as "no key stored" and the user is prompted to re-enter.
internal static class DpapiHelper
{
    public static string? Encrypt(string? plainText)
    {
        if (plainText is null) return null;
        try
        {
            var bytes = Encoding.UTF8.GetBytes(plainText);
            var encrypted = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encrypted);
        }
        catch (CryptographicException)
        {
            return null;
        }
    }

    public static string? Decrypt(string? encryptedBase64)
    {
        if (encryptedBase64 is null) return null;
        try
        {
            var bytes = Convert.FromBase64String(encryptedBase64);
            var decrypted = ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decrypted);
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException)
        {
            // Different user, different machine, or corrupted data — treat as no key.
            return null;
        }
    }
}
