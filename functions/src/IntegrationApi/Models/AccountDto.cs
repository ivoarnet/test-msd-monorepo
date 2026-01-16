namespace IntegrationApi.Models;

/// <summary>
/// Represents an account entity from Dataverse.
/// </summary>
public class AccountDto
{
    /// <summary>
    /// Gets or sets the unique identifier for the account.
    /// </summary>
    /// <example>d290f1ee-6c54-4b01-90e6-d701748f0851</example>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the account name.
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

    /// <summary>
    /// Gets or sets the account creation date.
    /// </summary>
    public DateTime CreatedOn { get; set; }

    /// <summary>
    /// Gets or sets the last modification date.
    /// </summary>
    public DateTime ModifiedOn { get; set; }
}
