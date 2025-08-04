namespace PlaidMCP.Security;

public sealed record ItemSecret(
    string ItemRef,              // e.g., "item_8a2c"
    string ItemId,               // Plaid item_id
    string InstitutionId,        // e.g., "ins_3"
    string EncryptedAccessToken, // base64 payload (nonce|cipher|tag) or JSON
    string? TransactionsCursor,  // last sync cursor
    DateTime CreatedAtUtc,
    DateTime LastUsedAtUtc
);