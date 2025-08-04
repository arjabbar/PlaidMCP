# Plaid MCP Server

A .NET Model Context Protocol (MCP) server that provides AI agents with access to Plaid's banking APIs for personal finance management.

## Overview

This MCP server exposes Plaid's financial data APIs as tools that AI agents can use to help users manage their personal finances. It supports both sandbox testing and production environments, providing a secure and structured way for AI assistants to interact with banking data.

## Features

- **Account Management**: List accounts and get real-time balances
- **Transaction Sync**: Incremental transaction synchronization with cursor-based pagination
- **Sandbox Testing**: Create test items and data for development
- **Link Token Creation**: Generate tokens for Plaid Link integration
- **Item Information**: Retrieve detailed information about connected financial institutions

## Prerequisites

- .NET 9.0 or later
- Plaid account with API credentials
- MCP-compatible client (like Claude Desktop)

## Installation

1. Clone the repository:
```bash
git clone <repository-url>
cd PlaidMCP
```

2. Restore dependencies:
```bash
dotnet restore
```

3. Build the project:
```bash
dotnet build
```

## Configuration

Set the following environment variables:

```bash
export PLAID_CLIENT_ID="your_plaid_client_id"
export PLAID_SECRET="your_plaid_secret"
export PLAID_ENV="sandbox"  # or "development" or "production"
```

## Usage

### Running the Server

```bash
PLAID_CLIENT_ID=your_id PLAID_SECRET=your_secret PLAID_ENV=sandbox dotnet run
```

### Available Tools

The server exposes the following tools to MCP clients:

#### `PlaidCreateSandboxItem`
Create a sandbox item for testing purposes.
- **Parameters**: `institution_id`, `products`
- **Returns**: Access token and item details

#### `PlaidListAccounts`
List all accounts for a given access token.
- **Parameters**: `access_token`
- **Returns**: Account details including balances

#### `PlaidBalances`
Get real-time account balances.
- **Parameters**: `access_token`
- **Returns**: Current balance information for all accounts

#### `PlaidTransactionsSync`
Sync transactions with incremental updates.
- **Parameters**: `access_token`, `cursor` (optional)
- **Returns**: Added, modified, and removed transactions with next cursor

#### `PlaidGetItemInfo`
Get detailed information about a Plaid item.
- **Parameters**: `access_token`
- **Returns**: Item details, institution info, and available products

#### `PlaidCreateLinkToken`
Create a link token for Plaid Link initialization.
- **Parameters**: `user_id`, `user_name`, `user_email`, `products`, `country_codes`
- **Returns**: Link token and expiration

## MCP Client Configuration

To use this server with Claude Desktop, add the following to your MCP configuration:

```json
{
  "mcpServers": {
    "plaid": {
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/PlaidMCP"],
      "env": {
        "PLAID_CLIENT_ID": "your_plaid_client_id",
        "PLAID_SECRET": "your_plaid_secret", 
        "PLAID_ENV": "sandbox"
      }
    }
  }
}
```

## Development

### Project Structure

```
PlaidMCP/
├── Program.cs              # Main MCP server implementation
├── PlaidMCP.csproj        # Project file with dependencies
├── .github/
│   └── copilot-instructions.md
└── README.md
```

### Adding New Tools

To add new Plaid API endpoints:

1. Create a new static method in the `PlaidTools` class
2. Decorate with `[McpServerTool]` and `[Description]` attributes
3. Add proper parameter descriptions using `[Description]` attributes
4. Implement error handling and return structured JSON responses

### Testing

Use Plaid's sandbox environment for testing:

1. Set `PLAID_ENV=sandbox`
2. Use the `PlaidCreateSandboxItem` tool to create test data
3. Test other tools with the generated access tokens

## Security Considerations

- **Never log access tokens** - They provide full access to user financial data
- **Use environment variables** for sensitive configuration
- **Validate all inputs** to prevent injection attacks
- **Follow Plaid's security guidelines** for production deployments

## Dependencies

- **ModelContextProtocol** (0.3.0-preview.3) - Official C# MCP SDK
- **Going.Plaid** (6.47.1) - Plaid .NET client library
- **Microsoft.Extensions.Hosting** (9.0.7) - Hosting framework

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support

For issues related to:
- **Plaid APIs**: Check [Plaid's documentation](https://plaid.com/docs/)
- **MCP Protocol**: See [MCP documentation](https://modelcontextprotocol.io/)
- **This server**: Open an issue in this repository
