namespace PlaidMCP.Security;

public static class SafeLog
{
    /// <summary>
    /// Redacts sensitive values from log output
    /// </summary>
    public static string Redact(string? value)
    {
        if (string.IsNullOrEmpty(value)) return value ?? "";
        
        // For access tokens, public tokens, and secrets - show only first/last few chars
        if (value.StartsWith("access-") || value.StartsWith("public-") || value.StartsWith("secret-"))
        {
            if (value.Length <= 10) return "***";
            return $"{value[..4]}***{value[^4..]}";
        }
        
        // For other potentially sensitive data, just show length
        if (value.Length > 50)
        {
            return $"[REDACTED:{value.Length}chars]";
        }
        
        return value;
    }
    
    /// <summary>
    /// Redacts exception messages that might contain sensitive data
    /// </summary>
    public static string RedactException(Exception ex)
    {
        var message = ex.Message;
        // Simple redaction - replace any token-like strings
        message = System.Text.RegularExpressions.Regex.Replace(message, 
            @"\b(access-|public-|secret-)[a-zA-Z0-9_-]+", 
            "[REDACTED_TOKEN]");
        return message;
    }
}