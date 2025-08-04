using System.Security.Cryptography;
using System.Text;

namespace PlaidMCP.Security;

public static class AesGcmCrypter
{
    public static (string payload, byte[] keyUsed) EncryptToBase64(string plaintext, byte[] key)
    {
        // payload format: base64(nonce|cipher|tag)
        byte[] nonce = RandomNumberGenerator.GetBytes(12);
        byte[] plainBytes = Encoding.UTF8.GetBytes(plaintext);
        byte[] cipher = new byte[plainBytes.Length];
        byte[] tag = new byte[16];

        using var gcm = new AesGcm(key, 16);
        gcm.Encrypt(nonce, plainBytes, cipher, tag);

        byte[] payload = new byte[nonce.Length + cipher.Length + tag.Length];
        Buffer.BlockCopy(nonce, 0, payload, 0, nonce.Length);
        Buffer.BlockCopy(cipher, 0, payload, nonce.Length, cipher.Length);
        Buffer.BlockCopy(tag, 0, payload, nonce.Length + cipher.Length, tag.Length);

        return (Convert.ToBase64String(payload), key);
    }

    public static string DecryptFromBase64(string payload, byte[] key)
    {
        byte[] bytes = Convert.FromBase64String(payload);
        var nonce = bytes[..12];
        var tag = bytes[^16..];
        var cipher = bytes[12..^16];
        var plain = new byte[cipher.Length];

        using var gcm = new AesGcm(key, 16);
        gcm.Decrypt(nonce, cipher, tag, plain);
        return Encoding.UTF8.GetString(plain);
    }
}