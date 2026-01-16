# Migration Guide: Deprecated Patterns to Modern Approaches

This guide helps developers migrate from deprecated Dynamics 365 / Dataverse patterns to modern, recommended approaches.

---

## 📋 Table of Contents

- [Plugin Patterns](#plugin-patterns)
- [Client Script Patterns](#client-script-patterns)
- [Why Migrate?](#why-migrate)
- [Migration Timeline](#migration-timeline)

---

## 🔌 Plugin Patterns

### Overview

Microsoft has evolved the plugin execution context over multiple releases, introducing `IPluginExecutionContext4` with the `ContextSummary` property for enhanced debugging. The recommended pattern now uses a LocalPluginContext wrapper for cleaner service access.

### Deprecated: Direct Service Calls

**Old Pattern** (Still works but not recommended):

```csharp
public class AccountCreatePlugin : IPlugin
{
    public void Execute(IServiceProvider serviceProvider)
    {
        // ❌ Deprecated: Multiple GetService calls scattered through code
        var tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
        var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
        var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
        var orgService = serviceFactory.CreateOrganizationService(context.UserId);

        try
        {
            tracingService.Trace("Starting execution");
            
            // Business logic
            if (!context.InputParameters.Contains("Target"))
            {
                return;
            }
            
            var entity = (Entity)context.InputParameters["Target"];
            // ... more logic
        }
        catch (Exception ex)
        {
            tracingService.Trace($"Error: {ex.Message}");
            throw new InvalidPluginExecutionException(ex.Message, ex);
        }
    }
}
```

### Modern: LocalPluginContext Pattern

**New Pattern** (Recommended):

```csharp
/// <summary>
/// Base plugin class using LocalPluginContext pattern.
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

        // ✅ Modern: Use LocalPluginContext wrapper
        var localContext = new LocalPluginContext(serviceProvider);

        try
        {
            localContext.Trace($"Executing {GetType().Name}");
            
            // Enhanced debugging with IPluginExecutionContext4
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

/// <summary>
/// LocalPluginContext encapsulates plugin services.
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

    public IOrganizationService OrganizationService => OrganizationServiceFactory.CreateOrganizationService(PluginExecutionContext.UserId);

    public IOrganizationService SystemOrganizationService => OrganizationServiceFactory.CreateOrganizationService(null);

    public void Trace(string message)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            TracingService?.Trace($"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} - {message}");
        }
    }
}

/// <summary>
/// Example plugin implementation using modern pattern.
/// </summary>
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

        // Validate input
        if (!context.InputParameters.Contains("Target") ||
            !(context.InputParameters["Target"] is Entity entity))
        {
            localContext.Trace("Target parameter missing or invalid");
            return;
        }

        if (entity.LogicalName != "account")
        {
            localContext.Trace($"Unexpected entity type: {entity.LogicalName}");
            return;
        }

        // Business logic with clean service access
        var service = localContext.OrganizationService;
        
        // Example: Validate account name
        if (!entity.Contains("name") || string.IsNullOrWhiteSpace(entity["name"]?.ToString()))
        {
            throw new InvalidPluginExecutionException("Account name is required.");
        }

        localContext.Trace("Account validation completed successfully");
    }
}
```

### Migration Steps for Plugins

1. **Create LocalPluginContext class** in your shared utilities folder
2. **Update PluginBase class** to use LocalPluginContext pattern
3. **Change ExecutePlugin signature** from:
   ```csharp
   protected abstract void ExecutePlugin(
       IServiceProvider serviceProvider,
       ITracingService tracingService,
       IPluginExecutionContext context);
   ```
   To:
   ```csharp
   protected abstract void ExecutePlugin(LocalPluginContext localContext);
   ```
4. **Update existing plugins** to use localContext:
   - Replace `tracingService.Trace()` with `localContext.Trace()`
   - Replace direct service retrieval with `localContext.OrganizationService`
   - Access context via `localContext.PluginExecutionContext`
5. **Test thoroughly** - ensure all plugins work correctly after migration
6. **Update documentation** in plugin XML comments if needed

---

## 🖥️ Client Script Patterns

### Deprecated: Xrm.Page

**Old Pattern** (Deprecated since Dynamics 365 v9.0):

```typescript
// ❌ Deprecated: Direct Xrm.Page access
function onLoad() {
    var name = Xrm.Page.getAttribute("name");
    name.setValue("New Value");
    
    Xrm.Page.ui.setFormNotification("Welcome", "INFO", "welcome");
}

function onChange() {
    var accountType = Xrm.Page.getAttribute("accounttype").getValue();
    // Business logic
}
```

### Modern: ExecutionContext and FormContext

**New Pattern** (Recommended):

```typescript
// ✅ Modern: Get formContext from executionContext
export function onLoad(executionContext: Xrm.Events.EventContext): void {
    const formContext = executionContext.getFormContext();
    
    const name = formContext.getAttribute("name");
    if (name) {
        name.setValue("New Value");
    }
    
    formContext.ui.setFormNotification("Welcome", "INFO", "welcome");
}

export function onChange(executionContext: Xrm.Events.EventContext): void {
    const formContext = executionContext.getFormContext();
    
    const accountType = formContext.getAttribute<Xrm.Attributes.OptionSetAttribute>("accounttype");
    if (accountType) {
        const value = accountType.getValue();
        // Business logic with null checking
    }
}
```

### Deprecated: window.parent.Xrm

**Old Pattern** (Not recommended):

```typescript
// ❌ Not recommended: Parent window access
var xrm = window.parent.Xrm;
var formContext = xrm.Page;
```

**New Pattern** (Recommended):

```typescript
// ✅ Modern: Use executionContext parameter
export function onLoad(executionContext: Xrm.Events.EventContext): void {
    const formContext = executionContext.getFormContext();
    // Use formContext methods
}
```

### Deprecated: alert(), confirm(), prompt()

**Old Pattern** (Deprecated):

```typescript
// ❌ Deprecated: Browser dialogs
alert("Record saved!");
if (confirm("Delete this record?")) {
    // Delete logic
}
var input = prompt("Enter value:");
```

**New Pattern** (Recommended):

```typescript
// ✅ Modern: Xrm.Navigation dialogs
await Xrm.Navigation.openAlertDialog({
    text: "Record saved!",
    title: "Success"
});

const result = await Xrm.Navigation.openConfirmDialog({
    text: "Delete this record?",
    title: "Confirm Delete"
});

if (result.confirmed) {
    // Delete logic
}

const inputResult = await Xrm.Navigation.openInputDialog({
    text: "Enter value:",
    title: "Input Required"
});

if (inputResult.confirmed) {
    const value = inputResult.value;
    // Use value
}
```

### Deprecated: Synchronous AJAX

**Old Pattern** (Deprecated):

```typescript
// ❌ Deprecated: Synchronous XMLHttpRequest
var xhr = new XMLHttpRequest();
xhr.open("GET", url, false); // false = synchronous
xhr.send();
var data = JSON.parse(xhr.responseText);
```

**New Pattern** (Recommended):

```typescript
// ✅ Modern: async/await with Xrm.WebApi
async function getData(entityId: string): Promise<any> {
    try {
        const result = await Xrm.WebApi.retrieveRecord(
            "account",
            entityId,
            "?$select=name,revenue,industrycode"
        );
        return result;
    } catch (error) {
        console.error("Error retrieving data:", error);
        throw error;
    }
}

// Usage
export async function onLoad(executionContext: Xrm.Events.EventContext): Promise<void> {
    const formContext = executionContext.getFormContext();
    const accountId = formContext.data.entity.getId();
    
    if (accountId) {
        try {
            const data = await getData(accountId);
            // Use data
        } catch (error) {
            await Xrm.Navigation.openErrorDialog({
                message: "Failed to load data"
            });
        }
    }
}
```

### Migration Steps for Client Scripts

1. **Update function signatures** to accept `executionContext` parameter
2. **Add TypeScript types** for better type safety
3. **Replace Xrm.Page** with `executionContext.getFormContext()`
4. **Replace browser dialogs** with Xrm.Navigation methods
5. **Convert synchronous AJAX** to async/await with Xrm.WebApi
6. **Add null/undefined checks** for all attribute and control access
7. **Update event registrations** in Dynamics 365 to pass execution context
8. **Test all form events** thoroughly

---

## 💡 Why Migrate?

### Benefits of Modern Patterns

#### Plugins:
- **Better testability**: LocalPluginContext can be mocked easily
- **Improved debugging**: Access to `IPluginExecutionContext4.ContextSummary`
- **Cleaner code**: Centralized service access
- **Performance**: Lazy loading and caching of services
- **Maintainability**: Single source of truth for plugin services

#### Client Scripts:
- **Future-proof**: Microsoft actively maintains the modern APIs
- **Cross-browser compatibility**: Xrm API works consistently
- **Type safety**: Full TypeScript support with type definitions
- **Better error handling**: Modern APIs provide detailed error information
- **Consistency**: Aligned with Power Platform development patterns

### Risks of Not Migrating

1. **Deprecation warnings**: Future versions may show warnings or errors
2. **Security issues**: Deprecated patterns may have security vulnerabilities
3. **Performance problems**: Old patterns may not be optimized for modern browsers
4. **Maintenance burden**: Harder to onboard new developers who learn modern patterns
5. **Limited support**: Microsoft focuses support on modern approaches

---

## 📅 Migration Timeline

### Immediate (High Priority)

1. **New development**: Always use modern patterns for new plugins and client scripts
2. **Critical plugins**: Migrate high-volume or business-critical plugins first
3. **Public-facing forms**: Migrate client scripts on customer-facing forms

### Short-term (3-6 months)

1. **Active plugins**: Migrate plugins that are frequently modified
2. **Complex forms**: Migrate forms with extensive JavaScript customizations
3. **Update documentation**: Ensure all examples use modern patterns

### Long-term (6-12 months)

1. **Legacy plugins**: Migrate remaining plugins during maintenance windows
2. **Archive code**: Document old pattern examples as "legacy" if needed for reference
3. **Training**: Ensure all team members are trained on modern patterns

---

## 📚 Additional Resources

### Microsoft Documentation
- [Plugin Best Practices](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/best-practices/business-logic/)
- [Understanding the Data Context](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/understand-the-data-context)
- [Deprecated Client API](https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/clientapi/client-api-deprecated)
- [Client API Best Practices](https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/best-practices/)

### Internal Documentation
- [.NET Coding Standards](/docs/standards/dotnet-coding-standards.md)
- [TypeScript Coding Standards](/docs/standards/typescript-coding-standards.md)
- [Plugin README](/src/plugins/README.md)
- [Client Scripts README](/src/client-scripts/README.md)

---

## ❓ Questions or Issues?

If you encounter issues during migration:

1. **Check examples**: Review the coding standards documents for complete examples
2. **Ask the team**: Open a GitHub Discussion for help
3. **Report problems**: Create an issue if you find bugs or unexpected behavior
4. **Share learnings**: Update this guide with migration tips you discover

---

**Last Updated**: 2024
**Maintained By**: Platform Team
