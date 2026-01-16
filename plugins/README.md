# Dataverse Plugins

.NET server-side business logic for Microsoft Dataverse.

---

## 📋 Overview

This folder contains **Plugins** - event-driven business logic that runs automatically when data changes in Dataverse.

> **Note**: For creating **Custom APIs** (reusable operations exposed via API), see [/plugins/custom-apis/README.md](/plugins/custom-apis/README.md)

### Plugins vs Custom APIs

| Aspect | Plugins | Custom APIs |
|--------|---------|-------------|
| **Trigger** | Automatic (on data events) | Explicit call |
| **Use Case** | Data validation, automation | Reusable operations |
| **Invocation** | Dataverse pipeline | API call from anywhere |
| **Examples** | Validate fields, cascade updates | Calculate commission, generate report |
| **Documentation** | This README | [Custom APIs README](/plugins/custom-apis/README.md) |

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

        protected override void ExecutePlugin(
            IServiceProvider serviceProvider,
            ITracingService tracingService,
            IPluginExecutionContext context)
        {
            tracingService.Trace("AccountCreatePlugin: Starting execution");

            // Validate context
            if (!context.InputParameters.Contains("Target") ||
                !(context.InputParameters["Target"] is Entity entity))
            {
                tracingService.Trace("Target entity not found");
                return;
            }

            if (entity.LogicalName != "account")
            {
                return;
            }

            tracingService.Trace($"Processing account: {entity.Id}");

            // Business logic here
            ValidateAccountName(entity, tracingService);
            SetDefaultValues(entity, tracingService);

            tracingService.Trace("AccountCreatePlugin: Completed successfully");
        }

        private void ValidateAccountName(Entity account, ITracingService tracingService)
        {
            if (!account.Contains("name") || string.IsNullOrWhiteSpace(account["name"].ToString()))
            {
                throw new InvalidPluginExecutionException("Account name is required.");
            }

            tracingService.Trace("Account name validation passed");
        }

        private void SetDefaultValues(Entity account, ITracingService tracingService)
        {
            if (!account.Contains("accounttype"))
            {
                account["accounttype"] = new OptionSetValue(1); // Default to customer
                tracingService.Trace("Set default account type");
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

All plugins inherit from `PluginBase`:

```csharp
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

### Querying Data

```csharp
// Get organization service
var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
var service = serviceFactory.CreateOrganizationService(context.UserId);

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
```

### Updating Related Records

```csharp
foreach (var contact in contacts)
{
    contact["accountstatus"] = new OptionSetValue(2); // Update status
    service.Update(contact);
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
- Inherit from `PluginBase`
- Validate all inputs
- Use tracing extensively
- Query only needed columns
- Handle exceptions gracefully
- Write unit tests

❌ **Don't**:
- Log sensitive data (passwords, PII)
- Use `ColumnSet(true)` (retrieve all columns)
- Make synchronous web requests (use async steps)
- Hard-code environment URLs
- Catch exceptions without re-throwing
- Use late-bound entities without validation

---

## 🔗 Resources

- [Plugin Development Guide](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/plug-ins)
- [Plugin Registration Tool](https://www.nuget.org/packages/Microsoft.CrmSdk.XrmTooling.PluginRegistrationTool)
- [Debugging Plugins](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/tutorial-debug-plug-in)
- [Coding Standards](/docs/standards/dotnet-coding-standards.md)

---

**Questions? Contact the Platform team or open a [GitHub Discussion](https://github.com/ivoarnet/test-msd-monorepo/discussions).**
