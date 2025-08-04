using PlaidMCP.Security;
using System.ComponentModel;
using ModelContextProtocol.Server;

namespace PlaidMCP.Tools;

[McpServerToolType]
public static class PlaidListItemsTool
{
    [McpServerTool, Description("List linked items for a user (aliases only, no secrets)")]
    public static async Task<string> PlaidListItems(
        ITokenStore store, 
        [Description("Your internal user id")] string user_id)
    {
        try
        {
            var items = await store.ListAsync(user_id);
            return System.Text.Json.JsonSerializer.Serialize(new 
            {
                items = items.Select(i => new 
                {
                    i.ItemRef, 
                    i.ItemId, 
                    i.InstitutionId, 
                    i.TransactionsCursor, 
                    i.CreatedAtUtc, 
                    i.LastUsedAtUtc
                }),
                success = true,
                total_count = items.Count
            });
        }
        catch (Exception ex)
        {
            return System.Text.Json.JsonSerializer.Serialize(new 
            { 
                error = SafeLog.RedactException(ex),
                success = false 
            });
        }
    }
}