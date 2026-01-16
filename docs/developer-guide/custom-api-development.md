# Custom API Development Guide

Step-by-step guide for developing, testing, and deploying Dataverse Custom APIs.

---

## 📋 Table of Contents

1. [Planning Your Custom API](#planning-your-custom-api)
2. [Creating the Definition](#creating-the-definition)
3. [Implementing the Logic](#implementing-the-logic)
4. [Testing](#testing)
5. [Deployment](#deployment)
6. [Troubleshooting](#troubleshooting)

---

## Planning Your Custom API

### Step 1: Define the Purpose

Ask yourself:
- **What problem does this solve?**
- **Who will use this?** (JavaScript, plugins, Power Automate, external apps)
- **Should this be a Custom API or a plugin?**

| Choose Custom API If... | Choose Plugin If... |
|------------------------|---------------------|
| Called explicitly (on demand) | Reacts to data events |
| Needs to be discoverable | Runs automatically on Create/Update/Delete |
| Used by multiple consumers | Tied to specific entity lifecycle |
| Requires strongly-typed contract | Needs to modify data in pipeline |

### Step 2: Design the Contract

Define inputs and outputs:

**Example: Calculate Discount**
```
Input:
- OrderTotal: Decimal (required)
- CustomerTier: OptionSet (optional, default: Standard)
- PromoCode: String (optional)

Output:
- DiscountAmount: Money
- DiscountPercentage: Decimal
- AppliedPromoCode: String
```

### Step 3: Choose Binding Type

| Binding Type | Use When | Example |
|-------------|----------|---------|
| **Global (0)** | Not tied to specific entity | `CalculateShipping`, `ValidateEmail` |
| **Entity (1)** | Operates on single entity | `Account.CalculateLifetimeValue` |
| **EntityCollection (2)** | Operates on multiple entities | `BulkUpdatePrices` |

### Step 4: Function vs Action

| Type | HTTP Method | Characteristics | Use For |
|------|-------------|----------------|---------|
| **Function** | GET | Read-only, no side effects | Calculations, validations, queries |
| **Action** | POST | Can modify data | Data updates, complex operations |

**Rule of thumb**: If it changes data or has side effects, use Action.

---

## Creating the Definition

### Option 1: Using Plugin Registration Tool (Recommended)

1. **Open Plugin Registration Tool**
2. **Connect** to your environment
3. **Register → Register New Custom API**
4. **Fill in details**:

```
General:
  Unique Name: contoso_CalculateDiscount
  Display Name: Calculate Discount
  Description: Calculates discount based on order total and customer tier
  Binding Type: Global
  Bound Entity: (leave blank for global)

Settings:
  Is Function: No (Action)
  Is Private: No (Public)
  Execute Privilege Name: (optional - leave blank for no restriction)
  Allowed Custom Processing Step Type: Async and Sync
  Workflow SDK Step Enabled: Yes (allows use in Power Automate)
```

5. **Add Request Parameters**:

Click **Register New Request Parameter**:

```
Parameter 1:
  Unique Name: OrderTotal
  Display Name: Order Total
  Type: Decimal
  Is Optional: No

Parameter 2:
  Unique Name: CustomerTier
  Display Name: Customer Tier
  Type: Picklist
  Is Optional: Yes

Parameter 3:
  Unique Name: PromoCode
  Display Name: Promo Code
  Type: String
  Is Optional: Yes
```

6. **Add Response Properties**:

Click **Register New Response Property**:

```
Property 1:
  Unique Name: DiscountAmount
  Display Name: Discount Amount
  Type: Money

Property 2:
  Unique Name: DiscountPercentage
  Display Name: Discount Percentage
  Type: Decimal

Property 3:
  Unique Name: AppliedPromoCode
  Display Name: Applied Promo Code
  Type: String
```

### Option 2: Programmatically (C#)

```csharp
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

public void CreateCustomApiDefinition(IOrganizationService service)
{
    // Create Custom API
    var customApi = new Entity("customapi")
    {
        ["uniquename"] = "contoso_CalculateDiscount",
        ["displayname"] = "Calculate Discount",
        ["description"] = "Calculates discount based on order total and customer tier",
        ["bindingtype"] = new OptionSetValue(0), // Global
        ["boundentitylogicalname"] = null,
        ["executeprivilegename"] = null,
        ["isfunction"] = false, // Action
        ["isprivate"] = false, // Public
        ["workflowsdkstepenabled"] = true,
        ["allowedcustomprocessingsteptype"] = new OptionSetValue(0), // Both async and sync
        ["iscustomizable"] = new BooleanManagedProperty(true)
    };
    var customApiId = service.Create(customApi);

    // Create request parameters
    CreateRequestParameter(service, customApiId, "OrderTotal", "Order Total", 2, false); // Decimal
    CreateRequestParameter(service, customApiId, "CustomerTier", "Customer Tier", 9, true); // Picklist
    CreateRequestParameter(service, customApiId, "PromoCode", "Promo Code", 10, true); // String

    // Create response properties
    CreateResponseProperty(service, customApiId, "DiscountAmount", "Discount Amount", 8); // Money
    CreateResponseProperty(service, customApiId, "DiscountPercentage", "Discount Percentage", 2); // Decimal
    CreateResponseProperty(service, customApiId, "AppliedPromoCode", "Applied Promo Code", 10); // String
}

private void CreateRequestParameter(
    IOrganizationService service,
    Guid customApiId,
    string uniqueName,
    string displayName,
    int type,
    bool isOptional)
{
    var param = new Entity("customapirequestparameter")
    {
        ["customapiid"] = new EntityReference("customapi", customApiId),
        ["uniquename"] = uniqueName,
        ["displayname"] = displayName,
        ["type"] = new OptionSetValue(type),
        ["isoptional"] = isOptional,
        ["logicalentityname"] = null
    };
    service.Create(param);
}

private void CreateResponseProperty(
    IOrganizationService service,
    Guid customApiId,
    string uniqueName,
    string displayName,
    int type)
{
    var prop = new Entity("customapiresponseproperty")
    {
        ["customapiid"] = new EntityReference("customapi", customApiId),
        ["uniquename"] = uniqueName,
        ["displayname"] = displayName,
        ["type"] = new OptionSetValue(type),
        ["logicalentityname"] = null
    };
    service.Create(prop);
}
```

### Option 3: Using Solution XML (Advanced)

Include in your solution customizations.xml:

```xml
<customapi uniquename="contoso_CalculateDiscount">
  <displayname>Calculate Discount</displayname>
  <description>Calculates discount based on order total and customer tier</description>
  <bindingtype>0</bindingtype>
  <isfunction>0</isfunction>
  <isprivate>0</isprivate>
  <workflowsdkstepenabled>1</workflowsdkstepenabled>
</customapi>
```

---

## Implementing the Logic

### Step 1: Create the Plugin Project

```bash
cd plugins/custom-apis
dotnet new classlib -n Contoso.CustomApis
cd Contoso.CustomApis
dotnet add package Microsoft.CrmSdk.CoreAssemblies
```

### Step 2: Create Request/Response Classes

```csharp
// CalculateDiscountRequest.cs
namespace Contoso.CustomApis.CalculateDiscount
{
    public class CalculateDiscountRequest
    {
        public decimal OrderTotal { get; set; }
        public int? CustomerTier { get; set; }
        public string PromoCode { get; set; }

        public static CalculateDiscountRequest FromContext(IPluginExecutionContext context)
        {
            return new CalculateDiscountRequest
            {
                OrderTotal = (decimal)context.InputParameters["OrderTotal"],
                CustomerTier = context.InputParameters.Contains("CustomerTier")
                    ? ((OptionSetValue)context.InputParameters["CustomerTier"])?.Value
                    : null,
                PromoCode = context.InputParameters.Contains("PromoCode")
                    ? (string)context.InputParameters["PromoCode"]
                    : null
            };
        }

        public void Validate()
        {
            if (OrderTotal <= 0)
            {
                throw new InvalidPluginExecutionException("OrderTotal must be greater than zero.");
            }

            if (CustomerTier.HasValue && (CustomerTier < 1 || CustomerTier > 3))
            {
                throw new InvalidPluginExecutionException("CustomerTier must be between 1 and 3.");
            }
        }
    }
}
```

```csharp
// CalculateDiscountResponse.cs
namespace Contoso.CustomApis.CalculateDiscount
{
    public class CalculateDiscountResponse
    {
        public Money DiscountAmount { get; set; }
        public decimal DiscountPercentage { get; set; }
        public string AppliedPromoCode { get; set; }

        public void ToContext(IPluginExecutionContext context)
        {
            context.OutputParameters["DiscountAmount"] = DiscountAmount;
            context.OutputParameters["DiscountPercentage"] = DiscountPercentage;
            context.OutputParameters["AppliedPromoCode"] = AppliedPromoCode ?? string.Empty;
        }
    }
}
```

### Step 3: Implement the Custom API

```csharp
// CalculateDiscountApi.cs
using Microsoft.Xrm.Sdk;
using System;

namespace Contoso.CustomApis.CalculateDiscount
{
    /// <summary>
    /// Implements the contoso_CalculateDiscount Custom API.
    /// Calculates discount based on order total, customer tier, and promo codes.
    /// </summary>
    public class CalculateDiscountApi : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var service = serviceFactory.CreateOrganizationService(context.UserId);

            tracingService.Trace("CalculateDiscountApi: Starting execution");

            try
            {
                // Parse and validate request
                var request = CalculateDiscountRequest.FromContext(context);
                request.Validate();

                tracingService.Trace($"OrderTotal: {request.OrderTotal}, CustomerTier: {request.CustomerTier}, PromoCode: {request.PromoCode}");

                // Calculate discount
                var calculator = new DiscountCalculator(service, tracingService);
                var response = calculator.Calculate(request);

                // Return response
                response.ToContext(context);

                tracingService.Trace($"Discount calculated: {response.DiscountAmount.Value} ({response.DiscountPercentage}%)");
                tracingService.Trace("CalculateDiscountApi: Completed successfully");
            }
            catch (InvalidPluginExecutionException)
            {
                throw; // Re-throw validation errors
            }
            catch (Exception ex)
            {
                tracingService.Trace($"Error: {ex.Message}");
                throw new InvalidPluginExecutionException($"Failed to calculate discount: {ex.Message}", ex);
            }
        }
    }
}
```

### Step 4: Implement Business Logic

```csharp
// DiscountCalculator.cs
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;

namespace Contoso.CustomApis.CalculateDiscount
{
    internal class DiscountCalculator
    {
        private readonly IOrganizationService _service;
        private readonly ITracingService _tracingService;

        public DiscountCalculator(IOrganizationService service, ITracingService tracingService)
        {
            _service = service;
            _tracingService = tracingService;
        }

        public CalculateDiscountResponse Calculate(CalculateDiscountRequest request)
        {
            decimal discountPercentage = 0m;
            string appliedPromoCode = null;

            // Calculate tier discount
            discountPercentage += GetTierDiscount(request.CustomerTier);

            // Calculate promo code discount
            if (!string.IsNullOrWhiteSpace(request.PromoCode))
            {
                var promoDiscount = GetPromoCodeDiscount(request.PromoCode);
                if (promoDiscount > 0)
                {
                    discountPercentage += promoDiscount;
                    appliedPromoCode = request.PromoCode;
                    _tracingService.Trace($"Promo code '{request.PromoCode}' applied: {promoDiscount}%");
                }
            }

            // Cap at 50% discount
            if (discountPercentage > 0.5m)
            {
                _tracingService.Trace($"Discount capped at 50% (was {discountPercentage * 100}%)");
                discountPercentage = 0.5m;
            }

            // Calculate amount
            decimal discountAmount = request.OrderTotal * discountPercentage;

            return new CalculateDiscountResponse
            {
                DiscountAmount = new Money(discountAmount),
                DiscountPercentage = discountPercentage,
                AppliedPromoCode = appliedPromoCode
            };
        }

        private decimal GetTierDiscount(int? tier)
        {
            if (!tier.HasValue) return 0m;

            switch (tier.Value)
            {
                case 1: // Bronze
                    return 0.05m; // 5%
                case 2: // Silver
                    return 0.10m; // 10%
                case 3: // Gold
                    return 0.15m; // 15%
                default:
                    return 0m;
            }
        }

        private decimal GetPromoCodeDiscount(string promoCode)
        {
            // Query promo code entity
            var query = new QueryExpression("contoso_promocode")
            {
                ColumnSet = new ColumnSet("contoso_discountpercentage", "contoso_expirydate", "contoso_isactive"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("contoso_code", ConditionOperator.Equal, promoCode)
                    }
                }
            };

            var results = _service.RetrieveMultiple(query);

            if (results.Entities.Count == 0)
            {
                _tracingService.Trace($"Promo code '{promoCode}' not found");
                return 0m;
            }

            var promoEntity = results.Entities[0];
            bool isActive = promoEntity.GetAttributeValue<bool>("contoso_isactive");
            DateTime? expiryDate = promoEntity.Contains("contoso_expirydate")
                ? promoEntity.GetAttributeValue<DateTime>("contoso_expirydate")
                : null;

            // Validate promo code
            if (!isActive)
            {
                _tracingService.Trace($"Promo code '{promoCode}' is inactive");
                return 0m;
            }

            if (expiryDate.HasValue && expiryDate.Value < DateTime.UtcNow)
            {
                _tracingService.Trace($"Promo code '{promoCode}' has expired");
                return 0m;
            }

            decimal discountPercentage = promoEntity.GetAttributeValue<decimal>("contoso_discountpercentage");
            return discountPercentage / 100m; // Convert percentage to decimal
        }
    }
}
```

### Step 5: Build the Assembly

```bash
dotnet build -c Release
```

Make sure the assembly is **strong-named** (required for Dataverse plugins).

---

## Testing

### Unit Tests

```csharp
// CalculateDiscountApiTests.cs
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using FakeXrmEasy;

namespace Contoso.CustomApis.Tests
{
    [TestClass]
    public class CalculateDiscountApiTests
    {
        [TestMethod]
        public void Execute_WithGoldTier_Applies15PercentDiscount()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();
            
            var pluginContext = context.GetDefaultPluginContext();
            pluginContext.MessageName = "contoso_CalculateDiscount";
            pluginContext.InputParameters = new ParameterCollection
            {
                { "OrderTotal", 1000.00m },
                { "CustomerTier", new OptionSetValue(3) } // Gold
            };
            pluginContext.OutputParameters = new ParameterCollection();

            var api = new CalculateDiscountApi();

            // Act
            context.ExecutePluginWith(pluginContext, api);

            // Assert
            var discountAmount = (Money)pluginContext.OutputParameters["DiscountAmount"];
            var discountPercentage = (decimal)pluginContext.OutputParameters["DiscountPercentage"];

            Assert.AreEqual(150.00m, discountAmount.Value);
            Assert.AreEqual(0.15m, discountPercentage);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidPluginExecutionException))]
        public void Execute_WithNegativeOrderTotal_ThrowsException()
        {
            // Arrange
            var context = new XrmFakedContext();
            var pluginContext = context.GetDefaultPluginContext();
            pluginContext.MessageName = "contoso_CalculateDiscount";
            pluginContext.InputParameters = new ParameterCollection
            {
                { "OrderTotal", -100.00m }
            };

            var api = new CalculateDiscountApi();

            // Act
            context.ExecutePluginWith(pluginContext, api); // Should throw
        }
    }
}
```

### Integration Testing (Manual)

1. **Register the assembly** using Plugin Registration Tool
2. **Register the step**:
   - Message: `contoso_CalculateDiscount`
   - Stage: PostOperation (40)
   - Mode: Synchronous

3. **Test from Browser Console** (F12):

```javascript
var request = {
    OrderTotal: 1000.00,
    CustomerTier: { Value: 3 }, // Gold
    
    getMetadata: function() {
        return {
            boundParameter: null,
            parameterTypes: {
                "OrderTotal": { typeName: "Edm.Decimal", structuralProperty: 1 },
                "CustomerTier": { typeName: "Edm.Int32", structuralProperty: 1 }
            },
            operationType: 0,
            operationName: "contoso_CalculateDiscount"
        };
    }
};

Xrm.WebApi.online.execute(request).then(
    function(response) { return response.json(); }
).then(function(result) {
    console.log("Discount Amount:", result.DiscountAmount);
    console.log("Discount Percentage:", result.DiscountPercentage);
});
```

4. **Check trace logs** in Plugin Registration Tool

---

## Deployment

### Development Environment

1. Build: `dotnet build -c Release`
2. Register assembly in Plugin Registration Tool
3. Register step on Custom API message
4. Test

### Production Deployment

1. **Add to solution**:
   - Custom API definition
   - Plugin assembly
   - Request parameters
   - Response properties

2. **Export solution** (managed)

3. **Import to target environment**

4. **Verify** Custom API appears in metadata:
   ```
   GET [Organization URI]/api/data/v9.2/$metadata
   ```

### CI/CD Pipeline

```yaml
# .github/workflows/custom-apis-ci.yml
name: Custom APIs CI/CD

on:
  push:
    paths:
      - 'plugins/custom-apis/**'

jobs:
  build-and-deploy:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      
      - name: Restore dependencies
        run: dotnet restore plugins/custom-apis
      
      - name: Build
        run: dotnet build plugins/custom-apis -c Release
      
      - name: Run tests
        run: dotnet test plugins/custom-apis
      
      - name: Deploy to Dev
        run: |
          # Use Power Platform CLI to deploy
          pac solution import -p solution.zip
```

---

## Troubleshooting

### Custom API Not Appearing in Metadata

**Symptom**: Custom API not visible in `$metadata` or API calls fail with "Action does not exist"

**Causes & Solutions**:
1. **Not published**: Publish all customizations
2. **Private API**: Set `isprivate = false`
3. **Cache**: Wait 5-10 minutes or clear browser cache
4. **Wrong unique name**: Verify exact spelling (case-sensitive)

### Parameters Not Received in Plugin

**Symptom**: `InputParameters` missing expected values

**Solutions**:
1. **Check parameter names**: Must match exactly (case-sensitive)
2. **Verify not optional**: Set `isoptional = false` for required params
3. **Check caller**: Ensure caller passes all required parameters

### Plugin Not Executing

**Symptom**: Custom API calls succeed but plugin doesn't run

**Solutions**:
1. **Verify step registration**: Message name must match Custom API unique name exactly
2. **Check stage**: Use PostOperation (40) for Custom APIs
3. **Check filters**: Remove any entity filters (use "none" for global APIs)

### Permission Denied

**Symptom**: Users get "Principal user lacks privilege" error

**Solutions**:
1. **Set execute privilege**: Specify required privilege in `executeprivilegename`
2. **Grant privilege**: Add privilege to security roles
3. **Check entity permissions**: Ensure user can access related entities

### Timeout Errors

**Symptom**: "SQL timeout" or "Operation timeout"

**Solutions**:
1. **Optimize queries**: Use specific ColumnSet, add indexes
2. **Use async processing**: For operations > 2 minutes
3. **Batch operations**: Use `ExecuteMultipleRequest`
4. **Cache data**: Reduce database calls

---

## Best Practices Checklist

Before deploying, verify:

- [ ] Custom API has descriptive unique name with publisher prefix
- [ ] All parameters and responses are documented (description field)
- [ ] Input validation implemented
- [ ] Comprehensive tracing added
- [ ] No sensitive data in traces or responses
- [ ] Unit tests cover happy path and error cases
- [ ] Integration tested in dev environment
- [ ] Execute privilege set if needed
- [ ] Error messages are user-friendly
- [ ] Performance tested with realistic data volumes

---

## Additional Resources

- [Custom API Reference](/plugins/custom-apis/README.md)
- [Simple Example](/plugins/custom-apis/examples/simple-custom-api.md)
- [Async Example](/plugins/custom-apis/examples/async-custom-api.md)
- [.NET Coding Standards](/docs/standards/dotnet-coding-standards.md)
- [Microsoft Custom API Docs](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/custom-api)

---

**Happy Custom API Development! 🚀**
