# Dataverse Plugins

.NET server-side business logic for Microsoft Dataverse.

---

## 📋 Overview

This folder contains:
- **Plugins**: Event-driven business logic (Create, Update, Delete, etc.)
- **Custom APIs**: Custom operations exposed via Dataverse API
- **Shared utilities**: Common code reused across plugins

---

## 🏗️ Structure

```
plugins/
├── src/
│   ├── Contoso.Plugins/           # Main plugin project
│   │   ├── Account/                # Account entity plugins
│   │   ├── Contact/                # Contact entity plugins
│   │   ├── Shared/                 # Shared base classes and helpers
│   │   └── Contoso.Plugins.csproj
│   └── Contoso.CustomApis/         # Custom API implementations
│       └── Contoso.CustomApis.csproj
├── tests/
│   └── Contoso.Plugins.Tests/
│       └── Contoso.Plugins.Tests.csproj
├── Contoso.Plugins.sln
└── README.md
```

---

## 🚀 Getting Started

### Prerequisites

- .NET SDK 8.0 or higher
- Visual Studio 2022 (recommended) or VS Code
- Dataverse environment for testing

### Build

```bash
cd plugins
dotnet restore
dotnet build
```

### Run Tests

```bash
dotnet test
```

### Create a Strong Name Key (First Time Only)

Plugins must be strong-named:

```bash
cd src/Contoso.Plugins
sn -k ContosoPlugins.snk
```

Add to `.csproj`:
```xml
<PropertyGroup>
  <SignAssembly>true</SignAssembly>
  <AssemblyOriginatorKeyFile>ContosoPlugins.snk</AssemblyOriginatorKeyFile>
</PropertyGroup>
```

---

## 📝 Creating a New Plugin

### 1. Create Plugin Class

Create a new file in the appropriate entity folder:

```csharp
// src/Contoso.Plugins/Account/AccountCreatePlugin.cs
using Microsoft.Xrm.Sdk;
using Contoso.Plugins.Shared;
using System;

namespace Contoso.Plugins.Account
{
    /// <summary>
    /// Plugin that executes when an Account is created.
    /// Validates business rules and sets default values.
    /// </summary>
    public class AccountCreatePlugin : PluginBase
    {
        public AccountCreatePlugin(string unsecureConfig, string secureConfig)
            : base(unsecureConfig, secureConfig)
        {
        }

        protected override void ExecutePlugin(LocalPluginContext localContext)
        {
            localContext.Trace("AccountCreatePlugin: Starting execution");
            
            var context = localContext.PluginExecutionContext;

            // Validate context
            if (!context.InputParameters.Contains("Target") ||
                !(context.InputParameters["Target"] is Entity entity))
            {
                localContext.Trace("Target entity not found");
                return;
            }

            if (entity.LogicalName != "account")
            {
                localContext.Trace($"Unexpected entity type: {entity.LogicalName}");
                return;
            }

            localContext.Trace($"Processing account: {entity.Id}");

            // Business logic here
            ValidateAccountName(entity, localContext);
            SetDefaultValues(entity, localContext);

            localContext.Trace("AccountCreatePlugin: Completed successfully");
        }

        private void ValidateAccountName(Entity account, LocalPluginContext localContext)
        {
            if (!account.Contains("name") || string.IsNullOrWhiteSpace(account["name"].ToString()))
            {
                localContext.Trace("Account name validation failed");
                throw new InvalidPluginExecutionException("Account name is required.");
            }

            localContext.Trace("Account name validation passed");
        }

        private void SetDefaultValues(Entity account, LocalPluginContext localContext)
        {
            if (!account.Contains("accounttype"))
            {
                account["accounttype"] = new OptionSetValue(1); // Default to customer
                localContext.Trace("Set default account type to 1 (Customer)");
            }
        }
    }
}
```

### 2. Register Plugin

Use the Plugin Registration Tool:

1. Connect to your Dataverse environment
2. Register new assembly → Select built DLL
3. Register new step:
   - **Message**: Create
   - **Primary Entity**: account
   - **Stage**: Pre-operation (20) or Post-operation (40)
   - **Execution Mode**: Synchronous

### 3. Test Plugin

- Create an account in Dataverse
- Check plugin trace logs
- Verify expected behavior

---

## 🎯 Common Patterns

### Base Plugin Class

All plugins inherit from `PluginBase` using the modern LocalPluginContext pattern:

```csharp
// See /docs/standards/dotnet-coding-standards.md for the full implementation
public abstract class PluginBase : IPlugin
{
    protected string UnsecureConfig { get; }
    protected string SecureConfig { get; }

    protected PluginBase(string unsecureConfig, string secureConfig)
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

        var localContext = new LocalPluginContext(serviceProvider);

        try
        {
            localContext.Trace($"Executing {GetType().Name}");
            
            // Log context summary for better debugging
            if (localContext.PluginExecutionContext is IPluginExecutionContext4 context4)
            {
                localContext.Trace($"Context Summary: {context4.ContextSummary}");
            }
            
            ExecutePlugin(localContext);
        }
        catch (InvalidPluginExecutionException)
        {
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
```

**Note**: The `LocalPluginContext` class provides clean access to plugin services and is defined in the shared utilities. See [.NET Coding Standards](/docs/standards/dotnet-coding-standards.md) for the complete implementation.

### Querying Data

```csharp
// Get organization service from LocalPluginContext
var service = localContext.OrganizationService;

// Query using QueryExpression
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
localContext.Trace($"Retrieved {contacts.Count} contacts");
```

### Updating Related Records

```csharp
var service = localContext.OrganizationService;

foreach (var contact in contacts)
{
    contact["accountstatus"] = new OptionSetValue(2); // Update status
    service.Update(contact);
    localContext.Trace($"Updated contact: {contact.Id}");
}
```

---

## 🧪 Testing

### Unit Test Example

```csharp
[TestClass]
public class AccountCreatePluginTests
{
    [TestMethod]
    public void ExecutePlugin_WithValidAccount_Succeeds()
    {
        // Arrange
        var serviceProvider = new FakeServiceProvider();
        var context = new FakePluginExecutionContext
        {
            MessageName = "Create",
            Stage = 20,
            InputParameters = new ParameterCollection
            {
                { "Target", new Entity("account") { ["name"] = "Test Account" } }
            }
        };
        
        serviceProvider.SetService(context);
        var plugin = new AccountCreatePlugin(null, null);

        // Act
        plugin.Execute(serviceProvider);

        // Assert
        Assert.IsTrue(context.InputParameters["Target"] is Entity);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidPluginExecutionException))]
    public void ExecutePlugin_WithEmptyName_ThrowsException()
    {
        // Arrange
        var serviceProvider = new FakeServiceProvider();
        var context = new FakePluginExecutionContext
        {
            MessageName = "Create",
            Stage = 20,
            InputParameters = new ParameterCollection
            {
                { "Target", new Entity("account") { ["name"] = "" } }
            }
        };
        
        serviceProvider.SetService(context);
        var plugin = new AccountCreatePlugin(null, null);

        // Act
        plugin.Execute(serviceProvider); // Should throw
    }
}
```

Use [FakeXrmEasy](https://github.com/DynamicsValue/fake-xrm-easy) for mocking Dataverse services.

---

## 🔧 Configuration

### Assembly Attributes

Update `AssemblyInfo.cs`:

```csharp
[assembly: AssemblyTitle("Contoso Dynamics 365 Plugins")]
[assembly: AssemblyDescription("Business logic plugins for Dataverse")]
[assembly: AssemblyCompany("Contoso Ltd.")]
[assembly: AssemblyProduct("Contoso.Plugins")]
[assembly: AssemblyCopyright("Copyright © 2024")]
[assembly: AssemblyVersion("1.0.0.0")]
```

---

## 📦 Deployment

### Manual Deployment

1. Build in Release mode: `dotnet build -c Release`
2. Locate DLL in `bin/Release/net462/`
3. Use Plugin Registration Tool to update assembly

### CI/CD Deployment

See [plugins-ci.yml](/.github/workflows/plugins-ci.yml) for automated deployment.

---

## 📚 Best Practices

✅ **Do**:
- Inherit from `PluginBase` using LocalPluginContext pattern
- Validate all inputs
- Use tracing extensively
- Query only needed columns
- Handle exceptions gracefully
- Write unit tests
- Use `IPluginExecutionContext4.ContextSummary` for better debugging

❌ **Don't**:
- Log sensitive data (passwords, PII)
- Use `ColumnSet(true)` (retrieve all columns)
- Make synchronous web requests (use async steps)
- Hard-code environment URLs
- Catch exceptions without re-throwing
- Use late-bound entities without validation
- Directly call `GetService(typeof(...))` multiple times - use LocalPluginContext instead

---

## ⚠️ Deprecated Patterns

### Old Pattern (Deprecated)

The following pattern is outdated but still functional:

```csharp
public void Execute(IServiceProvider serviceProvider)
{
    // ❌ Deprecated: Direct GetService calls
    var tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
    var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
    var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
    var service = serviceFactory.CreateOrganizationService(context.UserId);
}
```

### Modern Pattern (Recommended)

Use the LocalPluginContext pattern instead:

```csharp
public void Execute(IServiceProvider serviceProvider)
{
    // ✅ Modern: Use LocalPluginContext
    var localContext = new LocalPluginContext(serviceProvider);
    
    // Clean access to all services
    var context = localContext.PluginExecutionContext;
    var service = localContext.OrganizationService;
    localContext.Trace("Message goes here");
    
    // Access to IPluginExecutionContext4 features
    if (context is IPluginExecutionContext4 context4)
    {
        localContext.Trace($"Context: {context4.ContextSummary}");
    }
}
```

### Benefits of Modern Pattern

1. **Better encapsulation**: Services are cached and accessed through properties
2. **Cleaner code**: No repeated `GetService(typeof(...))` calls
3. **Enhanced debugging**: Easy access to `IPluginExecutionContext4.ContextSummary`
4. **Testability**: LocalPluginContext can be mocked for unit tests
5. **Maintainability**: Centralized service management

---

## 🔗 Resources

- [Plugin Development Guide](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/plug-ins)
- [Plugin Registration Tool](https://www.nuget.org/packages/Microsoft.CrmSdk.XrmTooling.PluginRegistrationTool)
- [Debugging Plugins](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/tutorial-debug-plug-in)
- [Coding Standards](/docs/standards/dotnet-coding-standards.md)
- [Evolution of Plugin Context](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/understand-the-data-context)

---

**Questions? Contact the Platform team or open a [GitHub Discussion](https://github.com/ivoarnet/test-msd-monorepo/discussions).**
