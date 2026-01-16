# Example: Simple Custom API

This example demonstrates creating a simple Custom API that validates and formats a phone number.

---

## Custom API Definition

**Unique Name**: `contoso_FormatPhoneNumber`  
**Display Name**: Format Phone Number  
**Description**: Validates and formats a phone number to standard format  
**Binding Type**: Global (0)  
**Is Function**: false (Action)  
**Is Private**: false (Public)

---

## Request Parameters

| Unique Name | Display Name | Type | Optional | Description |
|------------|--------------|------|----------|-------------|
| `PhoneNumber` | Phone Number | String (10) | No | Raw phone number to format |
| `CountryCode` | Country Code | String (10) | Yes | ISO country code (default: US) |

---

## Response Properties

| Unique Name | Display Name | Type | Description |
|------------|--------------|------|-------------|
| `FormattedNumber` | Formatted Number | String (10) | Phone number in standard format |
| `IsValid` | Is Valid | Boolean (0) | Whether the phone number is valid |
| `ErrorMessage` | Error Message | String (10) | Error details if validation failed |

---

## Implementation

### 1. Create Custom API Definition

```csharp
using Microsoft.Xrm.Sdk;
using System;

// Create Custom API record
var customApi = new Entity("customapi")
{
    ["uniquename"] = "contoso_FormatPhoneNumber",
    ["displayname"] = "Format Phone Number",
    ["description"] = "Validates and formats a phone number to standard format",
    ["bindingtype"] = new OptionSetValue(0), // Global
    ["executeprivilegename"] = null, // No special privilege required
    ["isfunction"] = false, // Action
    ["isprivate"] = false, // Public
    ["workflowsdkstepenabled"] = true, // Can be used in workflows
    ["iscustomizable"] = new BooleanManagedProperty(true)
};
var customApiId = service.Create(customApi);

// Create request parameters
var phoneNumberParam = new Entity("customapirequestparameter")
{
    ["customapiid"] = new EntityReference("customapi", customApiId),
    ["uniquename"] = "PhoneNumber",
    ["displayname"] = "Phone Number",
    ["description"] = "Raw phone number to format",
    ["type"] = new OptionSetValue(10), // String
    ["isoptional"] = false
};
service.Create(phoneNumberParam);

var countryCodeParam = new Entity("customapirequestparameter")
{
    ["customapiid"] = new EntityReference("customapi", customApiId),
    ["uniquename"] = "CountryCode",
    ["displayname"] = "Country Code",
    ["description"] = "ISO country code (default: US)",
    ["type"] = new OptionSetValue(10), // String
    ["isoptional"] = true
};
service.Create(countryCodeParam);

// Create response properties
var formattedNumberProp = new Entity("customapiresponseproperty")
{
    ["customapiid"] = new EntityReference("customapi", customApiId),
    ["uniquename"] = "FormattedNumber",
    ["displayname"] = "Formatted Number",
    ["description"] = "Phone number in standard format",
    ["type"] = new OptionSetValue(10) // String
};
service.Create(formattedNumberProp);

var isValidProp = new Entity("customapiresponseproperty")
{
    ["customapiid"] = new EntityReference("customapi", customApiId),
    ["uniquename"] = "IsValid",
    ["displayname"] = "Is Valid",
    ["description"] = "Whether the phone number is valid",
    ["type"] = new OptionSetValue(0) // Boolean
};
service.Create(isValidProp);

var errorMessageProp = new Entity("customapiresponseproperty")
{
    ["customapiid"] = new EntityReference("customapi", customApiId),
    ["uniquename"] = "ErrorMessage",
    ["displayname"] = "Error Message",
    ["description"] = "Error details if validation failed",
    ["type"] = new OptionSetValue(10) // String
};
service.Create(errorMessageProp);
```

### 2. Implement the Plugin

```csharp
using Microsoft.Xrm.Sdk;
using System;
using System.Text.RegularExpressions;

namespace Contoso.CustomApis
{
    /// <summary>
    /// Implements the contoso_FormatPhoneNumber Custom API.
    /// Validates and formats phone numbers to standard format.
    /// </summary>
    public class FormatPhoneNumberApi : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            // Get services
            var tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            tracingService.Trace("FormatPhoneNumberApi: Starting execution");

            try
            {
                // Extract input parameters
                if (!context.InputParameters.Contains("PhoneNumber"))
                {
                    throw new InvalidPluginExecutionException("PhoneNumber parameter is required.");
                }

                string phoneNumber = (string)context.InputParameters["PhoneNumber"];
                string countryCode = context.InputParameters.Contains("CountryCode") 
                    ? (string)context.InputParameters["CountryCode"] 
                    : "US";

                tracingService.Trace($"Input: PhoneNumber={phoneNumber}, CountryCode={countryCode}");

                // Validate and format
                var result = FormatPhoneNumber(phoneNumber, countryCode);

                // Set output parameters
                context.OutputParameters["FormattedNumber"] = result.FormattedNumber;
                context.OutputParameters["IsValid"] = result.IsValid;
                context.OutputParameters["ErrorMessage"] = result.ErrorMessage ?? string.Empty;

                tracingService.Trace($"Output: IsValid={result.IsValid}, FormattedNumber={result.FormattedNumber}");
                tracingService.Trace("FormatPhoneNumberApi: Completed successfully");
            }
            catch (Exception ex)
            {
                tracingService.Trace($"Error: {ex.Message}");
                throw new InvalidPluginExecutionException($"Failed to format phone number: {ex.Message}", ex);
            }
        }

        private FormatResult FormatPhoneNumber(string phoneNumber, string countryCode)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return new FormatResult
                {
                    IsValid = false,
                    ErrorMessage = "Phone number cannot be empty.",
                    FormattedNumber = string.Empty
                };
            }

            // Remove all non-digit characters
            string digitsOnly = Regex.Replace(phoneNumber, @"[^\d]", "");

            // Format based on country code
            if (countryCode == "US")
            {
                return FormatUSPhoneNumber(digitsOnly);
            }
            else
            {
                return new FormatResult
                {
                    IsValid = false,
                    ErrorMessage = $"Country code '{countryCode}' is not supported.",
                    FormattedNumber = string.Empty
                };
            }
        }

        private FormatResult FormatUSPhoneNumber(string digitsOnly)
        {
            // US phone numbers should be 10 digits (or 11 with country code)
            if (digitsOnly.Length == 11 && digitsOnly.StartsWith("1"))
            {
                digitsOnly = digitsOnly.Substring(1); // Remove country code
            }

            if (digitsOnly.Length != 10)
            {
                return new FormatResult
                {
                    IsValid = false,
                    ErrorMessage = "US phone numbers must be 10 digits.",
                    FormattedNumber = string.Empty
                };
            }

            // Format as (XXX) XXX-XXXX
            string formatted = $"({digitsOnly.Substring(0, 3)}) {digitsOnly.Substring(3, 3)}-{digitsOnly.Substring(6, 4)}";

            return new FormatResult
            {
                IsValid = true,
                ErrorMessage = null,
                FormattedNumber = formatted
            };
        }

        private class FormatResult
        {
            public bool IsValid { get; set; }
            public string FormattedNumber { get; set; }
            public string ErrorMessage { get; set; }
        }
    }
}
```

### 3. Register the Plugin

1. Build the assembly
2. Use Plugin Registration Tool:
   - Register the assembly
   - Register a new step:
     - **Message**: `contoso_FormatPhoneNumber`
     - **Primary Entity**: None
     - **Stage**: PostOperation (40)
     - **Execution Mode**: Synchronous

---

## Usage Examples

### From JavaScript (Form Script)

```javascript
function formatPhoneNumber() {
    var rawPhone = Xrm.Page.getAttribute("telephone1").getValue();
    
    if (!rawPhone) {
        return;
    }

    var request = {
        PhoneNumber: rawPhone,
        CountryCode: "US",
        
        getMetadata: function() {
            return {
                boundParameter: null,
                parameterTypes: {
                    "PhoneNumber": { typeName: "Edm.String", structuralProperty: 1 },
                    "CountryCode": { typeName: "Edm.String", structuralProperty: 1 }
                },
                operationType: 0, // Action
                operationName: "contoso_FormatPhoneNumber"
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
        if (result.IsValid) {
            // Update the field with formatted number
            Xrm.Page.getAttribute("telephone1").setValue(result.FormattedNumber);
            Xrm.Utility.alertDialog("Phone formatted: " + result.FormattedNumber);
        } else {
            // Show error
            Xrm.Utility.alertDialog("Invalid phone: " + result.ErrorMessage);
        }
    }).catch(function(error) {
        console.error("Error calling Custom API:", error);
    });
}
```

### From C# Plugin

```csharp
var request = new OrganizationRequest("contoso_FormatPhoneNumber")
{
    ["PhoneNumber"] = "(555) 123-4567",
    ["CountryCode"] = "US"
};

var response = service.Execute(request);

if ((bool)response["IsValid"])
{
    string formattedNumber = (string)response["FormattedNumber"];
    tracingService.Trace($"Formatted: {formattedNumber}");
    
    // Use the formatted number
    entity["telephone1"] = formattedNumber;
}
else
{
    string errorMessage = (string)response["ErrorMessage"];
    throw new InvalidPluginExecutionException($"Phone validation failed: {errorMessage}");
}
```

### From Web API (REST)

```http
POST [Organization URI]/api/data/v9.2/contoso_FormatPhoneNumber
Content-Type: application/json

{
  "PhoneNumber": "5551234567",
  "CountryCode": "US"
}
```

**Response**:
```json
{
  "@odata.context": "[Organization URI]/api/data/v9.2/$metadata#Microsoft.Dynamics.CRM.contoso_FormatPhoneNumberResponse",
  "FormattedNumber": "(555) 123-4567",
  "IsValid": true,
  "ErrorMessage": ""
}
```

### From Power Automate

1. Add action: **Perform an unbound action**
2. Select **contoso_FormatPhoneNumber**
3. Fill in parameters:
   - **PhoneNumber**: (use dynamic content)
   - **CountryCode**: `US`
4. Use outputs in subsequent steps

---

## Unit Tests

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using System;

namespace Contoso.CustomApis.Tests
{
    [TestClass]
    public class FormatPhoneNumberApiTests
    {
        [TestMethod]
        public void Execute_WithValidUSPhone_FormatsCorrectly()
        {
            // Arrange
            var api = new FormatPhoneNumberApi();
            var context = new FakePluginExecutionContext
            {
                InputParameters = new ParameterCollection
                {
                    { "PhoneNumber", "5551234567" },
                    { "CountryCode", "US" }
                },
                OutputParameters = new ParameterCollection()
            };
            var serviceProvider = new FakeServiceProvider(context);

            // Act
            api.Execute(serviceProvider);

            // Assert
            Assert.AreEqual("(555) 123-4567", context.OutputParameters["FormattedNumber"]);
            Assert.AreEqual(true, context.OutputParameters["IsValid"]);
        }

        [TestMethod]
        public void Execute_WithInvalidPhone_ReturnsError()
        {
            // Arrange
            var api = new FormatPhoneNumberApi();
            var context = new FakePluginExecutionContext
            {
                InputParameters = new ParameterCollection
                {
                    { "PhoneNumber", "123" },
                    { "CountryCode", "US" }
                },
                OutputParameters = new ParameterCollection()
            };
            var serviceProvider = new FakeServiceProvider(context);

            // Act
            api.Execute(serviceProvider);

            // Assert
            Assert.AreEqual(false, context.OutputParameters["IsValid"]);
            Assert.IsTrue(((string)context.OutputParameters["ErrorMessage"]).Length > 0);
        }

        [TestMethod]
        public void Execute_WithFormattedPhone_CleansAndFormats()
        {
            // Arrange
            var api = new FormatPhoneNumberApi();
            var context = new FakePluginExecutionContext
            {
                InputParameters = new ParameterCollection
                {
                    { "PhoneNumber", "(555) 123-4567" },
                    { "CountryCode", "US" }
                },
                OutputParameters = new ParameterCollection()
            };
            var serviceProvider = new FakeServiceProvider(context);

            // Act
            api.Execute(serviceProvider);

            // Assert
            Assert.AreEqual("(555) 123-4567", context.OutputParameters["FormattedNumber"]);
            Assert.AreEqual(true, context.OutputParameters["IsValid"]);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidPluginExecutionException))]
        public void Execute_WithMissingParameter_ThrowsException()
        {
            // Arrange
            var api = new FormatPhoneNumberApi();
            var context = new FakePluginExecutionContext
            {
                InputParameters = new ParameterCollection(), // Missing PhoneNumber
                OutputParameters = new ParameterCollection()
            };
            var serviceProvider = new FakeServiceProvider(context);

            // Act
            api.Execute(serviceProvider); // Should throw
        }
    }
}
```

---

## Key Takeaways

✅ **Simple and focused**: Does one thing well  
✅ **Comprehensive validation**: Checks all inputs  
✅ **Clear error messages**: Helps users understand what went wrong  
✅ **Well-documented**: Easy for others to use  
✅ **Unit tested**: Ensures reliability  
✅ **Reusable**: Can be called from anywhere  

This pattern can be adapted for other simple validation and transformation Custom APIs.
