# Dynamics 365 / Power Platform Monorepo

> **Enterprise-grade Dynamics 365 development repository** featuring plugins, PCF components, Azure Functions, client scripts, and Infrastructure as Code.

[![CI Status](https://github.com/ivoarnet/test-msd-monorepo/workflows/CI/badge.svg)](https://github.com/ivoarnet/test-msd-monorepo/actions)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

---

## 📋 Table of Contents

- [Overview](#overview)
- [Repository Structure](#repository-structure)
- [Quick Start](#quick-start)
- [Components](#components)
- [Development](#development)
- [CI/CD](#cicd)
- [Documentation](#documentation)
- [Contributing](#contributing)

---

## 🎯 Overview

This monorepo contains all components for our Dynamics 365 / Dataverse solution:

| Component | Technology | Purpose |
|-----------|-----------|---------|
| **[Plugins](/src/plugins)** | .NET 8+ | Server-side business logic (plugins, custom APIs) |
| **[PCF](/src/pcf)** | TypeScript, React | Custom UI controls (PowerApps Component Framework) |
| **[Client Scripts](/src/client-scripts)** | TypeScript | Form and ribbon customizations |
| **[Azure Functions](/src/functions)** | .NET/Node.js | Integration APIs and background processing |
| **[Solutions](/src/solutions)** | XML | Dataverse solution exports |
| **[Terraform](/infra/terraform)** | HCL | Infrastructure as Code for Azure resources |

---

## 📁 Repository Structure

```
/
├── .github/              # GitHub Actions workflows and templates
├── docs/                 # Documentation and architecture decisions
│   ├── architecture/     # ADRs (Architecture Decision Records)
│   ├── developer-guide/  # Developer onboarding and guides
│   └── standards/        # Coding standards and best practices
├── infra/                # Infrastructure as Code
│   └── terraform/        # Terraform modules and configurations
├── src/                  # Source code
│   ├── client-scripts/   # JavaScript/TypeScript for forms and ribbons
│   ├── functions/        # Azure Functions for integrations
│   ├── pcf/              # PowerApps Component Framework controls
│   ├── plugins/          # .NET Dataverse plugins and custom APIs
│   └── solutions/        # Dataverse solution exports
└── README.md             # This file
```

See [ADR-002: Folder Structure](/docs/architecture/ADR-002-folder-structure.md) for detailed rationale.

---

## 🚀 Quick Start

### Prerequisites

- **Node.js**: 18+ (for PCF, client scripts)
- **.NET SDK**: 8.0+ (for plugins, Azure Functions)
- **Power Platform CLI**: Latest version
- **Terraform**: 1.5+ (for infrastructure)
- **Azure CLI**: Latest version
- **Git**: 2.30+

### Initial Setup

```bash
# Clone the repository
git clone https://github.com/ivoarnet/test-msd-monorepo.git
cd test-msd-monorepo

# Install Node.js dependencies (PCF, client scripts)
cd src/pcf && npm install && cd ../..
cd src/client-scripts && npm install && cd ../..

# Restore .NET dependencies (plugins)
cd src/plugins && dotnet restore && cd ../..

# Restore Azure Functions dependencies
cd src/functions && dotnet restore && cd ../..

# Initialize Terraform (optional, for infrastructure)
cd infra/terraform/environments/dev && terraform init && cd ../../../..
```

### Building Components

```bash
# Build plugins
cd src/plugins && dotnet build && cd ../..

# Build PCF components
cd src/pcf && npm run build && cd ../..

# Build client scripts
cd src/client-scripts && npm run build && cd ../..

# Build Azure Functions
cd src/functions && dotnet build && cd ../..
```

### Running Tests

```bash
# Test plugins
cd src/plugins && dotnet test && cd ../..

# Test client scripts
cd src/client-scripts && npm test && cd ../..

# Test Azure Functions
cd src/functions && dotnet test && cd ../..
```

---

## 🧩 Components

### [Plugins](/src/plugins)
**.NET server-side logic for Dataverse**

- Business logic triggered by Dataverse events (Create, Update, Delete)
- Custom APIs for specialized operations
- [Learn more →](/src/plugins/README.md)

**Key Technologies**: .NET 8+, Microsoft.CrmSdk.CoreAssemblies, IPlugin

---

### [PCF](/src/pcf)
**Custom UI controls for model-driven apps**

- Reusable components (data grids, file uploaders, charts)
- Built with TypeScript and React
- [Learn more →](/src/pcf/README.md)

**Key Technologies**: PowerApps Component Framework, TypeScript, React, Fluent UI

---

### [Client Scripts](/src/client-scripts)
**Form and ribbon JavaScript/TypeScript**

- Form lifecycle logic (onLoad, onSave, onChange)
- Ribbon button actions
- Business rules and validations
- [Learn more →](/src/client-scripts/README.md)

**Key Technologies**: TypeScript, Xrm.WebApi, Dataverse Web API

---

### [Azure Functions](/src/functions)
**Integration APIs and background jobs**

- REST APIs for external integrations
- Scheduled jobs (e.g., nightly data sync)
- Event-driven processing
- [Learn more →](/src/functions/README.md)

**Key Technologies**: Azure Functions, .NET/Node.js, Azure Service Bus

---

### [Solutions](/src/solutions)
**Dataverse solution exports**

- Source-controlled solution XML
- Managed and unmanaged solutions
- [Learn more →](/src/solutions/README.md)

**Key Technologies**: Power Platform CLI, Dataverse Solutions

---

### [Terraform](/infra/terraform)
**Infrastructure as Code**

- Azure resources (Function Apps, App Service Plans, Key Vault)
- API Management configuration
- Environment management (dev, test, prod)
- [Learn more →](/infra/terraform/README.md)

**Key Technologies**: Terraform, Azure Provider

---

## 💻 Development

### Branching Strategy

- **main**: Production-ready code
- **feature/***: New features (e.g., `feature/add-customer-validation`)
- **bugfix/***: Bug fixes (e.g., `bugfix/fix-date-calculation`)
- **hotfix/***: Production hotfixes

### Code Ownership

This repository uses [CODEOWNERS](/.github/CODEOWNERS) for approval workflows. Changes to specific components require approval from designated teams.

### Coding Standards

- **Plugins**: Follow [.NET Coding Standards](/docs/standards/dotnet-coding-standards.md)
- **TypeScript**: Follow [TypeScript Standards](/docs/standards/typescript-coding-standards.md)
- **Terraform**: Follow [Terraform Best Practices](/docs/standards/terraform-best-practices.md)

See [docs/standards/](/docs/standards/) for comprehensive guidelines.

### Development Workflow

1. **Create a branch**: `git checkout -b feature/my-feature`
2. **Make changes**: Follow component-specific guides
3. **Test locally**: Run tests for affected components
4. **Commit**: Use [Conventional Commits](https://www.conventionalcommits.org/)
   ```bash
   git commit -m "feat(plugins): add account validation logic"
   ```
5. **Push and create PR**: CI/CD runs automatically
6. **Code review**: Address feedback from CODEOWNERS
7. **Merge**: Squash and merge to main

---

## 🔄 CI/CD

### GitHub Actions Workflows

All CI/CD pipelines are in [`.github/workflows/`](/.github/workflows/):

| Workflow | Trigger | Purpose |
|----------|---------|---------|
| **[plugins-ci.yml](/.github/workflows/plugins-ci.yml)** | Changes to `plugins/**` | Build, test, and package plugins |
| **[pcf-ci.yml](/.github/workflows/pcf-ci.yml)** | Changes to `pcf/**` | Build and package PCF components |
| **[client-scripts-ci.yml](/.github/workflows/client-scripts-ci.yml)** | Changes to `client-scripts/**` | Build and bundle client scripts |
| **[functions-ci.yml](/.github/workflows/functions-ci.yml)** | Changes to `functions/**` | Build, test, and deploy Azure Functions |
| **[terraform-plan.yml](/.github/workflows/terraform-plan.yml)** | Changes to `terraform/**` | Validate and plan infrastructure changes |

### Pipeline Stages

Each pipeline follows a consistent structure:

1. **Build**: Compile code, restore dependencies
2. **Test**: Run unit and integration tests
3. **Package**: Create deployment artifacts (DLLs, ZIP files)
4. **Deploy**: Push to target environment (dev → test → prod)

See [CI/CD Documentation](/docs/developer-guide/cicd-overview.md) for details.

---

## 📚 Documentation

### Architecture
- [ADR-001: Repository Strategy](/docs/architecture/ADR-001-repository-strategy.md) - Monorepo vs multi-repo
- [ADR-002: Folder Structure](/docs/architecture/ADR-002-folder-structure.md) - Component organization

### Developer Guides
- [Getting Started](/docs/developer-guide/getting-started.md) - Onboarding for new developers
- [Local Development Setup](/docs/developer-guide/local-development.md) - Environment configuration
- [CI/CD Overview](/docs/developer-guide/cicd-overview.md) - Pipeline documentation
- [Debugging Guide](/docs/developer-guide/debugging.md) - Troubleshooting tips

### Coding Standards
- [.NET Coding Standards](/docs/standards/dotnet-coding-standards.md)
- [TypeScript Coding Standards](/docs/standards/typescript-coding-standards.md)
- [Terraform Best Practices](/docs/standards/terraform-best-practices.md)
- [Plugin Development Patterns](/docs/standards/plugin-patterns.md)

### Component-Specific Docs
- [Plugins README](/plugins/README.md)
- [PCF README](/pcf/README.md)
- [Client Scripts README](/client-scripts/README.md)
- [Azure Functions README](/functions/README.md)
- [Terraform README](/terraform/README.md)

---

## 🤝 Contributing

We welcome contributions! Please follow these steps:

1. **Read the guides**: Familiarize yourself with [coding standards](/docs/standards/)
2. **Check issues**: Look for `good-first-issue` labels
3. **Create a branch**: Follow the branching strategy
4. **Write tests**: Maintain or improve code coverage
5. **Submit a PR**: Fill out the pull request template
6. **Respond to feedback**: Work with reviewers

See [CONTRIBUTING.md](/CONTRIBUTING.md) for detailed guidelines.

---

## 🛠️ Tooling and GitHub Copilot

This repository is optimized for **GitHub Copilot**:

- **Clear folder structure** helps Copilot understand context
- **Rich README files** provide AI with component overviews
- **Inline documentation** (XML docs, JSDoc) guides code generation
- **Example code** in `/examples/` folders demonstrates patterns
- **Naming conventions** make intent obvious

### Copilot Tips
- Open component-specific READMEs for better suggestions
- Use descriptive variable and function names
- Add comments describing business logic before writing code
- Leverage examples in `/examples/` folders as reference

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 🆘 Support

- **Issues**: Report bugs or request features via [GitHub Issues](https://github.com/ivoarnet/test-msd-monorepo/issues)
- **Discussions**: Ask questions in [GitHub Discussions](https://github.com/ivoarnet/test-msd-monorepo/discussions)
- **Documentation**: Check [docs/](/docs/) for guides and ADRs

---

## 🗺️ Roadmap

- [ ] Add example plugin implementations
- [ ] Create PCF component templates
- [ ] Implement automated solution packaging
- [ ] Add performance testing pipeline
- [ ] Create developer container (devcontainer.json)

See [ROADMAP.md](/ROADMAP.md) for the full roadmap.

---

**Made with ❤️ for the Dynamics 365 community**