# Dataverse Custom APIs

Create custom operations that can be called from the Dataverse Web API, plugins, workflows, and client code.

---

## 📋 Overview

**Custom APIs** provide a way to define custom business logic as reusable operations in Dataverse. Unlike plugins that react to data events, Custom APIs:

- Are **invoked explicitly** like regular API calls
- Can be called from **anywhere** (JavaScript, plugins, Power Automate, external apps)
- Have **strongly-typed inputs and outputs**
- Appear in the **API metadata** and can be discovered
- Support **both synchronous and asynchronous** execution

---

## 🎯 When to Use Custom APIs

| Use Custom API When... | Use Plugin When... |
|------------------------|-------------------|
| Creating reusable business logic | Reacting to data changes (Create, Update, Delete) |
| Building callable operations | Enforcing validation rules automatically |
| Exposing functionality to external systems | Implementing data-driven workflows |
| Need strongly-typed parameters | Need to run on specific entity events |
| Building integration endpoints | Need to run in the pipeline (Pre/Post operation) |

### Real-World Examples

✅ **Good Custom API Use Cases**:
- `CalculateCommission`: Calculate sales commission based on deals
- `GenerateInvoice`: Create invoice from order with complex logic
- `ValidateCreditCard`: Validate payment information
- `SyncExternalSystem`: Trigger data sync with external system
- `GenerateReport`: Create and return formatted report data

❌ **Bad Custom API Use Cases** (use plugins instead):
- Automatically populate fields when record created
- Prevent deletion if related records exist
- Cascade status changes to child records
- Audit data changes

---

## 🏗️ Structure

```
custom-apis/
├── src/
│   └── Contoso.CustomApis/           # Custom API implementations
│       ├── CalculateCommission/       # Example: Commission calculation
│       │   ├── CalculateCommissionApi.cs
│       │   ├── CalculateCommissionRequest.cs
│       │   └── CalculateCommissionResponse.cs
│       ├── Shared/                    # Shared base classes and helpers
│       │   ├── CustomApiBase.cs
│       │   └── ValidationHelper.cs
│       └── Contoso.CustomApis.csproj
├── tests/
│   └── Contoso.CustomApis.Tests/
│       ├── CalculateCommissionApiTests.cs
│       └── Contoso.CustomApis.Tests.csproj
├── examples/                          # Example implementations
│   ├── simple-custom-api.md
│   └── async-custom-api.md
└── README.md                          # This file
```

---

## 🚀 Getting Started

### Prerequisites

- .NET SDK 8.0 or higher
- Visual Studio 2022 or VS Code
- Dataverse environment with System Administrator role
- Plugin Registration Tool (for registration)

### Build

```bash
cd plugins/custom-apis
dotnet restore
dotnet build
```

### Run Tests

```bash
dotnet test
```

---

## 📝 Creating a Custom API

### Step 1: Define the Custom API in Dataverse

Custom APIs must first be defined in Dataverse using the Custom API table:

**Option A: Using Plugin Registration Tool**
1. Open Plugin Registration Tool
2. Select "Register" → "Register New Custom API"
3. Fill in details (see table below)

**Option B: Using Solution Explorer**
1. Navigate to your solution
2. Add New → More → Custom API
3. Fill in the form

**Option C: Programmatically**
```csharp
var customApi = new Entity("customapi")
{
    ["uniquename"] = "contoso_CalculateCommission",
    ["displayname"] = "Calculate Commission",
    ["description"] = "Calculates sales commission based on deal amount",
    ["bindingtype"] = new OptionSetValue(0), // 0 = Global, 1 = Entity, 2 = EntityCollection
    ["boundentitylogicalname"] = null, // Only if binding to entity
    ["executeprivilegename"] = null, // Optional: Privilege required to execute
    ["isfunction"] = false, // false = Action, true = Function
    ["isprivate"] = false, // false = Public API
    ["workflowsdkstepenabled"] = true, // Can be used in workflows
    ["iscustomizable"] = new BooleanManagedProperty(true)
};
service.Create(customApi);
```

**Key Properties**:

| Property | Description | Example |
|----------|-------------|---------|
| `uniquename` | API identifier (use publisher prefix) | `contoso_CalculateCommission` |
| `displayname` | Human-readable name | "Calculate Commission" |
| `bindingtype` | 0=Global, 1=Entity, 2=EntityCollection | 0 (Global) |
| `isfunction` | true=Function (GET, read-only), false=Action (POST) | false (Action) |
| `isprivate` | true=Internal only, false=Public API | false (Public) |

### Step 2: Define Request Parameters

Create Custom API Request Parameter records:

```csharp
var requestParam = new Entity("customapirequestparameter")
{
    ["customapiid"] = customApiId, // Reference to Custom API
    ["uniquename"] = "DealAmount",
    ["displayname"] = "Deal Amount",
    ["description"] = "Total deal amount in base currency",
    ["type"] = new OptionSetValue(10), // 10 = Decimal
    ["isoptional"] = false,
    ["logicalentityname"] = null // Only for Entity/EntityReference types
};
service.Create(requestParam);
```

**Common Parameter Types**:

| Type Code | Type | Example |
|-----------|------|---------|
| 0 | Boolean | true/false |
| 1 | DateTime | 2024-01-15T10:00:00Z |
| 2 | Decimal | 1234.56 |
| 3 | Entity | Account record |
| 4 | EntityCollection | List of Contact records |
| 5 | EntityReference | Lookup to Account |
| 6 | Float | 123.45 |
| 7 | Integer | 42 |
| 8 | Money | $1,000.00 |
| 9 | Picklist | Option set value |
| 10 | String | "Hello World" |
| 11 | StringArray | ["value1", "value2"] |
| 12 | Guid | 00000000-0000-0000-0000-000000000000 |

### Step 3: Define Response Properties

Create Custom API Response Property records:

```csharp
var responseProperty = new Entity("customapiresponseproperty")
{
    ["customapiid"] = customApiId,
    ["uniquename"] = "CommissionAmount",
    ["displayname"] = "Commission Amount",
    ["description"] = "Calculated commission amount",
    ["type"] = new OptionSetValue(8), // 8 = Money
    ["logicalentityname"] = null
};
service.Create(responseProperty);
```

### Step 4: Create the Implementation Class

Create a plugin that implements the Custom API:

```csharp
using Microsoft.Xrm.Sdk;
using System;

namespace Contoso.CustomApis.CalculateCommission
{
    /// <summary>
    /// Implements the contoso_CalculateCommission Custom API.
    /// Calculates sales commission based on deal amount and rep tier.
    /// </summary>
    public class CalculateCommissionApi : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            // Get services
            var tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var service = serviceFactory.CreateOrganizationService(context.UserId);

            tracingService.Trace("CalculateCommissionApi: Starting execution");

            try
            {
                // Extract input parameters
                var dealAmount = (decimal)context.InputParameters["DealAmount"];
                var salesRepId = (EntityReference)context.InputParameters["SalesRep"];
                
                tracingService.Trace($"DealAmount: {dealAmount}, SalesRep: {salesRepId.Id}");

                // Business logic
                decimal commissionRate = GetCommissionRate(service, salesRepId, tracingService);
                decimal commissionAmount = dealAmount * commissionRate;

                tracingService.Trace($"Commission calculated: {commissionAmount}");

                // Set output parameters
                context.OutputParameters["CommissionAmount"] = new Money(commissionAmount);
                context.OutputParameters["CommissionRate"] = commissionRate;

                tracingService.Trace("CalculateCommissionApi: Completed successfully");
            }
            catch (Exception ex)
            {
                tracingService.Trace($"Error: {ex.Message}");
                throw new InvalidPluginExecutionException($"Failed to calculate commission: {ex.Message}", ex);
            }
        }

        private decimal GetCommissionRate(IOrganizationService service, EntityReference salesRepId, ITracingService tracingService)
        {
            // Retrieve sales rep record
            var salesRep = service.Retrieve("systemuser", salesRepId.Id, new ColumnSet("contoso_tier"));
            
            // Determine rate based on tier
            if (salesRep.Contains("contoso_tier"))
            {
                var tier = salesRep.GetAttributeValue<OptionSetValue>("contoso_tier").Value;
                switch (tier)
                {
                    case 1: return 0.15m; // Platinum: 15%
                    case 2: return 0.10m; // Gold: 10%
                    case 3: return 0.05m; // Silver: 5%
                    default: return 0.03m; // Bronze: 3%
                }
            }

            return 0.03m; // Default
        }
    }
}
```

### Step 5: Register the Plugin

Use Plugin Registration Tool:

1. **Register Assembly**: Upload your DLL
2. **Register Step**:
   - **Message**: Select your Custom API unique name (e.g., `contoso_CalculateCommission`)
   - **Primary Entity**: None (for global APIs)
   - **Stage**: PostOperation (40)
   - **Execution Mode**: Synchronous

### Step 6: Call the Custom API

**From JavaScript (Client-side)**:
```javascript
// Using Xrm.WebApi
var request = {
    DealAmount: 50000.00,
    SalesRep: {
        "@odata.type": "Microsoft.Dynamics.CRM.systemuser",
        systemuserid: "00000000-0000-0000-0000-000000000000"
    },
    
    getMetadata: function() {
        return {
            boundParameter: null,
            parameterTypes: {
                "DealAmount": { typeName: "Edm.Decimal", structuralProperty: 1 },
                "SalesRep": { typeName: "mscrm.systemuser", structuralProperty: 5 }
            },
            operationType: 0, // 0 = Action, 1 = Function
            operationName: "contoso_CalculateCommission"
        };
    }
};

Xrm.WebApi.online.execute(request).then(
    function(response) {
        if (response.ok) {
            return response.json();
        }
    }
).then(function(result) {
    console.log("Commission: " + result.CommissionAmount);
    console.log("Rate: " + result.CommissionRate);
});
```

**From C# Plugin**:
```csharp
var request = new OrganizationRequest("contoso_CalculateCommission")
{
    ["DealAmount"] = 50000.00m,
    ["SalesRep"] = new EntityReference("systemuser", userId)
};

var response = service.Execute(request);
var commissionAmount = (Money)response["CommissionAmount"];
var commissionRate = (decimal)response["CommissionRate"];
```

**From Web API (REST)**:
```http
POST [Organization URI]/api/data/v9.2/contoso_CalculateCommission
Content-Type: application/json

{
  "DealAmount": 50000.00,
  "SalesRep@odata.bind": "/systemusers(00000000-0000-0000-0000-000000000000)"
}
```

**From Power Automate**:
- Add "Perform an unbound action" step
- Select your Custom API from the dropdown
- Fill in parameters

---

## 🎯 Common Patterns

### Base Custom API Class

Create a base class for consistent error handling and logging:

```csharp
public abstract class CustomApiBase : IPlugin
{
    public void Execute(IServiceProvider serviceProvider)
    {
        var tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
        var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

        try
        {
            tracingService.Trace($"Executing Custom API: {context.MessageName}");
            ExecuteCustomApi(serviceProvider, tracingService, context);
            tracingService.Trace($"Custom API {context.MessageName} completed successfully");
        }
        catch (Exception ex)
        {
            tracingService.Trace($"Error in {context.MessageName}: {ex.Message}");
            throw new InvalidPluginExecutionException(
                $"An error occurred in {context.MessageName}: {ex.Message}", ex);
        }
    }

    protected abstract void ExecuteCustomApi(
        IServiceProvider serviceProvider,
        ITracingService tracingService,
        IPluginExecutionContext context);

    protected IOrganizationService GetOrganizationService(IServiceProvider serviceProvider, IPluginExecutionContext context)
    {
        var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
        return serviceFactory.CreateOrganizationService(context.UserId);
    }
}
```

### Request/Response Wrapper Classes

For better maintainability, create typed request/response classes:

```csharp
// Request class
public class CalculateCommissionRequest
{
    public decimal DealAmount { get; set; }
    public EntityReference SalesRep { get; set; }

    public static CalculateCommissionRequest FromContext(IPluginExecutionContext context)
    {
        return new CalculateCommissionRequest
        {
            DealAmount = (decimal)context.InputParameters["DealAmount"],
            SalesRep = (EntityReference)context.InputParameters["SalesRep"]
        };
    }
}

// Response class
public class CalculateCommissionResponse
{
    public Money CommissionAmount { get; set; }
    public decimal CommissionRate { get; set; }

    public void ToContext(IPluginExecutionContext context)
    {
        context.OutputParameters["CommissionAmount"] = CommissionAmount;
        context.OutputParameters["CommissionRate"] = CommissionRate;
    }
}

// Usage in Custom API
public class CalculateCommissionApi : CustomApiBase
{
    protected override void ExecuteCustomApi(
        IServiceProvider serviceProvider,
        ITracingService tracingService,
        IPluginExecutionContext context)
    {
        var request = CalculateCommissionRequest.FromContext(context);
        var service = GetOrganizationService(serviceProvider, context);

        var response = new CalculateCommissionResponse
        {
            CommissionAmount = new Money(request.DealAmount * 0.1m),
            CommissionRate = 0.1m
        };

        response.ToContext(context);
    }
}
```

### Entity-Bound Custom API

For operations specific to an entity:

```csharp
// Custom API definition
bindingtype = 1 (Entity)
boundentitylogicalname = "account"

// Implementation
public class AccountCalculateLifetimeValueApi : CustomApiBase
{
    protected override void ExecuteCustomApi(
        IServiceProvider serviceProvider,
        ITracingService tracingService,
        IPluginExecutionContext context)
    {
        // Get the bound entity (account)
        var accountRef = (EntityReference)context.InputParameters["Target"];
        var service = GetOrganizationService(serviceProvider, context);

        // Business logic using the account
        var lifetimeValue = CalculateLifetimeValue(service, accountRef.Id);

        context.OutputParameters["LifetimeValue"] = new Money(lifetimeValue);
    }
}
```

**Called from JavaScript**:
```javascript
// Entity-bound calls include the entity ID in the URL
var accountId = "00000000-0000-0000-0000-000000000000";

Xrm.WebApi.online.execute({
    entity: { entityType: "account", id: accountId },
    
    getMetadata: function() {
        return {
            boundParameter: "entity",
            parameterTypes: {
                "entity": { typeName: "mscrm.account", structuralProperty: 5 }
            },
            operationType: 0,
            operationName: "contoso_CalculateLifetimeValue"
        };
    }
});
```

---

## 🧪 Testing

### Unit Test Example

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using System;

namespace Contoso.CustomApis.Tests
{
    [TestClass]
    public class CalculateCommissionApiTests
    {
        [TestMethod]
        public void Execute_WithValidInputs_CalculatesCommission()
        {
            // Arrange
            var serviceProvider = CreateServiceProvider();
            var context = CreateExecutionContext();
            
            context.InputParameters["DealAmount"] = 50000.00m;
            context.InputParameters["SalesRep"] = new EntityReference("systemuser", Guid.NewGuid());

            var api = new CalculateCommissionApi();

            // Act
            api.Execute(serviceProvider);

            // Assert
            Assert.IsTrue(context.OutputParameters.Contains("CommissionAmount"));
            var commission = (Money)context.OutputParameters["CommissionAmount"];
            Assert.IsTrue(commission.Value > 0);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidPluginExecutionException))]
        public void Execute_WithMissingParameter_ThrowsException()
        {
            // Arrange
            var serviceProvider = CreateServiceProvider();
            var context = CreateExecutionContext();
            // Missing DealAmount parameter

            var api = new CalculateCommissionApi();

            // Act
            api.Execute(serviceProvider); // Should throw
        }
    }
}
```

Use [FakeXrmEasy](https://github.com/DynamicsValue/fake-xrm-easy) for comprehensive mocking.

---

## 📦 Deployment

### Manual Deployment

1. Build in Release mode:
   ```bash
   dotnet build -c Release
   ```

2. Use Plugin Registration Tool:
   - Update assembly
   - Verify Custom API registration
   - Test with Plugin Profiler

### CI/CD Deployment

Custom APIs deploy the same as plugins. See [plugins-ci.yml](/.github/workflows/plugins-ci.yml).

**Pipeline Steps**:
1. Build assembly
2. Run unit tests
3. Create solution (includes Custom API definitions)
4. Import to target environment

---

## 📚 Best Practices

### ✅ Do

- **Use descriptive names**: `contoso_CalculateCommission` not `contoso_Calc1`
- **Validate all inputs**: Check for null, ranges, formats
- **Use tracing extensively**: Essential for production debugging
- **Return meaningful data**: Don't just return success/failure
- **Document parameters**: Clear descriptions in Custom API definition
- **Handle errors gracefully**: Provide actionable error messages
- **Use request/response classes**: Better maintainability
- **Write unit tests**: Test happy path and error cases

### ❌ Don't

- **Don't expose sensitive operations**: Require privileges when needed
- **Don't return sensitive data**: No passwords, keys, PII
- **Don't make long-running calls**: Use async for operations > 2 minutes
- **Don't hard-code values**: Use configuration or input parameters
- **Don't skip input validation**: Always validate before processing
- **Don't use generic names**: Avoid conflicts with future platform APIs

### Security Considerations

```csharp
// Set execute privilege to restrict access
executeprivilegename = "prvReadAccount"

// Validate permissions in code
private void ValidateUserAccess(IOrganizationService service, Guid userId)
{
    var request = new RetrieveUserPrivilegesRequest { UserId = userId };
    var response = (RetrieveUserPrivilegesResponse)service.Execute(request);
    
    if (!response.RolePrivileges.Any(p => p.PrivilegeName == "prvReadAccount"))
    {
        throw new InvalidPluginExecutionException("User lacks required privileges");
    }
}
```

---

## 🔧 Troubleshooting

### Custom API Not Appearing in Metadata

**Cause**: Not published or `isprivate = true`

**Solution**:
- Publish all customizations
- Check `isprivate` field is `false`
- Wait 5-10 minutes for metadata cache refresh

### "Action Does Not Exist" Error

**Cause**: Plugin not registered or wrong message name

**Solution**:
- Verify plugin step uses exact Custom API unique name
- Check step is registered on correct stage (PostOperation)
- Re-register the step if necessary

### Parameters Not Received in Plugin

**Cause**: Parameter name mismatch

**Solution**:
- Ensure parameter unique names match exactly (case-sensitive)
- Check parameter is marked as not optional
- Verify parameter type matches expected type

### Timeout on Long Operations

**Cause**: Synchronous execution exceeding 2-minute limit

**Solution**:
- Move long operations to async pattern
- Use background job (workflow or Azure Function)
- Break into smaller operations

---

## 🔗 Resources

- [Microsoft Custom API Documentation](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/custom-api)
- [Create a Custom API](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/create-custom-api-solution)
- [Custom API vs Plugin](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/custom-actions)
- [Calling Custom APIs](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/use-custom-actions-web-api)
- [.NET Coding Standards](/docs/standards/dotnet-coding-standards.md)

---

## 📞 Support

**Questions or Issues?**
- Open a [GitHub Discussion](https://github.com/ivoarnet/test-msd-monorepo/discussions)
- Check [examples/](/plugins/custom-apis/examples/) for code samples
- Contact the Platform team

---

**Build powerful, reusable business logic with Custom APIs! 🚀**
