# IntegrationApi - Azure Functions with OpenAPI Documentation

This Azure Functions project implements RESTful API endpoints for managing Dataverse accounts with automated OpenAPI documentation.

## Features

- ✅ **RESTful API Design**: Following REST best practices with proper HTTP verbs (GET, POST, PUT, DELETE)
- ✅ **OpenAPI Documentation**: Automatic API documentation using Microsoft.Azure.Functions.Worker.Extensions.OpenApi
- ✅ **.NET 8 Isolated Worker**: Modern Azure Functions runtime with better performance and isolation
- ✅ **Comprehensive DTOs**: Well-documented models with XML comments and examples
- ✅ **Standard Error Responses**: Consistent error handling across all endpoints

## API Endpoints

### Accounts Resource

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/accounts` | List all accounts (with optional filtering) |
| GET | `/api/accounts/{id}` | Get a specific account by ID |
| POST | `/api/accounts` | Create a new account |
| PUT | `/api/accounts/{id}` | Update an existing account |
| DELETE | `/api/accounts/{id}` | Delete an account |

## OpenAPI Documentation

### Accessing the Documentation

When running locally, the OpenAPI documentation is automatically available at:

- **Swagger UI**: `http://localhost:7071/api/swagger/ui`
- **OpenAPI JSON**: `http://localhost:7071/api/openapi/v3.json`
- **OpenAPI YAML**: `http://localhost:7071/api/openapi/v3.yaml`

### OpenAPI Features

All endpoints include:
- **Operation IDs**: Unique identifiers for each operation
- **Tags**: Logical grouping of operations (e.g., "Accounts")
- **Descriptions**: Detailed documentation of what each endpoint does
- **Parameters**: Complete parameter documentation with types and examples
- **Request Bodies**: Schema definitions for POST/PUT requests
- **Response Codes**: All possible HTTP status codes with their meanings
- **Examples**: Sample values for request and response models

### Authentication

The API uses Azure Functions API key authentication:
- Security scheme: ApiKey
- Location: Query parameter (`code`)
- Example: `http://localhost:7071/api/accounts?code=YOUR_FUNCTION_KEY`

## RESTful Design Patterns

This implementation follows RESTful best practices:

### 1. Resource-Based URLs
- ✅ Plural nouns for collections: `/api/accounts`
- ✅ Resource identifiers in path: `/api/accounts/{id}`
- ✅ No verbs in URLs (verbs come from HTTP methods)

### 2. Proper HTTP Verbs
- **GET**: Retrieve resources (safe, idempotent)
- **POST**: Create new resources (not idempotent)
- **PUT**: Update existing resources (idempotent)
- **DELETE**: Delete resources (idempotent)

### 3. HTTP Status Codes
- **200 OK**: Successful GET/PUT requests
- **201 Created**: Successful POST requests (with Location header)
- **204 No Content**: Successful DELETE requests
- **400 Bad Request**: Invalid request data
- **404 Not Found**: Resource doesn't exist
- **500 Internal Server Error**: Server errors

### 4. Standard Response Format
All error responses follow a consistent format:
```json
{
  "code": "ERROR_CODE",
  "message": "Human-readable error message",
  "details": {
    "fieldName": ["validation error 1", "validation error 2"]
  }
}
```

### 5. Resource Representation
- Use DTOs for data transfer
- Include all relevant resource properties
- Consistent naming conventions (PascalCase for C#, camelCase for JSON)

## Project Structure

```
IntegrationApi/
├── Functions/
│   └── AccountFunctions.cs       # RESTful API endpoints
├── Models/
│   ├── AccountDto.cs             # Account resource representation
│   ├── CreateAccountRequest.cs   # Create request model
│   ├── UpdateAccountRequest.cs   # Update request model
│   └── ErrorResponse.cs          # Standard error response
├── docs/
│   └── OPENAPI.md               # This file
├── Program.cs                    # Application startup with OpenAPI config
├── host.json                     # Function app configuration
├── local.settings.json           # Local development settings
└── IntegrationApi.csproj         # Project file with dependencies
```

## Development

### Building the Project

```bash
cd functions/src/IntegrationApi
dotnet restore
dotnet build
```

### Running Locally

```bash
cd functions/src/IntegrationApi
func start
```

The functions will be available at `http://localhost:7071`

### Testing Endpoints

Using curl:

```bash
# List accounts
curl http://localhost:7071/api/accounts?code=YOUR_FUNCTION_KEY

# Get specific account
curl http://localhost:7071/api/accounts/{id}?code=YOUR_FUNCTION_KEY

# Create account
curl -X POST http://localhost:7071/api/accounts?code=YOUR_FUNCTION_KEY \
  -H "Content-Type: application/json" \
  -d '{"name":"Contoso Ltd.","email":"contact@contoso.com"}'

# Update account
curl -X PUT http://localhost:7071/api/accounts/{id}?code=YOUR_FUNCTION_KEY \
  -H "Content-Type: application/json" \
  -d '{"name":"Updated Name","email":"newemail@contoso.com"}'

# Delete account
curl -X DELETE http://localhost:7071/api/accounts/{id}?code=YOUR_FUNCTION_KEY
```

## Dependencies

Key NuGet packages:

- **Microsoft.Azure.Functions.Worker** (v1.21.0): Core Functions worker
- **Microsoft.Azure.Functions.Worker.Extensions.Http** (v3.1.0): HTTP trigger support
- **Microsoft.Azure.Functions.Worker.Extensions.OpenApi** (v1.5.1): OpenAPI documentation
- **Microsoft.Azure.Functions.Worker.Sdk** (v1.17.0): Build-time SDK

## Best Practices Implemented

### 1. XML Documentation
All public classes and methods include XML documentation comments:
```csharp
/// <summary>
/// Retrieves a specific account by ID.
/// </summary>
/// <param name="req">The HTTP request.</param>
/// <param name="accountId">The account identifier.</param>
/// <returns>The account details or error response.</returns>
```

### 2. OpenAPI Attributes
Each function is decorated with comprehensive OpenAPI attributes:
```csharp
[OpenApiOperation(operationId: "GetAccount", tags: new[] { "Accounts" })]
[OpenApiParameter(name: "accountId", In = ParameterLocation.Path, Required = true)]
[OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, bodyType: typeof(AccountDto))]
```

### 3. Input Validation
All inputs are validated before processing:
```csharp
if (!Guid.TryParse(accountId, out Guid accountGuid))
{
    return BadRequest("Invalid account ID format");
}
```

### 4. Consistent Error Handling
Errors are returned in a standard format using ErrorResponse DTO.

### 5. Logging
All operations are logged for monitoring and debugging:
```csharp
_logger.LogInformation("GetAccount function triggered for account: {AccountId}", accountId);
```

## Future Enhancements

- [ ] Add authentication with Azure AD
- [ ] Implement actual Dataverse integration
- [ ] Add validation using FluentValidation
- [ ] Implement pagination for list operations
- [ ] Add API versioning
- [ ] Include more resources (contacts, orders, etc.)
- [ ] Add integration tests
- [ ] Implement HATEOAS links

## References

- [Azure Functions Documentation](https://learn.microsoft.com/en-us/azure/azure-functions/)
- [OpenAPI Extension for Azure Functions](https://github.com/Azure/azure-functions-openapi-extension)
- [RESTful API Design Guidelines](https://restfulapi.net/)
- [Microsoft REST API Guidelines](https://github.com/microsoft/api-guidelines)
