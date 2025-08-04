using System.Text.Json;
using System.Text.Json.Serialization;
using System.Runtime.InteropServices;

namespace PlaidMCP.Security;

public sealed class FileTokenStore : ITokenStore
{
    private readonly string _path;
    private readonly byte[] _key;
    private readonly bool _ephemeral;

    private sealed class VaultDoc
    {
        [JsonPropertyName("version")] public int Version { get; set; } = 1;
        [JsonPropertyName("users")] public Dictionary<string, Dictionary<string, ItemSecret>> Users { get; set; } = new();
    }

    public FileTokenStore(string? root = null)
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var dir = root ?? Path.Combine(home, ".plaidmcp", "v1");
        Directory.CreateDirectory(dir);
        EnsurePermissions(dir);
        _path = Path.Combine(dir, "tokens.json.gcm");
        var (key, eph) = VaultKeyProvider.GetKey();
        _key = key; _ephemeral = eph;
        if (!File.Exists(_path)) File.WriteAllText(_path, JsonSerializer.Serialize(new VaultDoc()));
        EnsurePermissions(_path);
    }

    public async Task<string> PutAsync(string userId, string itemId, string accessToken, string? institutionId)
    {
        string itemRef = $"item_{Guid.NewGuid().ToString("N")[..4]}";
        var now = DateTime.UtcNow;
        var doc = await LoadAsync();
        if (!doc.Users.TryGetValue(userId, out var dict)) doc.Users[userId] = dict = new();

        var (payload, _) = AesGcmCrypter.EncryptToBase64(accessToken, _key);
        dict[itemRef] = new ItemSecret(itemRef, itemId, institutionId ?? "", payload, null, now, now);
        await SaveAsync(doc);
        return itemRef;
    }

    public async Task<string?> GetAccessTokenAsync(string userId, string itemRefOrItemId)
    {
        var item = await GetAsync(userId, itemRefOrItemId);
        if (item is null) return null;
        return AesGcmCrypter.DecryptFromBase64(item.EncryptedAccessToken, _key);
    }

    public async Task<ItemSecret?> GetAsync(string userId, string itemRefOrItemId)
    {
        var doc = await LoadAsync();
        if (!doc.Users.TryGetValue(userId, out var dict)) return null;
        var match = dict.Values.FirstOrDefault(i => i.ItemRef == itemRefOrItemId || i.ItemId == itemRefOrItemId);
        return match;
    }

    public async Task<IReadOnlyList<ItemSecret>> ListAsync(string userId)
    {
        var doc = await LoadAsync();
        if (!doc.Users.TryGetValue(userId, out var dict)) return Array.Empty<ItemSecret>();
        return dict.Values.OrderBy(i => i.CreatedAtUtc).ToList();
    }

    public async Task RemoveAsync(string userId, string itemRefOrItemId)
    {
        var doc = await LoadAsync();
        if (doc.Users.TryGetValue(userId, out var dict))
        {
            var key = dict.Keys.FirstOrDefault(k => k == itemRefOrItemId || dict[k].ItemId == itemRefOrItemId);
            if (key is not null) dict.Remove(key);
            await SaveAsync(doc);
        }
    }

    public async Task UpdateCursorAsync(string userId, string itemRefOrItemId, string? cursor)
    {
        var doc = await LoadAsync();
        if (!doc.Users.TryGetValue(userId, out var dict)) return;
        var kv = dict.FirstOrDefault(p => p.Key == itemRefOrItemId || p.Value.ItemId == itemRefOrItemId);
        if (!kv.Equals(default(KeyValuePair<string, ItemSecret>)))
        {
            dict[kv.Key] = kv.Value with { TransactionsCursor = cursor, LastUsedAtUtc = DateTime.UtcNow };
            await SaveAsync(doc);
        }
    }

    private async Task<VaultDoc> LoadAsync()
        => JsonSerializer.Deserialize<VaultDoc>(await File.ReadAllTextAsync(_path)) ?? new VaultDoc();

    private async Task SaveAsync(VaultDoc doc)
    {
        var json = JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_path, json);
        EnsurePermissions(_path);
    }

    private static void EnsurePermissions(string path)
    {
        // Windows: leave to ACLs. Unix: chmod 600 for files, 700 for dirs.
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
        try
        {
            var isDir = Directory.Exists(path);
            var mode = isDir ? Convert.ToInt32("700", 8) : Convert.ToInt32("600", 8);
            // P/Invoke to chmod to avoid third-party packages
            Chmod(path, mode);
        }
        catch { /* best effort */ }
    }

    [System.Runtime.InteropServices.DllImport("libc", SetLastError = true)]
    private static extern int chmod(string pathname, int mode);
    private static void Chmod(string path, int mode) { try { chmod(path, mode); } catch { } }
}