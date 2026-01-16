# .NET Coding Standards for Dynamics 365 Plugins

These standards ensure consistent, maintainable, and high-quality .NET code across our Dynamics 365 plugins and custom APIs.

---

## 📋 General Principles

1. **Follow Microsoft Guidelines**: Base standard is [Microsoft's C# Coding Conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
2. **Fail Fast**: Validate inputs early and throw meaningful exceptions
3. **Logging**: Use tracing for debugging production issues
4. **Security**: Never log sensitive data (passwords, API keys, PII)
5. **Performance**: Minimize queries, avoid N+1 problems, use early-bound types

---

## 🏗️ Architecture Patterns

### Plugin Structure

All plugins should inherit from a base plugin class that uses the modern LocalPluginContext pattern:

```csharp
/// <summary>
/// Base class for all plugins using the modern LocalPluginContext pattern.
/// This approach provides better access to plugin context and services.
/// </summary>
public abstract class PluginBase : IPlugin
{
    protected string UnsecureConfig { get; }
    protected string SecureConfig { get; }

    public PluginBase(string unsecureConfig, string secureConfig)
    {
        UnsecureConfig = unsecureConfig;
        SecureConfig = secureConfig;
    }

    public void Execute(IServiceProvider serviceProvider)
    {
        if (serviceProvider == null)
        {
            throw new ArgumentNullException(nameof(serviceProvider));
        }

        // Use LocalPluginContext to encapsulate plugin services
        var localContext = new LocalPluginContext(serviceProvider);

        try
        {
            localContext.Trace($"Executing {GetType().Name}");
            
            // Log context summary for better debugging (available in IPluginExecutionContext4)
            if (localContext.PluginExecutionContext is IPluginExecutionContext4 context4)
            {
                localContext.Trace($"Context Summary: {context4.ContextSummary}");
            }
            
            ExecutePlugin(localContext);
        }
        catch (InvalidPluginExecutionException)
        {
            // Re-throw plugin execution exceptions as-is
            throw;
        }
        catch (Exception ex)
        {
            localContext.Trace($"Exception: {ex.Message}");
            localContext.Trace($"Stack Trace: {ex.StackTrace}");
            throw new InvalidPluginExecutionException($"Error in {GetType().Name}: {ex.Message}", ex);
        }
    }

    protected abstract void ExecutePlugin(LocalPluginContext localContext);
}

/// <summary>
/// LocalPluginContext encapsulates plugin services and provides easy access to them.
/// This is the modern pattern recommended by Microsoft.
/// </summary>
public class LocalPluginContext
{
    private readonly IServiceProvider _serviceProvider;
    private IPluginExecutionContext _pluginExecutionContext;
    private IOrganizationServiceFactory _organizationServiceFactory;
    private ITracingService _tracingService;

    public LocalPluginContext(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Gets the plugin execution context (cached).
    /// </summary>
    public IPluginExecutionContext PluginExecutionContext
    {
        get
        {
            if (_pluginExecutionContext == null)
            {
                _pluginExecutionContext = (IPluginExecutionContext)_serviceProvider.GetService(typeof(IPluginExecutionContext));
            }
            return _pluginExecutionContext;
        }
    }

    /// <summary>
    /// Gets the tracing service (cached).
    /// </summary>
    public ITracingService TracingService
    {
        get
        {
            if (_tracingService == null)
            {
                _tracingService = (ITracingService)_serviceProvider.GetService(typeof(ITracingService));
            }
            return _tracingService;
        }
    }

    /// <summary>
    /// Gets the organization service factory (cached).
    /// </summary>
    public IOrganizationServiceFactory OrganizationServiceFactory
    {
        get
        {
            if (_organizationServiceFactory == null)
            {
                _organizationServiceFactory = (IOrganizationServiceFactory)_serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            }
            return _organizationServiceFactory;
        }
    }

    /// <summary>
    /// Gets the organization service for the current user.
    /// </summary>
    public IOrganizationService OrganizationService => OrganizationServiceFactory.CreateOrganizationService(PluginExecutionContext.UserId);

    /// <summary>
    /// Gets the organization service for the system user (elevated privileges).
    /// Use with caution - bypasses user security.
    /// </summary>
    public IOrganizationService SystemOrganizationService => OrganizationServiceFactory.CreateOrganizationService(null);

    /// <summary>
    /// Writes a trace message.
    /// </summary>
    public void Trace(string message)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            TracingService?.Trace($"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} - {message}");
        }
    }
}
```

**⚠️ Deprecated Pattern Warning**: The old pattern of directly calling `GetService(typeof(...))` in the Execute method and passing individual services to ExecutePlugin is still functional but considered outdated. The LocalPluginContext pattern provides better encapsulation and testability.

---

## 📝 Naming Conventions

### Classes and Methods

| Element | Convention | Example |
|---------|-----------|---------|
| **Plugin Class** | `{Entity}{Event}Plugin` | `AccountCreatePlugin` |
| **Custom API** | `{Action}Api` | `CalculateCommissionApi` |
| **Helper Class** | Descriptive noun | `DataverseHelper`, `ValidationService` |
| **Method** | Verb + noun | `ValidatePhoneNumber()`, `GetAccountById()` |
| **Private Field** | `_camelCase` | `_organizationService` |
| **Constant** | `PascalCase` | `MaxRetryCount` |

### Examples

```csharp
// ✅ Good - using modern LocalPluginContext pattern
public class AccountCreatePlugin : PluginBase
{
    public AccountCreatePlugin(string unsecureConfig, string secureConfig)
        : base(unsecureConfig, secureConfig)
    {
    }

    protected override void ExecutePlugin(LocalPluginContext localContext)
    {
        localContext.Trace("Starting account validation");
        
        var context = localContext.PluginExecutionContext;
        var account = GetTargetEntity<Account>(context);
        
        ValidateAccountName(account, localContext);
    }

    private void ValidateAccountName(Account account, LocalPluginContext localContext)
    {
        if (string.IsNullOrWhiteSpace(account.Name))
        {
            localContext.Trace("Account name validation failed");
            throw new InvalidPluginExecutionException("Account name is required.");
        }
        
        localContext.Trace("Account name validation passed");
    }
    
    private Account GetTargetEntity<T>(IPluginExecutionContext context) where T : Entity
    {
        if (context.InputParameters.Contains("Target") && 
            context.InputParameters["Target"] is Entity entity)
        {
            return entity.ToEntity<T>();
        }
        
        throw new InvalidPluginExecutionException("Target entity not found.");
    }
}

// ❌ Bad - unclear naming
public class Plugin1 : IPlugin
{
    public void Execute(IServiceProvider sp)
    {
        var x = DoStuff();
    }
}
```

---

## 🔍 Code Quality Rules

### 1. Input Validation

Always validate plugin context and input data:

```csharp
protected override void ExecutePlugin(LocalPluginContext localContext)
{
    var context = localContext.PluginExecutionContext;
    
    localContext.Trace($"Message: {context.MessageName}, Stage: {context.Stage}, Entity: {context.PrimaryEntityName}");
    
    // Validate context
    if (context.InputParameters == null || !context.InputParameters.Contains("Target"))
    {
        localContext.Trace("Target parameter is missing - exiting");
        throw new InvalidPluginExecutionException("Target parameter is missing.");
    }

    // Validate message name
    if (context.MessageName != "Create")
    {
        localContext.Trace($"Unexpected message: {context.MessageName} - exiting");
        return; // Exit early if wrong message
    }

    // Validate stage
    if (context.Stage != 20) // Pre-operation
    {
        localContext.Trace($"Unexpected stage: {context.Stage} - exiting");
        return;
    }

    // Validate entity
    var target = context.InputParameters["Target"] as Entity;
    if (target == null || target.LogicalName != "account")
    {
        localContext.Trace($"Invalid target entity - exiting");
        return;
    }

    // Now safe to proceed
    ValidateAccount(target, localContext);
}
```

### 2. Exception Handling

```csharp
// ✅ Good - specific exceptions with context
try
{
    var service = localContext.OrganizationService;
    service.Update(entity);
    localContext.Trace("Entity updated successfully");
}
catch (FaultException<OrganizationServiceFault> ex)
{
    localContext.Trace($"Dataverse error: {ex.Detail.Message}");
    localContext.Trace($"Error code: {ex.Detail.ErrorCode}");
    throw new InvalidPluginExecutionException($"Failed to update record: {ex.Detail.Message}", ex);
}

// ❌ Bad - swallowing exceptions
try
{
    DoSomething();
}
catch
{
    // Silent failure - never do this
}
```

### 3. Tracing

Use tracing extensively for production debugging:

```csharp
localContext.Trace("Starting account validation");
localContext.Trace($"Account ID: {account.Id}, Name: {account.Name}");

if (account.Name.Length > 100)
{
    localContext.Trace("Account name exceeds max length");
    throw new InvalidPluginExecutionException("Account name must be 100 characters or less.");
}

localContext.Trace("Account validation passed");
```

**⚠️ Never trace sensitive data**: Passwords, API keys, credit card numbers, etc.

**💡 Tip**: Use `IPluginExecutionContext4.ContextSummary` for improved debugging - it provides a comprehensive summary of the plugin execution context including pipeline stage, message, and entity information.

---

## 🚀 Performance Best Practices

### 1. Use Early-Bound Types

```csharp
// ✅ Good - early-bound (strongly-typed)
var account = context.InputParameters["Target"] as Entity;
var typedAccount = account.ToEntity<Account>();
string name = typedAccount.Name;

// ❌ Bad - late-bound (string keys, error-prone)
string name = account["name"].ToString();
```

### 2. Minimize Queries

```csharp
// ✅ Good - single query with ColumnSet
var query = new QueryExpression("contact")
{
    ColumnSet = new ColumnSet("firstname", "lastname", "emailaddress1"),
    Criteria = new FilterExpression
    {
        Conditions =
        {
            new ConditionExpression("parentcustomerid", ConditionOperator.Equal, accountId)
        }
    }
};
var contacts = service.RetrieveMultiple(query).Entities;

// ❌ Bad - retrieving all columns
var query = new QueryExpression("contact")
{
    ColumnSet = new ColumnSet(true) // Retrieves all columns - slow!
};
```

### 3. Batch Operations

```csharp
// ✅ Good - use ExecuteMultipleRequest for bulk operations
var requestWithResults = new ExecuteMultipleRequest
{
    Settings = new ExecuteMultipleSettings
    {
        ContinueOnError = false,
        ReturnResponses = true
    },
    Requests = new OrganizationRequestCollection()
};

foreach (var entity in entities)
{
    requestWithResults.Requests.Add(new CreateRequest { Target = entity });
}

var response = (ExecuteMultipleResponse)service.Execute(requestWithResults);

// ❌ Bad - individual creates (N queries problem)
foreach (var entity in entities)
{
    service.Create(entity);
}
```

---

## 🔒 Security Best Practices

### 1. Use System User When Needed

```csharp
// User context (respects security roles)
var userService = localContext.OrganizationService;

// System context (bypasses security - use cautiously)
var systemService = localContext.SystemOrganizationService;
```
```

### 2. Validate Permissions

```csharp
private bool UserHasPrivilege(IOrganizationService service, Guid userId, string privilegeName)
{
    var request = new RetrieveUserPrivilegesRequest { UserId = userId };
    var response = (RetrieveUserPrivilegesResponse)service.Execute(request);
    return response.RolePrivileges.Any(p => p.PrivilegeName == privilegeName);
}
```

### 3. Sanitize User Input

```csharp
// ✅ Good - validate and sanitize
private string SanitizeInput(string input)
{
    if (string.IsNullOrWhiteSpace(input))
    {
        return string.Empty;
    }

    // Remove potentially dangerous characters
    return input.Trim().Replace("<", "").Replace(">", "");
}

// ❌ Bad - using unsanitized input in queries
var fetchXml = $"<fetch><entity name='account'><filter><condition attribute='name' operator='eq' value='{userInput}'/></filter></entity></fetch>";
```

---

## 📖 Documentation Standards

### XML Documentation

All public members must have XML doc comments:

```csharp
/// <summary>
/// Validates that the account name meets business requirements.
/// </summary>
/// <param name="account">The account entity to validate.</param>
/// <exception cref="InvalidPluginExecutionException">Thrown when validation fails.</exception>
private void ValidateAccount(Account account)
{
    // Implementation
}
```

### Code Comments

Use comments to explain **why**, not **what**:

```csharp
// ✅ Good - explains reasoning
// We need to query related contacts to check if any have pending orders
// before allowing the account to be deactivated
var contacts = QueryRelatedContacts(accountId);

// ❌ Bad - states the obvious
// Loop through contacts
foreach (var contact in contacts)
{
    // Do something
}
```

---

## ✅ Code Review Checklist

Before submitting a PR, verify:

- [ ] Plugin inherits from `PluginBase` using LocalPluginContext pattern
- [ ] Input validation present (context, entity, attributes)
- [ ] Tracing statements added for debugging
- [ ] No sensitive data logged
- [ ] Early-bound types used
- [ ] Queries use specific ColumnSet (not `AllColumns`)
- [ ] Exceptions include meaningful messages
- [ ] XML documentation on public members
- [ ] No hardcoded credentials or environment-specific values
- [ ] Unit tests added/updated
- [ ] Performance considered (batch operations for loops)
- [ ] No deprecated patterns (direct `GetService(typeof(...))` calls in Execute method)
- [ ] Uses `IPluginExecutionContext4.ContextSummary` for enhanced debugging where applicable

---

## ⚠️ Deprecated Patterns

### Old Pattern: Direct Service Retrieval (Deprecated)

The traditional pattern of directly calling `GetService(typeof(...))` in the Execute method is still functional but outdated:

```csharp
// ❌ Deprecated Pattern
public void Execute(IServiceProvider serviceProvider)
{
    // Repeated GetService calls - harder to test and maintain
    var tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
    var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
    var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
    var orgService = serviceFactory.CreateOrganizationService(context.UserId);
    
    try
    {
        tracingService.Trace("Executing plugin");
        // Business logic
    }
    catch (Exception ex)
    {
        tracingService.Trace($"Error: {ex.Message}");
        throw;
    }
}
```

### Modern Pattern: LocalPluginContext (Recommended)

The modern approach uses LocalPluginContext for better encapsulation and testability:

```csharp
// ✅ Modern Pattern
public void Execute(IServiceProvider serviceProvider)
{
    var localContext = new LocalPluginContext(serviceProvider);
    
    try
    {
        localContext.Trace($"Executing {GetType().Name}");
        
        // Enhanced debugging with IPluginExecutionContext4
        if (localContext.PluginExecutionContext is IPluginExecutionContext4 context4)
        {
            localContext.Trace($"Context: {context4.ContextSummary}");
        }
        
        ExecutePlugin(localContext);
    }
    catch (InvalidPluginExecutionException)
    {
        throw;
    }
    catch (Exception ex)
    {
        localContext.Trace($"Error: {ex.Message}");
        throw new InvalidPluginExecutionException($"Error in {GetType().Name}: {ex.Message}", ex);
    }
}

protected abstract void ExecutePlugin(LocalPluginContext localContext);
```

### Benefits of Modern Pattern

1. **Better Encapsulation**: Services are properties of LocalPluginContext, not scattered across your code
2. **Lazy Loading**: Services are instantiated only when needed
3. **Caching**: Services are cached for reuse during plugin execution
4. **Testability**: LocalPluginContext can be mocked for unit testing
5. **Enhanced Debugging**: Easy access to `IPluginExecutionContext4.ContextSummary`
6. **Cleaner Code**: Single entry point for all plugin services
7. **Maintainability**: Centralized service management

### Migration Guide

To migrate existing plugins:

1. Update PluginBase to use LocalPluginContext pattern (see Plugin Structure section above)
2. Change ExecutePlugin signature to accept `LocalPluginContext` instead of individual services
3. Access services through `localContext.OrganizationService`, `localContext.Trace()`, etc.
4. Update all plugin implementations to use the new signature

---

## 🧪 Testing Standards

### Unit Test Structure

```csharp
[TestClass]
public class AccountCreatePluginTests
{
    [TestMethod]
    public void ExecutePlugin_WithValidAccount_Succeeds()
    {
        // Arrange
        var context = CreatePluginContext("Create", "account");
        var account = new Account { Name = "Test Account" };
        context.InputParameters["Target"] = account.ToEntity<Entity>();
        
        var plugin = new AccountCreatePlugin(null, null);

        // Act
        plugin.Execute(context);

        // Assert
        // Verify expected behavior
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidPluginExecutionException))]
    public void ExecutePlugin_WithEmptyName_ThrowsException()
    {
        // Arrange
        var context = CreatePluginContext("Create", "account");
        var account = new Account { Name = "" };
        context.InputParameters["Target"] = account.ToEntity<Entity>();
        
        var plugin = new AccountCreatePlugin(null, null);

        // Act
        plugin.Execute(context); // Should throw
    }
}
```

---

## 🛠️ Tools and Enforcement

### EditorConfig

The repository includes `.editorconfig` to enforce:
- 4 spaces for indentation
- LF line endings
- UTF-8 encoding
- Trim trailing whitespace

### Static Analysis

- **StyleCop**: Enforces naming and documentation
- **FxCop**: Detects security and performance issues
- **SonarQube**: Code quality and code smells (optional)

---

## 📚 Additional Resources

- [Microsoft Dataverse Plugin Development](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/plug-ins)
- [Microsoft C# Coding Conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [Plugin Best Practices](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/best-practices/business-logic/)
- [Understanding the Data Context](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/understand-the-data-context)
- [IPluginExecutionContext Interface](https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.ipluginexecutioncontext)

---

**Questions? Open a GitHub Discussion or contact the Platform team.**
