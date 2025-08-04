namespace PlaidMCP.Security;

public interface ITokenStore
{
    Task<string> PutAsync(string userId, string itemId, string accessToken, string? institutionId);
    Task<string?> GetAccessTokenAsync(string userId, string itemRefOrItemId);
    Task<ItemSecret?> GetAsync(string userId, string itemRefOrItemId);
    Task<IReadOnlyList<ItemSecret>> ListAsync(string userId);
    Task RemoveAsync(string userId, string itemRefOrItemId);
    Task UpdateCursorAsync(string userId, string itemRefOrItemId, string? cursor);
}