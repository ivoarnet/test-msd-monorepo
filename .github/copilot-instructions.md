# GitHub Copilot Instructions for Dynamics 365 Monorepo

This document provides context and guidelines for GitHub Copilot when working with this Dynamics 365 / Power Platform monorepo.

---

## 🎯 Repository Overview

This is an **enterprise-grade monorepo** for Dynamics 365 / Dataverse development containing:

- **Plugins** (.NET 8+): Server-side business logic for Dataverse
- **PCF Components** (TypeScript/React): Custom UI controls for model-driven apps
- **Client Scripts** (TypeScript): Form and ribbon customizations
- **Azure Functions** (.NET/Node.js): Integration APIs and background jobs
- **Solutions**: Dataverse solution exports (XML)
- **Terraform**: Infrastructure as Code for Azure resources

---

## 📁 Repository Structure

```
/
├── .github/              # GitHub Actions workflows and templates
├── docs/                 # Documentation (architecture, guides, standards)
│   ├── architecture/     # ADRs (Architecture Decision Records)
│   ├── developer-guide/  # Developer onboarding and guides
│   └── standards/        # Coding standards for .NET, TypeScript, Terraform
├── infra/
│   └── terraform/        # Terraform configurations
├── src/
│   ├── client-scripts/   # TypeScript form/ribbon scripts
│   ├── functions/        # Azure Functions
│   ├── pcf/              # PowerApps Component Framework controls
│   ├── plugins/          # .NET Dataverse plugins
│   └── solutions/        # Dataverse solution exports
```

---

## 🎨 Coding Standards

### .NET Plugins

**Location**: `/src/plugins/`  
**Standards**: [docs/standards/dotnet-coding-standards.md](/docs/standards/dotnet-coding-standards.md)

**Key Guidelines**:
- All plugins inherit from `PluginBase` class
- Use early-bound types (strongly-typed entities)
- Always validate input parameters and context
- Include extensive tracing for production debugging
- Never log sensitive data (passwords, API keys, PII)
- Use specific `ColumnSet` in queries (avoid `AllColumns`)
- Use `ExecuteMultipleRequest` for bulk operations
- Follow naming: `{Entity}{Event}Plugin` (e.g., `AccountCreatePlugin`)

**Example Plugin Structure**:
```csharp
public class AccountCreatePlugin : PluginBase
{
    protected override void ExecutePlugin(
        IServiceProvider serviceProvider,
        ITracingService tracingService,
        IPluginExecutionContext context)
    {
        tracingService.Trace("Starting account validation");
        
        if (context.InputParameters == null || !context.InputParameters.Contains("Target"))
        {
            throw new InvalidPluginExecutionException("Target parameter is missing.");
        }

        var account = GetTargetEntity<Account>(context);
        ValidateAccount(account, tracingService);
    }
}
```

### TypeScript (PCF & Client Scripts)

**Location**: `/src/pcf/` and `/src/client-scripts/`  
**Standards**: [docs/standards/typescript-coding-standards.md](/docs/standards/typescript-coding-standards.md)

**Key Guidelines**:
- Use strong typing (avoid `any`)
- Use async/await instead of callbacks
- Always get FormContext from ExecutionContext
- Use type-safe attribute access with generics
- Implement comprehensive error handling
- Use JSDoc comments for public functions
- Follow naming: camelCase for functions, PascalCase for classes/interfaces
- Use Xrm API TypeScript definitions

**Example Form Script**:
```typescript
export function onLoad(executionContext: Xrm.Events.EventContext): void {
    const formContext = executionContext.getFormContext();
    
    // Type-safe attribute access
    const nameAttribute = formContext.getAttribute<Xrm.Attributes.StringAttribute>("name");
    if (nameAttribute) {
        nameAttribute.setRequiredLevel("required");
    }
}
```

**Example PCF Component**:
```typescript
export class DataGrid implements ComponentFramework.StandardControl<IInputs, IOutputs> {
    private _context: ComponentFramework.Context<IInputs>;
    private _container: HTMLDivElement;

    public init(
        context: ComponentFramework.Context<IInputs>,
        notifyOutputChanged: () => void,
        state: ComponentFramework.Dictionary,
        container: HTMLDivElement
    ): void {
        this._context = context;
        this._container = container;
    }

    public updateView(context: ComponentFramework.Context<IInputs>): void {
        this._context = context;
    }
}
```

### Terraform

**Location**: `/infra/terraform/`

**Key Guidelines**:
- Use modules for reusable infrastructure components
- Separate environments (dev, test, prod)
- Use variables for configuration
- Include meaningful descriptions for resources
- Follow naming conventions for Azure resources

---

## 🏗️ Architecture Context

### Dataverse / Dynamics 365 Concepts

When working with this repository, understand these core concepts:

- **Entity**: A table in Dataverse (e.g., Account, Contact, custom entities)
- **Plugin**: Server-side code that runs on Dataverse events (Create, Update, Delete, etc.)
- **Plugin Pipeline**: Pre-validation (10), Pre-operation (20), Main (30), Post-operation (40)
- **Form Context**: Client-side API for accessing form data and UI
- **Web API**: REST API for CRUD operations on Dataverse entities
- **PCF**: PowerApps Component Framework for custom UI controls
- **Solution**: Package containing customizations (entities, forms, plugins, etc.)

### Common Patterns

**Plugin Pattern - Retrieve Related Data**:
```csharp
var query = new QueryExpression("contact")
{
    ColumnSet = new ColumnSet("firstname", "lastname", "emailaddress1"),
    Criteria = new FilterExpression
    {
        Conditions =
        {
            new ConditionExpression("parentcustomerid", ConditionOperator.Equal, accountId),
            new ConditionExpression("statecode", ConditionOperator.Equal, 0) // Active only
        }
    }
};
var contacts = service.RetrieveMultiple(query).Entities;
```

**Client Script Pattern - Web API Query**:
```typescript
async function getActiveContacts(accountId: string): Promise<any[]> {
    const query = `?$select=firstname,lastname,emailaddress1` +
                  `&$filter=_parentcustomerid_value eq ${accountId} and statecode eq 0` +
                  `&$orderby=lastname asc`;
    
    const result = await Xrm.WebApi.retrieveMultipleRecords("contact", query);
    return result.entities;
}
```

---

## 🚀 Building and Testing

### Plugins (.NET)

```bash
# Navigate to plugins directory
cd src/plugins

# Restore dependencies
dotnet restore

# Build
dotnet build

# Run tests
dotnet test
```

### PCF Components

```bash
# Navigate to PCF directory
cd src/pcf

# Install dependencies
npm install

# Build
npm run build

# Watch mode
npm run watch
```

### Client Scripts

```bash
# Navigate to client scripts directory
cd src/client-scripts

# Install dependencies
npm install

# Build
npm run build

# Run tests
npm test

# Lint
npm run lint
```

### Azure Functions

```bash
# Navigate to functions directory
cd src/functions

# Restore dependencies
dotnet restore

# Build
dotnet build

# Run locally
func start

# Run tests
dotnet test
```

---

## 📚 Key Documentation

When suggesting code or answering questions, reference these documents:

- **Architecture Decisions**: `/docs/architecture/ADR-*.md`
- **.NET Standards**: `/docs/standards/dotnet-coding-standards.md`
- **TypeScript Standards**: `/docs/standards/typescript-coding-standards.md`
- **Component READMEs**: Each component has a README with specific guidance

---

## 🔒 Security Considerations

**Always Keep in Mind**:
- Never log sensitive data (passwords, API keys, credit cards, PII)
- Sanitize user input before using in queries or operations
- Use parameterized queries to prevent injection attacks
- Validate permissions before performing privileged operations
- Use system context (`CreateOrganizationService(null)`) only when necessary
- Store secrets in Azure Key Vault or secure configuration, never in code

**Example - Input Sanitization**:
```csharp
// Basic example - for production, use established libraries
private string SanitizeInput(string input)
{
    if (string.IsNullOrWhiteSpace(input))
    {
        return string.Empty;
    }
    
    // For HTML context, use proper encoding
    // System.Net.WebUtility.HtmlEncode(input) or
    // System.Web.HttpUtility.HtmlEncode(input)
    
    // For queries, use parameterized queries or QueryExpression
    // Never concatenate user input directly into FetchXML or queries
    return input.Trim();
}

// Better: Use QueryExpression with proper conditions
var query = new QueryExpression("account")
{
    Criteria = new FilterExpression
    {
        Conditions =
        {
            new ConditionExpression("name", ConditionOperator.Equal, userInput) // Safe
        }
    }
};
```

---

## 🎯 Code Suggestions Guidelines

### When Suggesting Plugin Code:

1. Always inherit from `PluginBase`
2. Include input validation at the start
3. Add tracing statements for debugging
4. Use early-bound types when possible
5. Include XML documentation comments
6. Handle exceptions with meaningful messages
7. Consider performance (minimize queries, use batch operations)

### When Suggesting Client Scripts:

1. Always get FormContext from ExecutionContext
2. Use TypeScript types and interfaces
3. Include null/undefined checks
4. Use async/await for Web API calls
5. Add JSDoc comments
6. Handle errors with user-friendly messages (use `Xrm.Navigation.openErrorDialog`)
7. Never use `alert()` - use Xrm dialogs instead

### When Suggesting PCF Components:

1. Follow PCF lifecycle methods (`init`, `updateView`, `getOutputs`, `destroy`)
2. Use TypeScript and React best practices
3. Leverage Fluent UI components when appropriate
4. Handle component state properly
5. Consider accessibility (ARIA labels, keyboard navigation)

### When Suggesting Terraform:

1. Use modules for reusability
2. Separate by environment (dev, test, prod)
3. Use variables for configuration
4. Include resource tags for cost tracking
5. Follow Azure naming conventions

---

## 🛠️ Common Tasks & Patterns

### Creating a New Plugin

1. Create class inheriting from `PluginBase`
2. Name it `{Entity}{Event}Plugin`
3. Implement `ExecutePlugin` method
4. Add input validation
5. Add tracing
6. Add error handling
7. Write unit tests
8. Register in assembly

### Creating a New PCF Component

1. Navigate to `/src/pcf/`
2. Use `pac pcf init` to scaffold component
3. Implement required lifecycle methods
4. Add TypeScript types for inputs/outputs
5. Build with `npm run build`
6. Test in Dynamics 365 environment

### Creating a New Client Script

1. Create TypeScript file in `/src/client-scripts/`
2. Export functions for events (onLoad, onChange, onSave)
3. Use proper typing with Xrm definitions
4. Build and bundle with npm
5. Upload to Dynamics 365

---

## 📊 CI/CD Context

This repository uses GitHub Actions for CI/CD:

- **plugins-ci.yml**: Builds and tests .NET plugins
- **pcf-ci.yml**: Builds and packages PCF components
- **functions-ci.yml**: Builds, tests, and deploys Azure Functions
- **terraform-plan.yml**: Validates infrastructure changes

When suggesting changes that affect CI/CD, consider:
- Build commands must work in CI environment
- Tests must be reliable and not flaky
- Dependencies must be properly declared
- Environment-specific values use variables/secrets

---

## 💡 Best Practices for AI Assistance

### DO:
✅ Reference existing patterns in the codebase  
✅ Follow established naming conventions  
✅ Include error handling and validation  
✅ Add appropriate logging/tracing  
✅ Consider performance implications  
✅ Write testable code  
✅ Include documentation comments  
✅ Follow the coding standards in `/docs/standards/`

### DON'T:
❌ Use `any` type in TypeScript without good reason  
❌ Log sensitive data  
❌ Create plugins without input validation  
❌ Use `AllColumns` in Dataverse queries  
❌ Ignore error handling  
❌ Use hardcoded environment-specific values  
❌ Skip XML/JSDoc documentation  
❌ Use `alert()` in client scripts (use Xrm dialogs)

---

## 🎓 Learning Resources

For deeper understanding of concepts:

- **Dataverse Plugins**: https://learn.microsoft.com/power-apps/developer/data-platform/plug-ins
- **Client API Reference**: https://learn.microsoft.com/power-apps/developer/model-driven-apps/clientapi/reference
- **PCF Documentation**: https://learn.microsoft.com/power-apps/developer/component-framework/overview
- **Xrm TypeScript Definitions**: https://github.com/DefinitelyTyped/DefinitelyTyped/tree/master/types/xrm
- **Azure Functions**: https://learn.microsoft.com/azure/azure-functions/

---

## 🤝 Contributing Workflow

When suggesting changes, keep in mind the team's workflow:

1. **Branch naming**: `feature/*`, `bugfix/*`, `hotfix/*`
2. **Commits**: Use Conventional Commits format
   - `feat(plugins): add account validation logic`
   - `fix(pcf): resolve data binding issue`
   - `docs(standards): update TypeScript guidelines`
3. **Code review**: Changes require CODEOWNERS approval
4. **Testing**: All tests must pass before merging
5. **Documentation**: Update relevant docs when changing behavior

---

## 📝 Component-Specific Notes

### Plugins (`/src/plugins/`)
- Target: .NET 8+
- Dependencies: Microsoft.CrmSdk.CoreAssemblies
- Testing: MSTest or xUnit
- Deployment: Registered via Plugin Registration Tool

### PCF (`/src/pcf/`)
- Target: TypeScript, React, Fluent UI
- Build: `npm run build` creates solution-ready package
- Testing: Jest for unit tests
- Deployment: Imported as solution component

### Client Scripts (`/src/client-scripts/`)
- Target: TypeScript (compiled to JavaScript)
- Build: Webpack bundles scripts
- Testing: Jest with Xrm mocks
- Deployment: Uploaded as web resources

### Azure Functions (`/src/functions/`)
- Target: .NET or Node.js
- Testing: xUnit or Jest
- Deployment: Azure Functions deployment via CI/CD

### Terraform (`/infra/terraform/`)
- Target: Azure resources
- State: Stored in Azure Storage
- Deployment: terraform plan/apply via CI/CD

---

## 🔍 Context Awareness

When analyzing code or suggesting changes:

1. **Check the component type** by file location
2. **Reference the appropriate coding standards** document
3. **Consider the execution context** (server-side plugin vs client-side script)
4. **Be aware of API limitations** (Dataverse API vs Web API vs Organization Service)
5. **Think about the deployment model** (plugins registered, scripts uploaded, etc.)

---

**This document helps GitHub Copilot understand the Dynamics 365 monorepo structure and provide contextually appropriate suggestions. For detailed standards, always refer to `/docs/standards/` documentation.**
