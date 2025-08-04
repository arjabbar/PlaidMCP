using Going.Plaid;
using Going.Plaid.Item;
using System.ComponentModel;
using ModelContextProtocol.Server;
using PlaidMCP.Security;

namespace PlaidMCP.Tools;

[McpServerToolType]
public static class PlaidExchangePublicTokenTool
{
    [McpServerTool, Description("Exchange Link public_token for a permanent access_token (stored securely as item_ref)")]
    public static async Task<string> PlaidExchangePublicToken(
        PlaidClient client,
        ITokenStore store,
        [Description("Your internal user id")] string user_id,
        [Description("Plaid Link public_token")] string public_token
    )
    {
        try
        {
            var resp = await client.ItemPublicTokenExchangeAsync(new ItemPublicTokenExchangeRequest { PublicToken = public_token });
            
            // Optionally call /institutions/get_by_id to fetch institution_id
            var itemId = resp.ItemId;
            var accessToken = resp.AccessToken;
            var itemRef = await store.PutAsync(user_id, itemId, accessToken, institutionId: null);

            // Return only alias; never the token
            return System.Text.Json.JsonSerializer.Serialize(new 
            { 
                item_ref = itemRef, 
                item_id = itemId,
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