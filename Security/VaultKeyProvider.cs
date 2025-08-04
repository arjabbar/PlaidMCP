using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace PlaidMCP.Security;

public static class VaultKeyProvider
{
    // returns: (key, isEphemeral)
    public static (byte[] key, bool ephemeral) GetKey()
    {
        var env = Environment.GetEnvironmentVariable("PLAIDMCP_VAULT_KEY");
        if (!string.IsNullOrWhiteSpace(env))
        {
            // expected base64 32 bytes
            var key = Convert.FromBase64String(env);
            if (key.Length != 32) throw new InvalidOperationException("PLAIDMCP_VAULT_KEY must be 32 bytes (base64).");
            return (key, false);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // derive a stable per-user key with DPAPI
            var entropy = System.Text.Encoding.UTF8.GetBytes("PlaidMCP.LocalVault/v1");
            var protectedKey = ProtectedData.Protect(RandomNumberGenerator.GetBytes(32), entropy, DataProtectionScope.CurrentUser);
            var unprotected = ProtectedData.Unprotect(protectedKey, entropy, DataProtectionScope.CurrentUser);
            return (unprotected, false);
        }

        // Fallback ephemeral key (warn in logs)
        return (RandomNumberGenerator.GetBytes(32), true);
    }
}