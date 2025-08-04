using Going.Plaid;
using Going.Plaid.Item;
using PlaidMCP.Security;
using System.ComponentModel;
using ModelContextProtocol.Server;

namespace PlaidMCP.Tools;

[McpServerToolType]
public static class PlaidRemoveItemTool
{
    [McpServerTool, Description("Revoke and remove an Item")]
    public static async Task<string> PlaidRemoveItem(
        PlaidClient client, 
        ITokenStore store, 
        [Description("Your internal user id")] string user_id, 
        [Description("Item reference from PlaidExchangePublicToken")] string item_ref)
    {
        try
        {
            var token = await store.GetAccessTokenAsync(user_id, item_ref)
                ?? throw new InvalidOperationException($"Unknown item_ref: {SafeLog.Redact(item_ref)}");
                
            await client.ItemRemoveAsync(new ItemRemoveRequest { AccessToken = token });
            await store.RemoveAsync(user_id, item_ref);
            
            return System.Text.Json.JsonSerializer.Serialize(new 
            { 
                removed = true,
                success = true
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