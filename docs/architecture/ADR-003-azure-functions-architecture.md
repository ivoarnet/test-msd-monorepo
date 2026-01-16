# ADR-003: Azure Functions Architecture for Dynamics 365 / Dataverse

**Status**: Open  
**Date**: 2024-01-16  
**Related**: ADR-001 (Repository Strategy), ADR-002 (Folder Structure)

---

## Context

When developing Azure Functions for Dynamics 365 / Dataverse integration, teams face a critical architectural choice between two fundamentally different approaches:

1. **Clean Architecture / Layered approach** - Domain-centric with strong separation of concerns
2. **Feature-oriented / Function-app-centric approach** - Vertical slice with pragmatic simplicity

This decision impacts:
- Testability and maintainability
- Time-to-market and developer productivity
- Reusability across applications
- Coupling to Azure Functions and Dataverse
- Long-term evolution and scalability

We need clear guidance on when to use each approach and how they can coexist in our monorepo.

---

## Approach A: Layered / Clean Architecture

### Description

This structure follows **Clean Architecture / Hexagonal Architecture** principles with strong separation between domain logic, infrastructure, and hosting concerns.

### Structure

```
PriceCalculatorFunctions/
├── Core/
│   ├── Abstractions/
│   │   ├── IPriceListProductsRepository.cs
│   │   └── IPriceCalculator.cs
│   ├── Models/
│   │   ├── PriceListProduct.cs
│   │   └── CalculationResult.cs
│   └── Services/
│       ├── PriceCalculator.cs
│       └── DiscountHelper.cs
│
├── Infrastructure/
│   ├── Dataverse/
│   │   ├── DataverseServiceClient.cs
│   │   └── DataverseModels/
│   ├── Repositories/
│   │   └── PriceListProductsRepository.cs
│   └── Mapping/
│       └── PriceListProductMapper.cs
│
├── API/
│   ├── Functions/
│   │   ├── CalculatePriceFunction.cs
│   │   └── GetPriceListFunction.cs
│   ├── Program.cs
│   └── Startup.cs
│
└── API.Tests/
    ├── Core/
    │   └── PriceCalculatorTests.cs
    └── Infrastructure/
        └── RepositoryTests.cs
```

### Key Characteristics

- **Multiple projects** in one solution:
  - **Core** - Abstractions (interfaces), domain models, business logic
  - **Infrastructure** - Dataverse access, repository implementations, data mapping
  - **API / Function App** - Thin orchestration layer
  - **API Tests** - Comprehensive test coverage
- **Strong separation** between:
  - Domain logic (testable without external dependencies)
  - Infrastructure concerns (Dataverse, repositories)
  - Hosting model (Azure Functions)
- **Dependency flow**: API → Infrastructure → Core (dependencies point inward)
- **Testability**: Core logic tested in isolation, infrastructure tested with integration tests

### When to Use

✅ **Approach A is better when:**

- You have **non-trivial business logic** (pricing engines, rule engines, calculations)
- Logic must be:
  - Testable without Dataverse
  - Reusable (e.g., future APIs, plugins, batch jobs, canvas apps)
- You want to **protect your domain from Dataverse/Azure coupling**
- The solution is expected to **live for years and evolve**
- Multiple teams or applications will consume the same business logic
- You need to support different hosting models (Functions, plugins, standalone services)

### Advantages

✅ **Excellent testability** - Core logic tested without external dependencies  
✅ **High reusability** - Business logic can be used in multiple contexts  
✅ **Low coupling** to Azure Functions and Dataverse  
✅ **Clear boundaries** - Easy to understand where logic belongs  
✅ **Maintainable** - Changes to infrastructure don't affect core logic  
✅ **Evolvable** - Can easily swap implementations or add new hosting models

### Disadvantages

❌ **Higher cognitive load** - More projects and abstractions to navigate  
❌ **Slower initial development** - More setup and ceremony required  
❌ **Overhead for simple cases** - Over-engineered for basic CRUD operations  
❌ **Learning curve** - Team needs to understand Clean Architecture principles

---

## Approach B: Feature-oriented / Function-app-centric

### Description

This is a **vertical slice / feature-first** structure inside a Function App, optimizing for speed and simplicity.

### Structure

```
IntegrationApi/
├── Functions/
│   ├── AccountFunctions.cs
│   ├── ContactFunctions.cs
│   └── OrderFunctions.cs
│
├── Models/
│   ├── AccountDto.cs
│   ├── ContactDto.cs
│   ├── CreateAccountRequest.cs
│   ├── UpdateAccountRequest.cs
│   └── ErrorResponse.cs
│
├── Services/
│   ├── DataverseService.cs
│   └── ValidationService.cs
│
├── Shared/
│   └── Extensions/
│       └── HttpRequestExtensions.cs
│
├── host.json
├── Program.cs
└── IntegrationApi.csproj
```

### Key Characteristics

- **One main Function App** (e.g., `IntegrationApi`)
- **Grouping by feature or API boundary**:
  - `Functions/AccountFunctions.cs` - All account-related endpoints
  - `Models/AccountDto.cs` - Request/response DTOs next to the function
- **Optional shared folder** for cross-cutting code
- **Direct Dataverse references** in the same project
- **Tests live alongside features** (or in parallel test project)
- **Minimal abstraction** - Interfaces only when needed

### When to Use

✅ **Approach B is better when:**

- The Function App is mainly:
  - **CRUD operations**
  - **Integration façade** / API wrapper
  - **Glue code** between systems
- **Business logic is thin** or non-existent
- **Team size is small** or mixed-skill
- You want **fast onboarding and low ceremony**
- Each Function App is **independently deployable**
- Time-to-market is critical
- The API is primarily exposing Dataverse entities

### Advantages

✅ **Lower cognitive load** - Simpler structure, easier to navigate  
✅ **Faster initial development** - Less ceremony, get started quickly  
✅ **Easy onboarding** - New developers productive immediately  
✅ **Clear feature grouping** - All related code in one place  
✅ **Pragmatic** - Right amount of structure for simple integrations  
✅ **Deployment efficiency** - Single unit to deploy and monitor

### Disadvantages

❌ **Moderate coupling** to Azure Functions and Dataverse  
❌ **Limited reusability** - Logic tied to Function App  
❌ **Integration-heavy testing** - Harder to test without Dataverse  
❌ **Less evolvable** - Changes to business logic affect entire app  
❌ **Not suitable for complex logic** - Can become messy as complexity grows

---

## Comparison

### Side-by-Side Analysis

| Aspect | Approach A – Clean Architecture | Approach B – Feature-oriented |
|--------|--------------------------------|------------------------------|
| **Primary focus** | Domain & business logic | API / integration surface |
| **Coupling to Azure Functions** | Very low | Moderate |
| **Dataverse dependency** | Isolated in Infrastructure | Often referenced directly |
| **Testability** | Excellent (domain & infra separately) | Good, but more integration-heavy |
| **Cognitive load** | Higher | Lower |
| **Time-to-market** | Slower initially | Faster |
| **Reuse across apps** | Very strong | Limited |
| **Typical use case** | Core business logic, pricing, calculations | CRUD APIs, integrations, glue code |
| **Team size** | Works well at any scale | Best for small-medium teams |
| **Maintenance** | Easier for complex logic | Easier for simple APIs |

---

## Decision Guidance

### ✅ There is no universally better structure – it depends on intent

The "right" architecture depends on:
1. **Complexity of business logic**
2. **Need for reusability**
3. **Team size and skills**
4. **Time constraints**
5. **Expected lifespan of the solution**

### Decision Tree

```
Do you have complex business logic?
├─ YES → Does it need to be reused elsewhere?
│   ├─ YES → Use Approach A (Clean Architecture)
│   └─ NO → Consider Approach A for testability
└─ NO → Is it mainly CRUD/integration?
    ├─ YES → Use Approach B (Feature-oriented)
    └─ NO → Re-evaluate complexity assessment
```

---

## Recommended Hybrid Approach

For Dynamics 365 / Dataverse projects at scale, the **best real-world setup is often a hybrid**:

### Hybrid Structure

```
functions/
├── src/
│   ├── PriceCalculator.Core/           # Clean Architecture
│   │   ├── Abstractions/
│   │   ├── Models/
│   │   └── Services/
│   │
│   ├── PriceCalculator.Infrastructure/ # Clean Architecture
│   │   ├── Dataverse/
│   │   └── Repositories/
│   │
│   ├── PriceCalculatorFunctions/       # Thin API layer
│   │   ├── Functions/
│   │   │   └── CalculatePriceFunction.cs
│   │   └── Program.cs
│   │
│   └── IntegrationApi/                 # Feature-oriented
│       ├── Functions/
│       │   ├── AccountFunctions.cs
│       │   └── ContactFunctions.cs
│       └── Models/
│
└── shared/
    └── Common/
        └── Extensions/
```

### Hybrid Principles

1. **Core domain & logic** → Clean Architecture (Approach A)
   - Pricing engines
   - Calculation services
   - Business rule engines
   - Shared D365 logic

2. **Integration APIs / façade Function Apps** → Feature-oriented (Approach B)
   - CRUD endpoints
   - Integration wrappers
   - Event handlers
   - Simple orchestration

3. **Function Apps reference Core** - Keep Functions thin:
   - Input validation
   - DTO mapping
   - Orchestration
   - Error handling

### Example: Hybrid Implementation

```csharp
// PriceCalculator.Core (Clean Architecture)
namespace PriceCalculator.Core.Services
{
    public interface IPriceCalculator
    {
        Task<CalculationResult> CalculatePriceAsync(PriceRequest request);
    }
    
    public class PriceCalculator : IPriceCalculator
    {
        // Pure business logic, no Azure/Dataverse dependencies
    }
}

// PriceCalculatorFunctions (Thin API layer)
namespace PriceCalculatorFunctions.Functions
{
    public class CalculatePriceFunction
    {
        private readonly IPriceCalculator _calculator;
        
        public CalculatePriceFunction(IPriceCalculator calculator)
        {
            _calculator = calculator;
        }
        
        [Function("CalculatePrice")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            // Thin orchestration: validate, map, call core, return
            var request = await req.ReadFromJsonAsync<PriceRequestDto>();
            var domainRequest = MapToDomain(request);
            var result = await _calculator.CalculatePriceAsync(domainRequest);
            return new OkObjectResult(MapToDto(result));
        }
        
        private PriceRequest MapToDomain(PriceRequestDto dto) { /* mapping logic */ }
        private PriceResultDto MapToDto(CalculationResult result) { /* mapping logic */ }
    }
}

// IntegrationApi (Feature-oriented for simple CRUD)
namespace IntegrationApi.Functions
{
    public class AccountFunctions
    {
        private readonly DataverseService _dataverseService;
        
        public AccountFunctions(DataverseService dataverseService)
        {
            _dataverseService = dataverseService;
        }
        
        [Function("GetAccount")]
        public async Task<IActionResult> GetAccount(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req,
            string accountId)
        {
            // Direct Dataverse access for simple CRUD
            var account = await _dataverseService.GetAccountAsync(accountId);
            return new OkObjectResult(account);
        }
    }
}
```

---

## Implementation Guidelines

### For Approach A (Clean Architecture)

1. **Define Core interfaces first** - Start with abstractions
2. **Keep Core pure** - No external dependencies (except common libraries)
3. **Implement Infrastructure** - Repositories, Dataverse clients
4. **Wire up DI in Function App** - Register services in Program.cs
5. **Keep Functions thin** - Only orchestration and mapping

### For Approach B (Feature-oriented)

1. **Group by feature** - Related endpoints together
2. **Use services for complexity** - Extract logic when it grows
3. **Keep models close** - DTOs next to functions that use them
4. **Add abstractions when needed** - Don't over-engineer upfront
5. **Refactor to Approach A if complexity increases**

### Dependency Injection Setup

```csharp
// Program.cs (both approaches)
var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        // Approach A: Register Core and Infrastructure
        services.AddScoped<IPriceCalculator, PriceCalculator>();
        services.AddScoped<IPriceRepository, DataversePriceRepository>();
        
        // Approach B: Register services directly
        services.AddScoped<DataverseService>();
        services.AddScoped<ValidationService>();
        
        // Common
        services.AddApplicationInsightsTelemetryWorkerService();
        services.AddHttpClient<IDataverseClient, DataverseClient>();
    })
    .Build();

await host.RunAsync();
```

---

## Migration Path

### From Approach B to Approach A

When a feature-oriented Function App grows complex:

1. **Identify business logic** to extract
2. **Create Core project** with abstractions
3. **Move business logic** to Core (keeping tests)
4. **Create Infrastructure project** with implementations
5. **Refactor Function App** to thin orchestration layer
6. **Update DI registration** to wire up new structure

### Indicators You Need to Migrate

- Business logic exceeds 200 lines in a single function
- Same logic duplicated across multiple functions
- Need to reuse logic in other applications
- Testing requires complex mocking of Dataverse
- Team struggles to understand function responsibilities

---

## Alignment with Best Practices

### Clean Architecture for Azure Functions

This ADR aligns with Microsoft's Clean Architecture guidance:
- Separation of concerns
- Dependency inversion
- Testable business logic
- Infrastructure isolation

**References:**
- [Clean Architecture with Azure Functions](https://learn.microsoft.com/en-us/azure/architecture/serverless/code)
- [Azure Functions best practices](https://learn.microsoft.com/en-us/azure/azure-functions/functions-best-practices)

### Azure Functions Best Practices

Both approaches support Microsoft's recommendations:
- Group related functions in one Function App
- Use dependency injection
- Keep functions focused and small
- Separate configuration from code
- Use managed identities

**References:**
- [Organizing Azure Functions](https://learn.microsoft.com/en-us/azure/azure-functions/functions-reference)

---

## Verdict

### Short Summary

- **Approach A** is **architecturally superior** for complex, long-lived Dynamics logic
- **Approach B** is **operationally superior** for simple APIs and integrations
- For **pricing, rules, and shared logic** → **Approach A wins**
- For **CRUD & integration endpoints** → **Approach B wins**
- **Hybrid approach** provides the best of both worlds at scale

### Practical Recommendation

1. **Start with Approach B** for new Function Apps with simple requirements
2. **Use Approach A** from the start for known complex business logic
3. **Refactor to Approach A** when complexity grows
4. **Maintain both approaches** in monorepo for different use cases
5. **Share Core logic** across multiple Function Apps when appropriate

---

## Related Decisions

- **ADR-001**: Monorepo strategy supports both approaches in one repository
- **ADR-002**: Folder structure accommodates both architectural styles
- Functions folder can contain both Clean Architecture solutions and feature-oriented apps

---

## Future Considerations

### Questions for Further Discussion

1. Should we create a **shared Core library** that multiple Function Apps reference?
2. How do we handle **versioning** when Core logic is shared?
3. Should we establish a **threshold** (LOC, complexity) for mandatory Approach A?
4. How do we **document architectural choices** at the Function App level?
5. Should we create **templates** for both approaches?

### Next Steps

1. Map existing `IntegrationApi` to this framework
2. Create example implementations of both approaches
3. Develop DI setup guide for hybrid scenarios
4. Review trade-offs with Dataverse plugin vs Function App decisions
5. Update developer guide with architecture decision tree

---

## References

### Architecture Patterns
- [Clean Architecture (Robert C. Martin)](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Hexagonal Architecture](https://alistair.cockburn.us/hexagonal-architecture/)
- [Vertical Slice Architecture](https://www.jimmybogard.com/vertical-slice-architecture/)

### Azure Functions
- [Azure Functions Clean Architecture](https://learn.microsoft.com/en-us/azure/architecture/serverless/code)
- [Azure Functions Best Practices](https://learn.microsoft.com/en-us/azure/azure-functions/functions-best-practices)
- [Organizing Azure Functions](https://learn.microsoft.com/en-us/azure/azure-functions/functions-reference)

### Related ADRs
- ADR-001: Repository Strategy for Dynamics 365 Development
- ADR-002: Monorepo Folder Structure

---

## Conclusion

This ADR provides clear guidance on choosing between Clean Architecture and feature-oriented approaches for Azure Functions in Dynamics 365 development. By understanding the strengths and use cases of each approach, and leveraging a hybrid strategy when appropriate, teams can make informed architectural decisions that balance:

- **Development speed** vs **long-term maintainability**
- **Simplicity** vs **reusability**
- **Pragmatism** vs **architectural purity**

The monorepo structure supports both approaches, allowing teams to choose the right tool for each job while maintaining consistency where it matters.

---

**Status**: Open  
**Feedback welcome**: Please contribute your experiences and suggestions for this architectural approach.
