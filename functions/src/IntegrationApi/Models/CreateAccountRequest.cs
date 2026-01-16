namespace IntegrationApi.Models;

/// <summary>
/// Request model for creating a new account.
/// </summary>
public class CreateAccountRequest
{
    /// <summary>
    /// Gets or sets the account name (required).
    /// </summary>
    /// <example>Contoso Ltd.</example>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the account number.
    /// </summary>
    /// <example>ACC-001234</example>
    public string? AccountNumber { get; set; }

    /// <summary>
    /// Gets or sets the primary email address.
    /// </summary>
    /// <example>contact@contoso.com</example>
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the primary phone number.
    /// </summary>
    /// <example>+1-555-0123</example>
    public string? Phone { get; set; }

    /// <summary>
    /// Gets or sets the annual revenue in USD.
    /// </summary>
    /// <example>1000000.00</example>
    public decimal? Revenue { get; set; }

    /// <summary>
    /// Gets or sets the industry classification.
    /// </summary>
    /// <example>Technology</example>
    public string? Industry { get; set; }
}
