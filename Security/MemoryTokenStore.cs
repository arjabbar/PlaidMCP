using System.Collections.Concurrent;

namespace PlaidMCP.Security;

public sealed class MemoryTokenStore : ITokenStore
{
    private readonly ConcurrentDictionary<(string userId, string itemRef), ItemSecret> _items = new();

    public Task<string> PutAsync(string userId, string itemId, string accessToken, string? institutionId)
    {
        string itemRef = $"item_{Guid.NewGuid().ToString("N")[..4]}";
        var now = DateTime.UtcNow;
        var secret = new ItemSecret(itemRef, itemId, institutionId ?? "", accessToken, null, now, now);
        _items[(userId, itemRef)] = secret with { EncryptedAccessToken = accessToken }; // plain in memory
        return Task.FromResult(itemRef);
    }

    public Task<string?> GetAccessTokenAsync(string userId, string itemRefOrItemId)
    {
        var result = _items.FirstOrDefault(kv =>
              kv.Key.userId == userId && (kv.Key.itemRef == itemRefOrItemId || kv.Value.ItemId == itemRefOrItemId)).Value?.EncryptedAccessToken;
        return Task.FromResult(result);
    }

    public Task<ItemSecret?> GetAsync(string userId, string itemRefOrItemId)
    {
        var result = _items.FirstOrDefault(kv =>
              kv.Key.userId == userId && (kv.Key.itemRef == itemRefOrItemId || kv.Value.ItemId == itemRefOrItemId)).Value;
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<ItemSecret>> ListAsync(string userId)
    {
        var result = _items.Where(kv => kv.Key.userId == userId).Select(kv => kv.Value).ToList();
        return Task.FromResult<IReadOnlyList<ItemSecret>>(result);
    }

    public Task RemoveAsync(string userId, string itemRefOrItemId)
    {
        var key = _items.Keys.FirstOrDefault(k => k.userId == userId &&
                       (_items[k].ItemRef == itemRefOrItemId || _items[k].ItemId == itemRefOrItemId));
        if (key != default) _items.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task UpdateCursorAsync(string userId, string itemRefOrItemId, string? cursor)
    {
        var pair = _items.FirstOrDefault(kv =>
            kv.Key.userId == userId && (kv.Key.itemRef == itemRefOrItemId || kv.Value.ItemId == itemRefOrItemId));
        if (pair.Value is not null) _items[pair.Key] = pair.Value with { TransactionsCursor = cursor, LastUsedAtUtc = DateTime.UtcNow };
        return Task.CompletedTask;
    }
}