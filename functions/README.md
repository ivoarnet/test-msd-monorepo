# Azure Functions

Integration APIs and background processing for Dynamics 365.

> **✨ NEW: OpenAPI Documentation Available!**  
> The IntegrationApi project now includes automated OpenAPI documentation using Microsoft.Azure.Functions.Worker.Extensions.OpenApi.  
> See [src/IntegrationApi/docs/OPENAPI.md](src/IntegrationApi/docs/OPENAPI.md) for details on the RESTful API implementation.

---

## 📋 Overview

Azure Functions provide:
- REST APIs for external integrations
- Scheduled background jobs
- Event-driven processing (Service Bus, Event Grid)
- Webhooks for third-party systems

---

## 🏗️ Structure

```
functions/
├── src/
│   ├── IntegrationApi/            # HTTP-triggered RESTful API functions
│   │   ├── Functions/
│   │   │   └── AccountFunctions.cs    # CRUD operations for accounts
│   │   ├── Models/
│   │   │   ├── AccountDto.cs          # Account resource representation
│   │   │   ├── CreateAccountRequest.cs # Create request model
│   │   │   ├── UpdateAccountRequest.cs # Update request model
│   │   │   └── ErrorResponse.cs        # Standard error response
│   │   ├── docs/
│   │   │   └── OPENAPI.md             # OpenAPI documentation guide
│   │   ├── host.json
│   │   ├── local.settings.json
│   │   ├── Program.cs
│   │   └── IntegrationApi.csproj
│   └── shared/                    # Code shared across function apps (future)
│       ├── Models/
│       └── Utilities/
├── tests/
│   └── IntegrationApi.Tests/     # Unit tests (future)
└── README.md
```

---

## 🚀 Getting Started

### Prerequisites

- .NET SDK 6.0+
- Azure Functions Core Tools v4
- Azure CLI
- Azure subscription (for deployment)

Install Azure Functions Core Tools:
```bash
npm install -g azure-functions-core-tools@4 --unsafe-perm true
```

### Build

```bash
cd functions
dotnet restore
dotnet build
```

### Run Locally

```bash
cd functions/src/IntegrationApi
cp ../../local.settings.json.example local.settings.json
# Edit local.settings.json with your values
func start
```

Functions will be available at `http://localhost:7071/api/{functionName}`

### Accessing OpenAPI Documentation

Once the IntegrationApi is running, you can access the automated API documentation:

- **Swagger UI**: `http://localhost:7071/api/swagger/ui`
- **OpenAPI JSON**: `http://localhost:7071/api/openapi/v3.json`
- **OpenAPI YAML**: `http://localhost:7071/api/openapi/v3.yaml`

The OpenAPI documentation provides:
- Interactive API testing
- Complete request/response schemas
- Authentication requirements
- Example values for all models

See [IntegrationApi OpenAPI Guide](src/IntegrationApi/docs/OPENAPI.md) for more details.

---

## 📝 Creating a New Function

### 1. Add Function Class

```csharp
// src/IntegrationApi/Functions/GetAccountFunction.cs
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Contoso.IntegrationApi.Functions
{
    /// <summary>
    /// HTTP trigger function to retrieve account details from Dataverse.
    /// </summary>
    public class GetAccountFunction
    {
        private readonly IDataverseService _dataverseService;

        public GetAccountFunction(IDataverseService dataverseService)
        {
            _dataverseService = dataverseService;
        }

        [FunctionName("GetAccount")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "accounts/{accountId}")] HttpRequest req,
            string accountId,
            ILogger log)
        {
            log.LogInformation($"GetAccount function triggered for account: {accountId}");

            try
            {
                // Validate input
                if (!Guid.TryParse(accountId, out Guid accountGuid))
                {
                    return new BadRequestObjectResult(new { error = "Invalid account ID format" });
                }

                // Query Dataverse
                var account = await _dataverseService.GetAccountByIdAsync(accountGuid);

                if (account == null)
                {
                    return new NotFoundObjectResult(new { error = "Account not found" });
                }

                // Return result
                return new OkObjectResult(account);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Error retrieving account");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
```

### 2. Dependency Injection Setup

```csharp
// src/IntegrationApi/Startup.cs
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Contoso.IntegrationApi.Startup))]

namespace Contoso.IntegrationApi
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            // Register services
            builder.Services.AddSingleton<IDataverseService, DataverseService>();
            builder.Services.AddHttpClient<IThirdPartyApiClient, ThirdPartyApiClient>();

            // Add configuration
            builder.Services.AddOptions<DataverseOptions>()
                .Configure<IConfiguration>((settings, configuration) =>
                {
                    configuration.GetSection("Dataverse").Bind(settings);
                });
        }
    }
}
```

### 3. Dataverse Service Implementation

```csharp
// src/IntegrationApi/Services/DataverseService.cs
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Threading.Tasks;

namespace Contoso.IntegrationApi.Services
{
    public interface IDataverseService
    {
        Task<AccountDto> GetAccountByIdAsync(Guid accountId);
    }

    public class DataverseService : IDataverseService
    {
        private readonly ServiceClient _serviceClient;

        public DataverseService(IConfiguration configuration)
        {
            var connectionString = configuration["Dataverse:ConnectionString"];
            _serviceClient = new ServiceClient(connectionString);
        }

        public async Task<AccountDto> GetAccountByIdAsync(Guid accountId)
        {
            var query = new QueryExpression("account")
            {
                ColumnSet = new ColumnSet("name", "accountnumber", "revenue"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("accountid", ConditionOperator.Equal, accountId)
                    }
                }
            };

            var result = await Task.Run(() => _serviceClient.RetrieveMultiple(query));

            if (result.Entities.Count == 0)
            {
                return null;
            }

            var entity = result.Entities[0];
            return new AccountDto
            {
                Id = entity.Id,
                Name = entity.GetAttributeValue<string>("name"),
                AccountNumber = entity.GetAttributeValue<string>("accountnumber"),
                Revenue = entity.GetAttributeValue<Money>("revenue")?.Value
            };
        }
    }
}
```

---

## 🎯 Common Patterns

### Timer-Triggered Function

```csharp
[FunctionName("SyncOrdersDaily")]
public async Task Run(
    [TimerTrigger("0 0 2 * * *")] TimerInfo timer, // Runs daily at 2 AM
    ILogger log)
{
    log.LogInformation($"SyncOrdersDaily triggered at: {DateTime.Now}");

    // Background processing logic
    await _orderSyncService.SyncOrdersAsync();

    log.LogInformation("SyncOrdersDaily completed");
}
```

### Service Bus Triggered Function

```csharp
[FunctionName("ProcessOrderQueue")]
public async Task Run(
    [ServiceBusTrigger("orders", Connection = "ServiceBusConnection")] string message,
    ILogger log)
{
    log.LogInformation($"Processing message: {message}");

    var order = JsonSerializer.Deserialize<OrderMessage>(message);
    await _orderProcessor.ProcessOrderAsync(order);
}
```

---

## 🔧 Configuration

### local.settings.json

For local development:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet",
    "Dataverse:ConnectionString": "AuthType=OAuth;Url=https://yourorg.crm.dynamics.com;...",
    "ServiceBusConnection": "Endpoint=sb://yournamespace.servicebus.windows.net/;...",
    "ThirdPartyApi:BaseUrl": "https://api.thirdparty.com",
    "ThirdPartyApi:ApiKey": "your-api-key-here"
  }
}
```

> ⚠️ **Never commit `local.settings.json`** - it's in `.gitignore`

### Azure App Settings

In production, configure via Azure Portal or CLI:

```bash
az functionapp config appsettings set \
  --name my-function-app \
  --resource-group my-rg \
  --settings "Dataverse:ConnectionString=..."
```

Or use Azure Key Vault references:

```json
{
  "Dataverse:ConnectionString": "@Microsoft.KeyVault(SecretUri=https://myvault.vault.azure.net/secrets/DataverseConnection/)"
}
```

---

## 🧪 Testing

### Unit Tests

```csharp
[TestClass]
public class GetAccountFunctionTests
{
    [TestMethod]
    public async Task Run_WithValidId_ReturnsAccount()
    {
        // Arrange
        var mockService = new Mock<IDataverseService>();
        mockService.Setup(s => s.GetAccountByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new AccountDto { Name = "Test Account" });

        var function = new GetAccountFunction(mockService.Object);
        var request = new DefaultHttpContext().Request;
        var logger = new Mock<ILogger>().Object;

        // Act
        var result = await function.Run(request, Guid.NewGuid().ToString(), logger);

        // Assert
        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
    }
}
```

### Integration Tests

```csharp
[TestClass]
public class DataverseServiceIntegrationTests
{
    private IDataverseService _service;

    [TestInitialize]
    public void Setup()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("testsettings.json")
            .Build();

        _service = new DataverseService(configuration);
    }

    [TestMethod]
    public async Task GetAccountById_WithRealEnvironment_ReturnsAccount()
    {
        // Requires connection to real Dataverse environment
        var accountId = Guid.Parse("..."); // Known test account
        var account = await _service.GetAccountByIdAsync(accountId);

        Assert.IsNotNull(account);
        Assert.AreEqual("Test Account", account.Name);
    }
}
```

---

## 📦 Deployment

### Manual Deployment

```bash
# Build and publish
cd functions/src/IntegrationApi
dotnet publish -c Release

# Deploy to Azure
func azure functionapp publish my-function-app
```

### CI/CD Deployment

See [functions-ci.yml](/.github/workflows/functions-ci.yml) for automated deployments.

---

## 🔒 Security

### API Key Authentication

Functions use function keys by default:

```
https://my-function-app.azurewebsites.net/api/GetAccount/123?code=YOUR_FUNCTION_KEY
```

### Azure AD Authentication

Enable Azure AD for stronger security:

```csharp
[FunctionName("GetAccount")]
[Authorize] // Requires valid JWT token
public async Task<IActionResult> Run(...)
{
    // Access user claims
    var userId = req.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
}
```

### Managed Identity

Use managed identity to access Dataverse:

```csharp
var credential = new DefaultAzureCredential();
var token = await credential.GetTokenAsync(new TokenRequestContext(
    new[] { "https://yourorg.crm.dynamics.com/.default" }
));
```

---

## 📊 Monitoring

### Application Insights

Add to `host.json`:

```json
{
  "logging": {
    "applicationInsights": {
      "samplingSettings": {
        "isEnabled": true,
        "maxTelemetryItemsPerSecond": 20
      }
    }
  }
}
```

### Custom Metrics

```csharp
private readonly TelemetryClient _telemetryClient;

public void TrackOrderProcessed(string orderId, double processingTimeMs)
{
    _telemetryClient.TrackEvent("OrderProcessed", new Dictionary<string, string>
    {
        { "OrderId", orderId }
    }, new Dictionary<string, double>
    {
        { "ProcessingTime", processingTimeMs }
    });
}
```

---

## 📚 Best Practices

✅ **Do**:
- Use dependency injection
- Validate all inputs
- Log important events
- Use async/await
- Handle transient failures (retry policies)
- Separate business logic from function handlers
- Use managed identities

❌ **Don't**:
- Store connection strings in code
- Make long-running synchronous calls
- Ignore exception handling
- Log sensitive data
- Use static HttpClient (use IHttpClientFactory)
- Deploy without testing locally

---

## 🔗 Resources

- [Azure Functions Documentation](https://learn.microsoft.com/en-us/azure/azure-functions/)
- [Dataverse ServiceClient](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/xrm-tooling/use-connection-strings-xrm-tooling-connect)
- [.NET Coding Standards](/docs/standards/dotnet-coding-standards.md)

---

**Questions? Contact the Integrations team or open a [GitHub Discussion](https://github.com/ivoarnet/test-msd-monorepo/discussions).**
