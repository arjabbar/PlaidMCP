# Plaid MCP Server

A .NET MCP (Model Context Protocol) server that integrates with Plaid for personal finance management. This server provides secure, encrypted token storage and exposes Plaid APIs as MCP tools for AI agents.

## 🔒 Security Features

### Secure Token Persistence
- **Encrypted Storage**: Access tokens are encrypted at rest using AES-GCM with 256-bit keys
- **Item References**: Tools use secure `item_ref` aliases instead of raw access tokens
- **No Secrets in I/O**: Access tokens and sensitive data are never returned in tool responses
- **Configurable Storage**: Choose between ephemeral (memory) or persistent (encrypted disk) storage
- **Cross-Platform**: Windows DPAPI support with AES-GCM fallback for other platforms

### Storage Backends
- **MemoryTokenStore** (default): Ephemeral in-memory storage for development/testing
- **FileTokenStore**: AES-GCM encrypted JSON storage in `~/.plaidmcp/v1/tokens.json.gcm`

## 🚀 Quick Start

### Installation & Setup

1. **Clone and build:**
   ```bash
   git clone https://github.com/arjabbar/PlaidMCP.git
   cd PlaidMCP
   dotnet build
   ```

2. **Set up environment variables:**
   ```bash
   export PLAID_CLIENT_ID=your_client_id
   export PLAID_SECRET=your_secret_key
   export PLAID_ENV=sandbox  # or development, production
   
   # Optional: Enable persistent storage
   export PLAIDMCP_PERSIST=true
   
   # Optional: Custom encryption key (recommended for production)
   export PLAIDMCP_VAULT_KEY=$(openssl rand -base64 32)
   ```

3. **Run the server:**
   ```bash
   # Ephemeral storage (default)
   dotnet run
   
   # Persistent storage
   dotnet run -- --persist
   # OR
   PLAIDMCP_PERSIST=true dotnet run
   ```

## 🔧 Available Tools

### Token Management Tools
- **PlaidCreateLinkToken**: Create Link tokens for Plaid Link initialization
- **PlaidExchangePublicToken**: Exchange public_token for secure item_ref (stores encrypted access_token)
- **PlaidListItems**: List all linked items for a user (shows item_ref, metadata, no secrets)
- **PlaidCreateUpdateLinkToken**: Create update-mode Link tokens for relinking existing items
- **PlaidRemoveItem**: Revoke and remove items (calls Plaid API and removes local storage)

### Account & Transaction Tools
- **PlaidListAccounts**: List accounts for an item_ref
- **PlaidBalances**: Get real-time account balances for an item_ref
- **PlaidTransactionsSync**: Sync transactions with automatic cursor persistence
- **PlaidGetItemInfo**: Get item details and institution information
- **PlaidCreateSandboxItem**: Create sandbox items for testing (development only)

### Key Changes from v1
- ✅ All tools now use `item_ref` instead of `access_token` parameters
- ✅ Automatic transaction cursor storage and retrieval
- ✅ Exception messages are redacted to prevent token leakage
- ✅ Access tokens never appear in tool responses

## 📋 Typical Workflow

### Initial Setup
1. **Create Link Token**:
   ```
   PlaidCreateLinkToken(user_id="user123", user_name="John Doe")
   → Returns: { link_token: "link-sandbox-...", expires_at: "..." }
   ```

2. **Complete Plaid Link** (in your app/frontend):
   - Use the link_token to initialize Plaid Link
   - User completes bank connection
   - Plaid Link returns a public_token

3. **Exchange for Secure Reference**:
   ```
   PlaidExchangePublicToken(user_id="user123", public_token="public-sandbox-...")
   → Returns: { item_ref: "item_a1b2", item_id: "...", success: true }
   ```

### Using Stored Items
4. **List Your Items**:
   ```
   PlaidListItems(user_id="user123")
   → Returns: { items: [{ item_ref: "item_a1b2", item_id: "...", ... }] }
   ```

5. **Access Account Data**:
   ```
   PlaidListAccounts(user_id="user123", item_ref="item_a1b2")
   PlaidBalances(user_id="user123", item_ref="item_a1b2")
   PlaidTransactionsSync(user_id="user123", item_ref="item_a1b2")
   ```

### Maintenance
6. **Update/Relink When Needed**:
   ```
   PlaidCreateUpdateLinkToken(user_id="user123", item_ref="item_a1b2")
   → Returns: { link_token: "link-update-...", expires_at: "..." }
   ```

7. **Remove Items**:
   ```
   PlaidRemoveItem(user_id="user123", item_ref="item_a1b2")
   → Returns: { removed: true, success: true }
   ```

## 🔐 Security Configuration

### Encryption Key Management

**Recommended for Production (Linux/macOS):**
```bash
# Generate and store a master key
export PLAIDMCP_VAULT_KEY=$(openssl rand -base64 32)
echo "PLAIDMCP_VAULT_KEY=$PLAIDMCP_VAULT_KEY" >> ~/.bashrc
```

**Windows:**
- Uses DPAPI by default (no manual key needed)
- Optionally set `PLAIDMCP_VAULT_KEY` for cross-platform compatibility

**Key Management Options:**
1. **Environment Variable**: `PLAIDMCP_VAULT_KEY` (base64-encoded 32 bytes)
2. **Windows DPAPI**: Automatic per-user key derivation (Windows only)
3. **Ephemeral Fallback**: Random key per session (development only)

### Storage Security
- **File Permissions**: Unix files/directories automatically set to 600/700
- **Encryption**: AES-GCM with 256-bit keys and 16-byte authentication tags
- **No Plaintext**: Access tokens never stored in plaintext
- **Zero Dependencies**: No external key management services required

## 🛠️ Development

### Environment Variables
```bash
PLAID_CLIENT_ID=your_client_id      # Required
PLAID_SECRET=your_secret_key        # Required  
PLAID_ENV=sandbox                   # Optional: sandbox|development|production
PLAIDMCP_PERSIST=true              # Optional: Enable persistent storage
PLAIDMCP_VAULT_KEY=<base64-key>    # Optional: Custom encryption key
```

### Project Structure
```
PlaidMCP/
├── Program.cs                     # Main application with legacy tools
├── PlaidMCP.csproj               # Project configuration
├── Security/                     # Security infrastructure
│   ├── ITokenStore.cs           # Token storage interface
│   ├── ItemSecret.cs            # Encrypted token metadata model
│   ├── MemoryTokenStore.cs      # In-memory storage implementation
│   ├── FileTokenStore.cs        # Encrypted file storage implementation
│   ├── AesGcmCrypter.cs         # AES-GCM encryption utilities
│   ├── VaultKeyProvider.cs      # Cross-platform key derivation
│   └── SafeLog.cs               # Sensitive data redaction
└── Tools/                       # New secure MCP tools
    ├── PlaidExchangePublicTokenTool.cs
    ├── PlaidListItemsTool.cs
    ├── PlaidCreateUpdateLinkTokenTool.cs
    └── PlaidRemoveItemTool.cs
```

## 📄 License

This project is licensed under the MIT License.