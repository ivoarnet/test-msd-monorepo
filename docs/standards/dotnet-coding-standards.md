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

All plugins should inherit from a base plugin class:

```csharp
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
        var tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
        var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

        try
        {
            tracingService.Trace($"Executing {GetType().Name}");
            ExecutePlugin(serviceProvider, tracingService, context);
        }
        catch (Exception ex)
        {
            tracingService.Trace($"Exception: {ex.Message}");
            throw new InvalidPluginExecutionException($"Error in {GetType().Name}: {ex.Message}", ex);
        }
    }

    protected abstract void ExecutePlugin(
        IServiceProvider serviceProvider,
        ITracingService tracingService,
        IPluginExecutionContext context);
}
```

### Service Locator Pattern

Extract service retrieval into helper:

```csharp
public class ServiceProvider
{
    private readonly IServiceProvider _serviceProvider;

    public ServiceProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IOrganizationService GetOrganizationService(Guid? userId = null)
    {
        var serviceFactory = (IOrganizationServiceFactory)_serviceProvider.GetService(typeof(IOrganizationServiceFactory));
        return serviceFactory.CreateOrganizationService(userId);
    }

    public ITracingService GetTracingService()
    {
        return (ITracingService)_serviceProvider.GetService(typeof(ITracingService));
    }
}
```

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
// ✅ Good
public class AccountCreatePlugin : PluginBase
{
    protected override void ExecutePlugin(IServiceProvider serviceProvider, ITracingService tracingService, IPluginExecutionContext context)
    {
        var account = GetTargetEntity<Account>(context);
        ValidateAccountName(account);
    }

    private void ValidateAccountName(Account account)
    {
        if (string.IsNullOrWhiteSpace(account.Name))
        {
            throw new InvalidPluginExecutionException("Account name is required.");
        }
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
protected override void ExecutePlugin(IServiceProvider serviceProvider, ITracingService tracingService, IPluginExecutionContext context)
{
    // Validate context
    if (context.InputParameters == null || !context.InputParameters.Contains("Target"))
    {
        throw new InvalidPluginExecutionException("Target parameter is missing.");
    }

    // Validate message name
    if (context.MessageName != "Create")
    {
        return; // Exit early if wrong message
    }

    // Validate stage
    if (context.Stage != 20) // Pre-operation
    {
        return;
    }

    // Validate entity
    var target = context.InputParameters["Target"] as Entity;
    if (target == null || target.LogicalName != "account")
    {
        return;
    }

    // Now safe to proceed
    ValidateAccount(target);
}
```

### 2. Exception Handling

```csharp
// ✅ Good - specific exceptions with context
try
{
    var service = serviceFactory.CreateOrganizationService(context.UserId);
    service.Update(entity);
}
catch (FaultException<OrganizationServiceFault> ex)
{
    tracingService.Trace($"Dataverse error: {ex.Detail.Message}");
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
tracingService.Trace("Starting account validation");
tracingService.Trace($"Account ID: {account.Id}, Name: {account.Name}");

if (account.Name.Length > 100)
{
    tracingService.Trace("Account name exceeds max length");
    throw new InvalidPluginExecutionException("Account name must be 100 characters or less.");
}

tracingService.Trace("Account validation passed");
```

**⚠️ Never trace sensitive data**: Passwords, API keys, credit card numbers, etc.

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
var userService = serviceFactory.CreateOrganizationService(context.UserId);

// System context (bypasses security - use cautiously)
var systemService = serviceFactory.CreateOrganizationService(null);
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

- [ ] Plugin inherits from `PluginBase`
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

---

**Questions? Open a GitHub Discussion or contact the Platform team.**
