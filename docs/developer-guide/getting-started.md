# Getting Started with Dynamics 365 Development

Welcome to the team! This guide will help you set up your development environment and start contributing.

---

## 📋 Prerequisites

### Required Software

1. **Git** (2.30+)
   - [Download Git](https://git-scm.com/downloads)
   - Verify: `git --version`

2. **Node.js** (18 LTS or higher)
   - [Download Node.js](https://nodejs.org/)
   - Verify: `node --version` and `npm --version`

3. **.NET SDK** (8.0 or higher)
   - [Download .NET](https://dotnet.microsoft.com/download)
   - Verify: `dotnet --version`

4. **Visual Studio 2022** or **VS Code**
   - **VS 2022**: Workload "ASP.NET and web development" + ".NET desktop development"
   - **VS Code**: Extensions: C#, ESLint, Prettier, Azure Functions

5. **Power Platform CLI**
   ```bash
   npm install -g @microsoft/powerplatform-cli
   ```
   - Verify: `pac --version`

6. **Azure CLI** (for Azure Functions and Terraform)
   - [Install Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)
   - Verify: `az --version`

7. **Terraform** (1.5+, for infrastructure)
   - [Download Terraform](https://www.terraform.io/downloads)
   - Verify: `terraform --version`

### Optional Tools

- **Postman** or **Insomnia**: For testing APIs
- **Azure Storage Explorer**: For working with Azure storage
- **Dataverse Developer Tools**: Browser extension for Dataverse debugging

---

## 🔧 Environment Setup

### 1. Clone the Repository

```bash
git clone https://github.com/ivoarnet/test-msd-monorepo.git
cd test-msd-monorepo
```

### 2. Install Dependencies

#### Plugins (.NET)
```bash
cd plugins
dotnet restore
dotnet build
cd ..
```

#### PCF Components
```bash
cd pcf
npm install
npm run build
cd ..
```

#### Client Scripts
```bash
cd client-scripts
npm install
npm run build
cd ..
```

#### Azure Functions
```bash
cd functions
dotnet restore
dotnet build
cd ..
```

### 3. Configure Development Environment

#### Dataverse Connection (for local testing)

Create a `appsettings.Development.json` file (gitignored) in plugin projects:

```json
{
  "ConnectionStrings": {
    "Dataverse": "AuthType=OAuth;Url=https://yourorg.crm.dynamics.com;Username=dev@yourorg.onmicrosoft.com;Password=***;AppId=51f81489-12ee-4a9e-aaae-a2591f45987d;RedirectUri=http://localhost;LoginPrompt=Auto"
  }
}
```

> **Security Note**: Never commit credentials. Use Azure Key Vault or environment variables in production.

#### Azure Functions Local Settings

Copy `local.settings.json.example` to `local.settings.json`:

```bash
cd functions
cp local.settings.json.example local.settings.json
# Edit local.settings.json with your values
```

---

## 🏃 Running Locally

### Plugins

Plugins run in Dataverse and cannot be executed locally outside of unit tests.

**Run unit tests**:
```bash
cd plugins
dotnet test
```

**Debug plugins**:
1. Use the Plugin Registration Tool to register your plugin
2. Attach Visual Studio debugger to `w3wp.exe` process (for on-premise)
3. For cloud, use [Plugin Profiling](https://docs.microsoft.com/en-us/power-apps/developer/data-platform/tutorial-debug-plug-in)

### PCF Components

**Development mode** (hot reload):
```bash
cd pcf/components/YourComponent
npm start watch
```

This launches a test harness in your browser.

**Deploy to Dataverse**:
```bash
pac pcf push --publisher-prefix contoso
```

### Client Scripts

**Build and watch**:
```bash
cd client-scripts
npm run watch
```

**Test**:
```bash
npm test
```

**Deploy**: Upload compiled `.js` files to Dataverse as web resources.

### Azure Functions

**Run locally**:
```bash
cd functions/src/YourFunctionApp
func start
```

Functions are available at `http://localhost:7071/api/{functionName}`

**Test with curl**:
```bash
curl -X POST http://localhost:7071/api/YourFunction -H "Content-Type: application/json" -d '{"key":"value"}'
```

---

## 🧪 Running Tests

### All Tests
Run from repository root:
```bash
# .NET tests
cd plugins && dotnet test && cd ..
cd functions && dotnet test && cd ..

# JavaScript/TypeScript tests
cd client-scripts && npm test && cd ..
cd pcf && npm test && cd ..
```

### Watch Mode (TDD)
```bash
# .NET
dotnet watch test

# JavaScript/TypeScript
npm run test:watch
```

---

## 📝 Coding Standards

Before writing code, review:

- [.NET Coding Standards](/docs/standards/dotnet-coding-standards.md)
- [TypeScript Coding Standards](/docs/standards/typescript-coding-standards.md)
- [Plugin Development Patterns](/docs/standards/plugin-patterns.md)

### Key Conventions

- **Commits**: Use [Conventional Commits](https://www.conventionalcommits.org/)
  ```
  feat(plugins): add account validation
  fix(pcf): resolve data grid sorting issue
  docs(readme): update setup instructions
  ```

- **Branches**: 
  - `feature/description-here`
  - `bugfix/description-here`
  - `hotfix/description-here`

- **Code Style**: 
  - .NET: Follow Microsoft guidelines, enforced by `.editorconfig`
  - TypeScript: ESLint + Prettier (configured in `package.json`)

---

## 🤝 Your First Contribution

### Step 1: Pick an Issue
Browse [open issues](https://github.com/ivoarnet/test-msd-monorepo/issues) and look for `good-first-issue` labels.

### Step 2: Create a Branch
```bash
git checkout -b feature/my-first-feature
```

### Step 3: Make Changes
Follow the component-specific guides:
- [Plugins Development](/plugins/README.md)
- [PCF Development](/pcf/README.md)
- [Client Scripts Development](/client-scripts/README.md)
- [Azure Functions Development](/functions/README.md)

### Step 4: Test Your Changes
Run relevant tests and ensure they pass.

### Step 5: Commit and Push
```bash
git add .
git commit -m "feat(component): description of change"
git push origin feature/my-first-feature
```

### Step 6: Create a Pull Request
1. Go to [GitHub](https://github.com/ivoarnet/test-msd-monorepo)
2. Click "New Pull Request"
3. Fill out the template
4. Wait for CI/CD checks and code review

---

## 🐛 Troubleshooting

### Issue: `dotnet restore` fails

**Solution**: 
- Ensure .NET SDK 8.0+ is installed
- Clear NuGet cache: `dotnet nuget locals all --clear`
- Check for proxy/firewall issues

### Issue: `npm install` fails

**Solution**: 
- Use Node.js LTS (v18 or v20)
- Clear npm cache: `npm cache clean --force`
- Delete `node_modules` and `package-lock.json`, then reinstall

### Issue: Power Platform CLI not found

**Solution**: 
```bash
npm install -g @microsoft/powerplatform-cli
# Or reinstall if already installed
npm uninstall -g @microsoft/powerplatform-cli
npm install -g @microsoft/powerplatform-cli
```

### Issue: Plugin registration fails

**Solution**: 
- Ensure your user has System Administrator or System Customizer role
- Check if the plugin assembly is strong-named (required)
- Verify the plugin class implements `IPlugin`

### Issue: Azure Functions won't start locally

**Solution**: 
- Install Azure Functions Core Tools: `npm install -g azure-functions-core-tools@4`
- Check `local.settings.json` is correctly configured
- Ensure no port conflicts (default: 7071)

---

## 📚 Next Steps

- **Understand the architecture**: Read [ADR-001: Repository Strategy](/docs/architecture/ADR-001-repository-strategy.md)
- **Learn the structure**: Read [ADR-002: Folder Structure](/docs/architecture/ADR-002-folder-structure.md)
- **Explore CI/CD**: Read [CI/CD Overview](/docs/developer-guide/cicd-overview.md)
- **Deep dive into components**: Check component-specific READMEs

---

## 💬 Getting Help

- **Questions**: Ask in [GitHub Discussions](https://github.com/ivoarnet/test-msd-monorepo/discussions)
- **Bugs**: Report via [GitHub Issues](https://github.com/ivoarnet/test-msd-monorepo/issues)
- **Team Chat**: Join our Slack/Teams channel (ask your team lead)

---

**Welcome aboard! Happy coding! 🚀**
