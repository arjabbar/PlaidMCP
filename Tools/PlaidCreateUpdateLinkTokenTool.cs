using Going.Plaid;
using Going.Plaid.Link;
using Going.Plaid.Entity;
using PlaidMCP.Security;
using System.ComponentModel;
using ModelContextProtocol.Server;

namespace PlaidMCP.Tools;

[McpServerToolType]
public static class PlaidCreateUpdateLinkTokenTool
{
    [McpServerTool, Description("Create an update-mode link token for relinking an existing item")]
    public static async Task<string> PlaidCreateUpdateLinkToken(
        PlaidClient client, 
        ITokenStore store,
        [Description("Your internal user id")] string user_id, 
        [Description("Item reference from PlaidExchangePublicToken")] string item_ref)
    {
        try
        {
            var accessToken = await store.GetAccessTokenAsync(user_id, item_ref)
                ?? throw new InvalidOperationException($"Unknown item_ref: {SafeLog.Redact(item_ref)}");
                
            var resp = await client.LinkTokenCreateAsync(new LinkTokenCreateRequest 
            {
                ClientName = "Plaid MCP Server",
                CountryCodes = new[] { CountryCode.Us },
                User = new LinkTokenCreateRequestUser { ClientUserId = user_id },
                AccessToken = accessToken // update mode
            });
            
            return System.Text.Json.JsonSerializer.Serialize(new 
            { 
                link_token = resp.LinkToken, 
                expires_at = resp.Expiration,
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