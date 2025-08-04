# Copilot Instructions for Plaid MCP Server

<!-- Use this file to provide workspace-specific custom instructions to Copilot. For more details, visit https://code.visualstudio.com/docs/copilot/copilot-customization#_use-a-githubcopilotinstructionsmd-file -->

This is a .NET MCP (Model Context Protocol) server project that integrates with Plaid for personal finance management.

## Project Overview
- **Framework**: .NET 9.0 console application
- **Purpose**: MCP server for AI agents to interact with Plaid financial APIs
- **Key Dependencies**: 
  - ModelContextProtocol (official C# SDK)
  - Going.Plaid (Plaid .NET client library)
  - Microsoft.Extensions.Hosting

## Architecture Guidelines
- The server exposes Plaid APIs as MCP tools through decorated static methods
- Uses dependency injection for PlaidClient configuration
- Implements proper error handling and JSON serialization for responses
- Supports both sandbox (testing) and production environments

## Environment Variables
- `PLAID_CLIENT_ID`: Your Plaid client ID
- `PLAID_SECRET`: Your Plaid secret key  
- `PLAID_ENV`: Environment (sandbox, development, production)

## Available MCP Tools
1. `PlaidCreateSandboxItem` - Create test items in sandbox
2. `PlaidListAccounts` - List all accounts for an access token
3. `PlaidBalances` - Get real-time account balances
4. `PlaidTransactionsSync` - Sync transactions with cursor-based pagination
5. `PlaidGetItemInfo` - Get item details and institution info
6. `PlaidCreateLinkToken` - Create link tokens for Plaid Link

## Best Practices
- Always handle exceptions and return structured JSON responses with success/error indicators
- Use proper Going.Plaid entity types and enums
- Follow MCP tool decoration patterns with `[McpServerTool]` and `[Description]` attributes
- Validate input parameters and provide helpful descriptions

## Additional Resources
You can find more info and examples at https://modelcontextprotocol.io/llms-full.txt

## Security Notes
- Never log or expose access tokens or sensitive credentials
- Store environment variables securely
- Use Plaid's sandbox environment for development and testing
