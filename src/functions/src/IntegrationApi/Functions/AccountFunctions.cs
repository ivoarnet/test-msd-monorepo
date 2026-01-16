using System.Net;
using IntegrationApi.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

namespace IntegrationApi.Functions;

/// <summary>
/// Azure Function for managing account resources following RESTful design patterns.
/// </summary>
public class AccountFunctions
{
    private readonly ILogger<AccountFunctions> _logger;

    public AccountFunctions(ILogger<AccountFunctions> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Retrieves a specific account by ID.
    /// </summary>
    /// <param name="req">The HTTP request.</param>
    /// <param name="accountId">The account identifier.</param>
    /// <returns>The account details or error response.</returns>
    [Function("GetAccount")]
    [OpenApiOperation(operationId: "GetAccount", tags: new[] { "Accounts" }, Summary = "Get an account by ID", Description = "Retrieves detailed information about a specific account from Dataverse.")]
    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
    [OpenApiParameter(name: "accountId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Description = "The unique identifier of the account")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(AccountDto), Description = "Successfully retrieved the account")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(ErrorResponse), Description = "Account not found")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(ErrorResponse), Description = "Invalid account ID format")]
    public async Task<HttpResponseData> GetAccount(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "accounts/{accountId}")] HttpRequestData req,
        string accountId)
    {
        _logger.LogInformation("GetAccount function triggered for account: {AccountId}", accountId);

        // Validate account ID format
        if (!Guid.TryParse(accountId, out Guid accountGuid))
        {
            var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequestResponse.WriteAsJsonAsync(new ErrorResponse
            {
                Code = "INVALID_ID",
                Message = "Invalid account ID format. Must be a valid GUID."
            });
            return badRequestResponse;
        }

        // Mock data retrieval - in real implementation, this would call a service
        var account = await GetAccountByIdAsync(accountGuid);

        if (account == null)
        {
            var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
            await notFoundResponse.WriteAsJsonAsync(new ErrorResponse
            {
                Code = "NOT_FOUND",
                Message = $"Account with ID '{accountId}' was not found."
            });
            return notFoundResponse;
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(account);
        return response;
    }

    /// <summary>
    /// Retrieves all accounts with optional filtering.
    /// </summary>
    /// <param name="req">The HTTP request.</param>
    /// <returns>A list of accounts.</returns>
    [Function("ListAccounts")]
    [OpenApiOperation(operationId: "ListAccounts", tags: new[] { "Accounts" }, Summary = "List all accounts", Description = "Retrieves a list of all accounts from Dataverse with optional filtering.")]
    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
    [OpenApiParameter(name: "industry", In = ParameterLocation.Query, Required = false, Type = typeof(string), Description = "Filter accounts by industry")]
    [OpenApiParameter(name: "top", In = ParameterLocation.Query, Required = false, Type = typeof(int), Description = "Number of records to return (default: 50)")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(AccountDto[]), Description = "Successfully retrieved accounts list")]
    public async Task<HttpResponseData> ListAccounts(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "accounts")] HttpRequestData req)
    {
        _logger.LogInformation("ListAccounts function triggered");

        // Get query parameters
        var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        var industry = query["industry"];
        var topParam = query["top"];
        var top = int.TryParse(topParam, out var topValue) ? topValue : 50;

        // Mock data retrieval
        var accounts = await GetAccountsAsync(industry, top);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(accounts);
        return response;
    }

    /// <summary>
    /// Creates a new account.
    /// </summary>
    /// <param name="req">The HTTP request.</param>
    /// <returns>The created account with assigned ID.</returns>
    [Function("CreateAccount")]
    [OpenApiOperation(operationId: "CreateAccount", tags: new[] { "Accounts" }, Summary = "Create a new account", Description = "Creates a new account in Dataverse with the provided information.")]
    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(CreateAccountRequest), Required = true, Description = "Account details to create")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "application/json", bodyType: typeof(AccountDto), Description = "Account successfully created")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(ErrorResponse), Description = "Invalid request data")]
    public async Task<HttpResponseData> CreateAccount(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "accounts")] HttpRequestData req)
    {
        _logger.LogInformation("CreateAccount function triggered");

        // Parse request body
        var createRequest = await req.ReadFromJsonAsync<CreateAccountRequest>();

        if (createRequest == null || string.IsNullOrWhiteSpace(createRequest.Name))
        {
            var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequestResponse.WriteAsJsonAsync(new ErrorResponse
            {
                Code = "VALIDATION_ERROR",
                Message = "Account name is required."
            });
            return badRequestResponse;
        }

        // Mock account creation
        var newAccount = await CreateAccountAsync(createRequest);

        var response = req.CreateResponse(HttpStatusCode.Created);
        response.Headers.Add("Location", $"/api/accounts/{newAccount.Id}");
        await response.WriteAsJsonAsync(newAccount);
        return response;
    }

    /// <summary>
    /// Updates an existing account.
    /// </summary>
    /// <param name="req">The HTTP request.</param>
    /// <param name="accountId">The account identifier.</param>
    /// <returns>The updated account details.</returns>
    [Function("UpdateAccount")]
    [OpenApiOperation(operationId: "UpdateAccount", tags: new[] { "Accounts" }, Summary = "Update an account", Description = "Updates an existing account in Dataverse with the provided information.")]
    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
    [OpenApiParameter(name: "accountId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Description = "The unique identifier of the account")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(UpdateAccountRequest), Required = true, Description = "Updated account details")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(AccountDto), Description = "Account successfully updated")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(ErrorResponse), Description = "Account not found")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(ErrorResponse), Description = "Invalid account ID format")]
    public async Task<HttpResponseData> UpdateAccount(
        [HttpTrigger(AuthorizationLevel.Function, "put", Route = "accounts/{accountId}")] HttpRequestData req,
        string accountId)
    {
        _logger.LogInformation("UpdateAccount function triggered for account: {AccountId}", accountId);

        // Validate account ID format
        if (!Guid.TryParse(accountId, out Guid accountGuid))
        {
            var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequestResponse.WriteAsJsonAsync(new ErrorResponse
            {
                Code = "INVALID_ID",
                Message = "Invalid account ID format. Must be a valid GUID."
            });
            return badRequestResponse;
        }

        // Parse request body
        var updateRequest = await req.ReadFromJsonAsync<UpdateAccountRequest>();

        if (updateRequest == null)
        {
            var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequestResponse.WriteAsJsonAsync(new ErrorResponse
            {
                Code = "INVALID_REQUEST",
                Message = "Request body cannot be empty."
            });
            return badRequestResponse;
        }

        // Check if account exists
        var existingAccount = await GetAccountByIdAsync(accountGuid);
        if (existingAccount == null)
        {
            var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
            await notFoundResponse.WriteAsJsonAsync(new ErrorResponse
            {
                Code = "NOT_FOUND",
                Message = $"Account with ID '{accountId}' was not found."
            });
            return notFoundResponse;
        }

        // Mock account update
        var updatedAccount = await UpdateAccountAsync(accountGuid, updateRequest);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(updatedAccount);
        return response;
    }

    /// <summary>
    /// Deletes an account.
    /// </summary>
    /// <param name="req">The HTTP request.</param>
    /// <param name="accountId">The account identifier.</param>
    /// <returns>No content on success.</returns>
    [Function("DeleteAccount")]
    [OpenApiOperation(operationId: "DeleteAccount", tags: new[] { "Accounts" }, Summary = "Delete an account", Description = "Deletes an existing account from Dataverse.")]
    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
    [OpenApiParameter(name: "accountId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Description = "The unique identifier of the account")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NoContent, Description = "Account successfully deleted")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(ErrorResponse), Description = "Account not found")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(ErrorResponse), Description = "Invalid account ID format")]
    public async Task<HttpResponseData> DeleteAccount(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "accounts/{accountId}")] HttpRequestData req,
        string accountId)
    {
        _logger.LogInformation("DeleteAccount function triggered for account: {AccountId}", accountId);

        // Validate account ID format
        if (!Guid.TryParse(accountId, out Guid accountGuid))
        {
            var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequestResponse.WriteAsJsonAsync(new ErrorResponse
            {
                Code = "INVALID_ID",
                Message = "Invalid account ID format. Must be a valid GUID."
            });
            return badRequestResponse;
        }

        // Check if account exists
        var existingAccount = await GetAccountByIdAsync(accountGuid);
        if (existingAccount == null)
        {
            var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
            await notFoundResponse.WriteAsJsonAsync(new ErrorResponse
            {
                Code = "NOT_FOUND",
                Message = $"Account with ID '{accountId}' was not found."
            });
            return notFoundResponse;
        }

        // Mock account deletion
        await DeleteAccountAsync(accountGuid);

        return req.CreateResponse(HttpStatusCode.NoContent);
    }

    // Mock service methods (in real implementation, these would be in a separate service layer)
    private Task<AccountDto?> GetAccountByIdAsync(Guid accountId)
    {
        // Mock implementation
        var mockAccount = new AccountDto
        {
            Id = accountId,
            Name = "Contoso Ltd.",
            AccountNumber = "ACC-001234",
            Email = "contact@contoso.com",
            Phone = "+1-555-0123",
            Revenue = 1000000.00m,
            Industry = "Technology",
            CreatedOn = DateTime.UtcNow.AddDays(-30),
            ModifiedOn = DateTime.UtcNow
        };
        
        return Task.FromResult<AccountDto?>(mockAccount);
    }

    private Task<List<AccountDto>> GetAccountsAsync(string? industry, int top)
    {
        // Mock implementation
        var accounts = new List<AccountDto>
        {
            new AccountDto
            {
                Id = Guid.NewGuid(),
                Name = "Contoso Ltd.",
                AccountNumber = "ACC-001234",
                Email = "contact@contoso.com",
                Industry = "Technology",
                CreatedOn = DateTime.UtcNow.AddDays(-30),
                ModifiedOn = DateTime.UtcNow
            },
            new AccountDto
            {
                Id = Guid.NewGuid(),
                Name = "Fabrikam Inc.",
                AccountNumber = "ACC-005678",
                Email = "info@fabrikam.com",
                Industry = "Manufacturing",
                CreatedOn = DateTime.UtcNow.AddDays(-60),
                ModifiedOn = DateTime.UtcNow.AddDays(-5)
            }
        };

        if (!string.IsNullOrEmpty(industry))
        {
            accounts = accounts.Where(a => a.Industry?.Equals(industry, StringComparison.OrdinalIgnoreCase) == true).ToList();
        }

        return Task.FromResult(accounts.Take(top).ToList());
    }

    private Task<AccountDto> CreateAccountAsync(CreateAccountRequest request)
    {
        // Mock implementation
        var newAccount = new AccountDto
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            AccountNumber = request.AccountNumber,
            Email = request.Email,
            Phone = request.Phone,
            Revenue = request.Revenue,
            Industry = request.Industry,
            CreatedOn = DateTime.UtcNow,
            ModifiedOn = DateTime.UtcNow
        };

        return Task.FromResult(newAccount);
    }

    private Task<AccountDto> UpdateAccountAsync(Guid accountId, UpdateAccountRequest request)
    {
        // Mock implementation
        var updatedAccount = new AccountDto
        {
            Id = accountId,
            Name = request.Name ?? "Contoso Ltd.",
            AccountNumber = request.AccountNumber,
            Email = request.Email,
            Phone = request.Phone,
            Revenue = request.Revenue,
            Industry = request.Industry,
            CreatedOn = DateTime.UtcNow.AddDays(-30),
            ModifiedOn = DateTime.UtcNow
        };

        return Task.FromResult(updatedAccount);
    }

    private Task DeleteAccountAsync(Guid accountId)
    {
        // Mock implementation
        return Task.CompletedTask;
    }
}
