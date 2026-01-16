# ADR-002: Monorepo Folder Structure

**Status**: Accepted  
**Date**: 2024-01-15  
**Related**: ADR-001 (Repository Strategy)

---

## Context

Define a clear, scalable folder structure that:
- Separates concerns by technology stack
- Supports GitHub Copilot with clear context
- Enables path-based CI/CD triggers
- Follows Microsoft and community best practices

---

## Decision

### Root-Level Structure

```
/
├── .github/               # GitHub-specific configuration
│   ├── workflows/         # CI/CD pipelines
│   ├── CODEOWNERS         # Code ownership rules
│   └── PULL_REQUEST_TEMPLATE.md
│
├── plugins/               # .NET Dataverse plugins
│   ├── src/               # Source code
│   ├── tests/             # Unit tests
│   └── README.md
│
├── pcf/                   # PowerApps Component Framework
│   ├── components/        # Individual PCF components
│   ├── shared/            # Shared utilities
│   └── README.md
│
├── client-scripts/        # Form & ribbon JavaScript/TypeScript
│   ├── src/               # Source code
│   ├── tests/             # Jest/Jasmine tests
│   └── README.md
│
├── functions/             # Azure Functions
│   ├── src/               # Function apps
│   ├── shared/            # Shared function utilities
│   ├── tests/             # Integration tests
│   └── README.md
│
├── solutions/             # Dataverse solution exports
│   ├── managed/           # Managed solutions
│   ├── unmanaged/         # Source-controlled unmanaged
│   └── README.md
│
├── terraform/             # Infrastructure as Code
│   ├── environments/      # Environment-specific configs
│   ├── modules/           # Reusable Terraform modules
│   └── README.md
│
├── docs/                  # Documentation
│   ├── architecture/      # ADRs and design docs
│   ├── developer-guide/   # Onboarding & how-tos
│   └── standards/         # Coding standards
│
├── scripts/               # Helper scripts (build, deploy)
│   ├── build/
│   └── deploy/
│
├── .gitignore
├── .editorconfig          # Cross-editor settings
├── LICENSE
└── README.md              # Repository overview
```

---

## Design Principles

### 1. Technology-Based Top-Level Folders
Each major technology stack gets a top-level folder:
- **plugins**: .NET ecosystem
- **pcf**: Node.js/React ecosystem  
- **client-scripts**: Browser JavaScript/TypeScript
- **functions**: Azure Functions (Node.js/.NET)

**Rationale**: Clear separation improves Copilot context and CI/CD path filtering

### 2. Consistent Sub-Structure
Each component folder follows a pattern:
```
<component>/
├── src/           # Source code
├── tests/         # Tests
├── docs/          # Component-specific docs (if extensive)
├── README.md      # Component overview
└── <config-files> # package.json, *.csproj, etc.
```

### 3. Shared Code Placement
- **Within component**: `<component>/shared/` (e.g., `plugins/src/Shared/`)
- **Cross-component**: Consider extracting to separate repo or package later

### 4. Environment Separation
Configuration is environment-agnostic in source; environment-specific values live in:
- **Terraform**: `terraform/environments/{dev,test,prod}/`
- **Azure Functions**: App Settings (not in repo)
- **Dataverse**: Solution environment variables

---

## Naming Conventions

| Context | Convention | Example |
|---------|-----------|---------|
| **Folders (root)** | lowercase-kebab-case | `client-scripts/` |
| **Folders (nested)** | PascalCase or camelCase per language | `src/Helpers/` (.NET), `src/utils/` (JS) |
| **.NET Projects** | PascalCase with namespace | `Contoso.Plugins.csproj` |
| **.NET Classes** | PascalCase | `AccountPlugin.cs` |
| **TypeScript/JS Files** | camelCase or PascalCase | `accountForm.ts`, `StringUtils.ts` |
| **PCF Components** | PascalCase | `DataGrid/` |
| **Workflows** | kebab-case | `plugins-ci.yml` |

---

## Component Details

### Plugins Structure
```
plugins/
├── src/
│   ├── Contoso.Plugins/           # Main plugin project
│   │   ├── Account/
│   │   │   ├── AccountCreatePlugin.cs
│   │   │   └── AccountUpdatePlugin.cs
│   │   ├── Shared/
│   │   │   ├── BasePlugin.cs
│   │   │   └── Extensions.cs
│   │   └── Contoso.Plugins.csproj
│   └── Contoso.CustomApis/        # Custom API implementations
│       └── Contoso.CustomApis.csproj
├── tests/
│   └── Contoso.Plugins.Tests/
│       ├── AccountPluginTests.cs
│       └── Contoso.Plugins.Tests.csproj
├── .editorconfig
├── Contoso.Plugins.sln
└── README.md
```

### PCF Structure
```
pcf/
├── components/
│   ├── DataGrid/                  # Individual component
│   │   ├── DataGrid/              # Generated PCF structure
│   │   ├── package.json
│   │   └── README.md
│   └── FileUploader/
├── shared/
│   └── utils/                     # Shared TypeScript utilities
├── package.json                   # Root package (workspace)
└── README.md
```

### Client Scripts Structure
```
client-scripts/
├── src/
│   ├── forms/
│   │   ├── account.ts             # Account form logic
│   │   └── contact.ts
│   ├── ribbons/
│   │   └── accountRibbon.ts
│   └── utils/
│       └── webApiHelper.ts        # Xrm.WebApi wrappers
├── tests/
│   └── forms/
│       └── account.test.ts
├── tsconfig.json
├── package.json
└── README.md
```

### Functions Structure
```
functions/
├── src/
│   ├── IntegrationApi/            # Function app
│   │   ├── Functions/
│   │   │   ├── GetCustomerFunction.cs
│   │   │   └── SyncOrderFunction.cs
│   │   ├── Services/
│   │   │   └── DataverseService.cs
│   │   ├── host.json
│   │   └── IntegrationApi.csproj
│   └── shared/                    # Shared across function apps
│       └── Models/
├── tests/
│   └── IntegrationApi.Tests/
├── local.settings.json            # Local dev (gitignored)
└── README.md
```

### Terraform Structure
```
terraform/
├── environments/
│   ├── dev/
│   │   ├── main.tf
│   │   ├── variables.tf
│   │   └── terraform.tfvars
│   ├── test/
│   └── prod/
├── modules/
│   ├── function-app/              # Reusable modules
│   │   ├── main.tf
│   │   ├── variables.tf
│   │   └── outputs.tf
│   ├── api-management/
│   └── dataverse-environment/
└── README.md
```

---

## GitHub-Specific Files

### CODEOWNERS
```
# Each component has designated owners
/plugins/           @team-backend
/pcf/               @team-frontend
/client-scripts/    @team-frontend
/functions/         @team-integrations
/terraform/         @team-devops
/docs/              @team-leads
```

### Workflow Organization
```
.github/workflows/
├── plugins-ci.yml           # Triggered by plugins/** changes
├── pcf-ci.yml               # Triggered by pcf/** changes
├── functions-ci.yml         # Triggered by functions/** changes
├── client-scripts-ci.yml    # Triggered by client-scripts/** changes
├── terraform-plan.yml       # Triggered by terraform/** changes
└── _reusable-build.yml      # Shared workflow steps
```

---

## Copilot Optimization

### 1. README Hierarchy
- **Root README**: High-level overview, quick start
- **Component README**: Technology-specific setup and patterns
- **docs/**: In-depth guides and standards

### 2. Inline Documentation
- **Plugins**: XML doc comments on all public members
- **TypeScript**: JSDoc for functions and interfaces
- **Functions**: Summary comments on trigger bindings

### 3. Example Code
Each component includes `/examples/` or `/templates/`:
- `plugins/examples/BasicPlugin.cs`
- `pcf/components/_template/`
- `client-scripts/examples/formLifecycle.ts`

---

## Rationale

### Why Flat Top-Level?
- Clear technology boundaries
- Easy path-based CI/CD
- Intuitive for new developers

### Why Not Group by Feature?
- D365 components are technology-coupled (plugin ↔ solution)
- Harder to set up CI/CD for specific stacks
- Less clear for specialized developers

### Why `src/` Sub-Folders?
- Separates source from config, tests, docs
- Standard in .NET and Node.js ecosystems
- Cleaner `git log --oneline src/` history

---

## Alternatives Considered

### Alternative: Group by Domain
```
/customer-management/
  ├── plugins/
  ├── pcf/
  └── scripts/
```
**Rejected**: Harder to share code, more complex CI/CD, less clear for Copilot

### Alternative: Fully Flat Structure
```
/AccountPlugin.cs
/DataGrid.tsx
/customerApi.ts
```
**Rejected**: Doesn't scale, no clear boundaries, poor Copilot context

---

## Conclusion

This structure balances:
- **Clarity**: Technology-based organization
- **Scalability**: Room to grow within each component
- **Tooling**: Optimized for CI/CD and Copilot
- **Pragmatism**: Follows .NET and Node.js conventions

It's opinionated but flexible—teams can customize while maintaining core principles.
