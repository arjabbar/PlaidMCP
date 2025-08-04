using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;
using Going.Plaid;
using Going.Plaid.Entity;
using Going.Plaid.Sandbox;
using Going.Plaid.Accounts;
using Going.Plaid.Transactions;
using Going.Plaid.Item;
using Going.Plaid.Link;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Information);

// Configure Plaid client with environment variables
builder.Services.AddSingleton(_ =>
{
    var env = System.Environment.GetEnvironmentVariable("PLAID_ENV") ?? "sandbox";
    var environment = env.ToLower() switch
    {
        "production" => Going.Plaid.Environment.Production,
        "development" => Going.Plaid.Environment.Development,
        _ => Going.Plaid.Environment.Sandbox
    };

    // Get credentials from environment variables
    var secret = System.Environment.GetEnvironmentVariable("PLAID_SECRET") 
        ?? throw new InvalidOperationException("PLAID_SECRET environment variable is required");
    var clientId = System.Environment.GetEnvironmentVariable("PLAID_CLIENT_ID") 
        ?? throw new InvalidOperationException("PLAID_CLIENT_ID environment variable is required");

    return new PlaidClient(environment, secret: secret, clientId: clientId);
});

// Configure MCP server
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()           // stdio server for MCP hosts
    .WithToolsFromAssembly();              // discover [McpServerTool]s

await builder.Build().RunAsync();

[McpServerToolType]
public static class PlaidTools
{
    /// <summary>
    /// Create a Sandbox Item and return an access_token for testing purposes.
    /// </summary>
    [McpServerTool, Description("Sandbox: create Item and return access_token for testing")]
    public static async Task<string> PlaidCreateSandboxItem(
        PlaidClient plaid,
        [Description("Plaid institution id, e.g., ins_3")] string institution_id = "ins_3",
        [Description("Products to enable (comma-separated): transactions,identity,assets")] string products = "transactions")
    {
        try
        {
            var productsList = products.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim().ToLower() switch
                {
                    "transactions" => Products.Transactions,
                    "identity" => Products.Identity,
                    "assets" => Products.Assets,
                    "investments" => Products.Investments,
                    "liabilities" => Products.Liabilities,
                    _ => Products.Transactions
                }).ToArray();
            
            var publicTokenRequest = new SandboxPublicTokenCreateRequest
            {
                InstitutionId = institution_id,
                InitialProducts = productsList
            };
            
            var publicTokenResponse = await plaid.SandboxPublicTokenCreateAsync(publicTokenRequest);
            
            var exchangeRequest = new ItemPublicTokenExchangeRequest 
            { 
                PublicToken = publicTokenResponse.PublicToken 
            };
            
            var exchangeResponse = await plaid.ItemPublicTokenExchangeAsync(exchangeRequest);
            
            return System.Text.Json.JsonSerializer.Serialize(new 
            { 
                access_token = exchangeResponse.AccessToken,
                item_id = exchangeResponse.ItemId,
                institution_id = institution_id,
                products = products,
                success = true
            });
        }
        catch (Exception ex)
        {
            return System.Text.Json.JsonSerializer.Serialize(new 
            { 
                error = ex.Message,
                success = false 
            });
        }
    }

    /// <summary>
    /// List all accounts for a given access_token.
    /// </summary>
    [McpServerTool, Description("List accounts for an access_token")]
    public static async Task<string> PlaidListAccounts(
        PlaidClient plaid, 
        [Description("Access token for the item")] string access_token)
    {
        try
        {
            var request = new AccountsGetRequest { AccessToken = access_token };
            var response = await plaid.AccountsGetAsync(request);
            
            var accounts = response.Accounts.Select(account => new
            {
                account_id = account.AccountId,
                name = account.Name,
                official_name = account.OfficialName,
                type = account.Type.ToString(),
                subtype = account.Subtype?.ToString(),
                mask = account.Mask,
                balances = new
                {
                    available = account.Balances?.Available,
                    current = account.Balances?.Current,
                    limit = account.Balances?.Limit,
                    iso_currency_code = account.Balances?.IsoCurrencyCode
                }
            });

            return System.Text.Json.JsonSerializer.Serialize(new
            {
                accounts = accounts,
                success = true,
                total_count = accounts.Count()
            });
        }
        catch (Exception ex)
        {
            return System.Text.Json.JsonSerializer.Serialize(new 
            { 
                error = ex.Message,
                success = false 
            });
        }
    }

    /// <summary>
    /// Get real-time account balances for an access_token.
    /// </summary>
    [McpServerTool, Description("Get balances for an access_token")]
    public static async Task<string> PlaidBalances(
        PlaidClient plaid, 
        [Description("Access token for the item")] string access_token)
    {
        try
        {
            var request = new AccountsBalanceGetRequest { AccessToken = access_token };
            var response = await plaid.AccountsBalanceGetAsync(request);
            
            var balances = response.Accounts.Select(account => new
            {
                account_id = account.AccountId,
                name = account.Name,
                balances = new
                {
                    available = account.Balances?.Available,
                    current = account.Balances?.Current,
                    limit = account.Balances?.Limit,
                    iso_currency_code = account.Balances?.IsoCurrencyCode,
                    last_updated = DateTime.UtcNow
                }
            });

            return System.Text.Json.JsonSerializer.Serialize(new
            {
                balances = balances,
                success = true,
                retrieved_at = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return System.Text.Json.JsonSerializer.Serialize(new 
            { 
                error = ex.Message,
                success = false 
            });
        }
    }

    /// <summary>
    /// Sync transactions using cursor-based pagination for incremental updates.
    /// </summary>
    [McpServerTool, Description("Transactions sync; returns added/modified/removed and next_cursor")]
    public static async Task<string> PlaidTransactionsSync(
        PlaidClient plaid, 
        [Description("Access token for the item")] string access_token, 
        [Description("Cursor for pagination - null for initial sync")] string? cursor = null)
    {
        try
        {
            var request = new TransactionsSyncRequest
            {
                AccessToken = access_token,
                Cursor = cursor
            };
            
            var response = await plaid.TransactionsSyncAsync(request);
            
            var addedTransactions = response.Added.Select(tx => new
            {
                transaction_id = tx.TransactionId,
                account_id = tx.AccountId,
                amount = tx.Amount,
                date = tx.Date,
                name = tx.Name,
                merchant_name = tx.MerchantName,
                personal_finance_category = tx.PersonalFinanceCategory?.Primary,
                account_owner = tx.AccountOwner,
                authorized_date = tx.AuthorizedDate,
                iso_currency_code = tx.IsoCurrencyCode
            });

            var modifiedTransactions = response.Modified.Select(tx => new
            {
                transaction_id = tx.TransactionId,
                account_id = tx.AccountId,
                amount = tx.Amount,
                date = tx.Date,
                name = tx.Name,
                merchant_name = tx.MerchantName,
                personal_finance_category = tx.PersonalFinanceCategory?.Primary,
                account_owner = tx.AccountOwner,
                authorized_date = tx.AuthorizedDate,
                iso_currency_code = tx.IsoCurrencyCode
            });

            return System.Text.Json.JsonSerializer.Serialize(new 
            {
                added = addedTransactions,
                modified = modifiedTransactions,
                removed = response.Removed.Select(r => new { transaction_id = r.TransactionId }),
                next_cursor = response.NextCursor,
                has_more = response.HasMore,
                success = true,
                sync_timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return System.Text.Json.JsonSerializer.Serialize(new 
            { 
                error = ex.Message,
                success = false 
            });
        }
    }

    /// <summary>
    /// Get detailed information about a Plaid Item, including institution and available products.
    /// </summary>
    [McpServerTool, Description("Get item details including institution info and available products")]
    public static async Task<string> PlaidGetItemInfo(
        PlaidClient plaid, 
        [Description("Access token for the item")] string access_token)
    {
        try
        {
            var request = new ItemGetRequest { AccessToken = access_token };
            var response = await plaid.ItemGetAsync(request);
            
            var item = response.Item;
            
            return System.Text.Json.JsonSerializer.Serialize(new
            {
                item_id = item.ItemId,
                institution_id = item.InstitutionId,
                available_products = item.AvailableProducts?.Select(p => p.ToString()),
                billed_products = item.BilledProducts?.Select(p => p.ToString()),
                consent_expiration_time = item.ConsentExpirationTime,
                update_type = item.UpdateType?.ToString(),
                success = true
            });
        }
        catch (Exception ex)
        {
            return System.Text.Json.JsonSerializer.Serialize(new 
            { 
                error = ex.Message,
                success = false 
            });
        }
    }

    /// <summary>
    /// Create a link token for Plaid Link initialization (for production use).
    /// </summary>
    [McpServerTool, Description("Create a link token for Plaid Link initialization")]
    public static async Task<string> PlaidCreateLinkToken(
        PlaidClient plaid,
        [Description("Unique user identifier")] string user_id,
        [Description("User's legal name")] string user_name = "Default User",
        [Description("User's email address")] string? user_email = null,
        [Description("Products to enable (comma-separated): transactions,identity,assets")] string products = "transactions",
        [Description("Country codes (comma-separated ISO 3166-1 alpha-2)")] string country_codes = "US")
    {
        try
        {
            var productsList = products.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim().ToLower() switch
                {
                    "transactions" => Products.Transactions,
                    "identity" => Products.Identity,
                    "assets" => Products.Assets,
                    "investments" => Products.Investments,
                    "liabilities" => Products.Liabilities,
                    _ => Products.Transactions
                }).ToArray();

            var countryCodes = country_codes.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(c => c.Trim().ToUpper() switch
                {
                    "US" => CountryCode.Us,
                    "CA" => CountryCode.Ca,
                    "GB" => CountryCode.Gb,
                    "FR" => CountryCode.Fr,
                    "ES" => CountryCode.Es,
                    "NL" => CountryCode.Nl,
                    "IE" => CountryCode.Ie,
                    _ => CountryCode.Us
                }).ToArray();

            var request = new LinkTokenCreateRequest
            {
                Products = productsList,
                ClientName = "Plaid MCP Server",
                CountryCodes = countryCodes,
                User = new LinkTokenCreateRequestUser
                {
                    ClientUserId = user_id,
                    LegalName = user_name,
                    EmailAddress = user_email
                }
            };

            var response = await plaid.LinkTokenCreateAsync(request);

            return System.Text.Json.JsonSerializer.Serialize(new
            {
                link_token = response.LinkToken,
                expiration = response.Expiration,
                success = true
            });
        }
        catch (Exception ex)
        {
            return System.Text.Json.JsonSerializer.Serialize(new 
            { 
                error = ex.Message,
                success = false 
            });
        }
    }
}
