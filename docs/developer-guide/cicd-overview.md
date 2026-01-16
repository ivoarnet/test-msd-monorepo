# CI/CD Overview

This document describes the Continuous Integration and Continuous Deployment (CI/CD) pipelines for the Dynamics 365 monorepo.

---

## 🎯 Goals

- **Automated testing**: Run tests on every push and PR
- **Fast feedback**: Developers know within minutes if changes break anything
- **Path-based triggers**: Only build/test what changed
- **Environment promotion**: Dev → Test → Prod with approvals
- **Artifact management**: Store build outputs for deployment

---

## 🏗️ Architecture

### Pipeline Strategy

Each component has its own CI/CD pipeline triggered by file path changes:

```
plugins/** changes       → plugins-ci.yml
pcf/** changes           → pcf-ci.yml
client-scripts/** changes → client-scripts-ci.yml
functions/** changes     → functions-ci.yml
terraform/** changes     → terraform-plan.yml
```

### Common Pipeline Stages

All pipelines follow a consistent structure:

1. **Checkout**: Clone repository code
2. **Setup**: Install language runtimes and dependencies
3. **Build**: Compile source code
4. **Test**: Run unit and integration tests
5. **Package**: Create deployment artifacts
6. **Deploy**: Push to target environments (optional, triggered manually or on merge to main)

---

## 📋 Pipeline Details

### 1. Plugins CI/CD (`plugins-ci.yml`)

**Triggers**: 
- Changes to `plugins/**`
- Changes to `.github/workflows/plugins-ci.yml`

**Steps**:
1. Setup .NET 6.0+
2. Restore NuGet packages
3. Build solution
4. Run unit tests (with coverage)
5. Build plugin assemblies (Release mode)
6. Strong-name sign assemblies
7. Upload artifacts (DLLs ready for Plugin Registration Tool)

**Artifacts**:
- `plugins-release.zip`: Signed plugin DLLs

**Deployment**:
- **Dev**: Auto-deploy on merge to `main`
- **Test/Prod**: Manual approval required

---

### 2. PCF CI/CD (`pcf-ci.yml`)

**Triggers**: 
- Changes to `pcf/**`
- Changes to `.github/workflows/pcf-ci.yml`

**Steps**:
1. Setup Node.js 18
2. Install npm dependencies
3. Build all PCF components
4. Run tests
5. Package components as solution files
6. Upload artifacts

**Artifacts**:
- `pcf-components.zip`: PCF solution files for import

**Deployment**:
- **Dev**: Auto-import to dev environment via Power Platform CLI
- **Test/Prod**: Manual import or ALM toolkit

---

### 3. Client Scripts CI/CD (`client-scripts-ci.yml`)

**Triggers**: 
- Changes to `client-scripts/**`
- Changes to `.github/workflows/client-scripts-ci.yml`

**Steps**:
1. Setup Node.js 18
2. Install dependencies
3. Run ESLint
4. Run unit tests
5. Build TypeScript → JavaScript bundles
6. Minify output
7. Upload artifacts

**Artifacts**:
- `client-scripts.zip`: JavaScript files ready for Dataverse web resources

**Deployment**:
- Manual upload to Dataverse as web resources
- Or automated via Power Platform Build Tools

---

### 4. Azure Functions CI/CD (`functions-ci.yml`)

**Triggers**: 
- Changes to `functions/**`
- Changes to `.github/workflows/functions-ci.yml`

**Steps**:
1. Setup .NET 6.0+
2. Restore dependencies
3. Build function apps
4. Run unit tests
5. Run integration tests (optional)
6. Publish function app
7. Create deployment package (ZIP)
8. Deploy to Azure Functions

**Artifacts**:
- `functions-{app-name}.zip`: Function app ready for deployment

**Deployment**:
- **Dev**: Auto-deploy on merge to `main`
- **Test**: Auto-deploy on merge to `release/*` branch
- **Prod**: Manual approval via GitHub Environments

**Environment Variables**:
- Managed via Azure App Settings (not in repo)
- Secrets stored in GitHub Secrets or Azure Key Vault

---

### 5. Terraform CI/CD (`terraform-plan.yml`)

**Triggers**: 
- Changes to `terraform/**`
- Changes to `.github/workflows/terraform-plan.yml`

**Steps**:
1. Setup Terraform CLI
2. Terraform init
3. Terraform validate
4. Terraform fmt check
5. Terraform plan (for each environment)
6. Upload plan artifacts
7. Terraform apply (manual approval required)

**Artifacts**:
- `terraform-plan-{env}.txt`: Plan output for review

**Deployment**:
- Always requires manual approval
- Apply runs on merge to `main` with approval gate

---

## 🚦 Branch Strategy and Triggers

### Pull Request (PR) Builds

**When**: On every PR to `main` or `develop`

**What**:
- Run all relevant pipelines based on changed files
- Execute build + test stages only (no deployment)
- Report status checks back to PR

**Requirements**:
- All checks must pass before merge
- Code owners must approve (via CODEOWNERS)

### Main Branch Builds

**When**: On merge to `main`

**What**:
- Run full CI/CD pipeline
- Deploy to **Dev** environment automatically
- Create deployment artifacts
- Tag release (optional, for versioned releases)

### Release Branches

**When**: On merge to `release/*` branches

**What**:
- Deploy to **Test** environment
- Run smoke tests
- Await approval for production

### Production Deployment

**When**: Manual trigger or merge to `main` with approval

**What**:
- Deploy to **Prod** environment
- Requires manual approval via GitHub Environments
- Run post-deployment validation
- Send notifications (Slack/Teams)

---

## 🔒 Security and Secrets

### GitHub Secrets

Store sensitive information in GitHub Secrets:

- `AZURE_CREDENTIALS`: Service principal for Azure deployments
- `DATAVERSE_DEV_URL`: Dataverse dev environment URL
- `DATAVERSE_DEV_CLIENT_ID`: App registration for dev
- `DATAVERSE_DEV_CLIENT_SECRET`: App secret for dev
- (Repeat for test/prod environments)

### Azure Key Vault

For production, integrate with Azure Key Vault:

```yaml
- name: Get secrets from Key Vault
  uses: Azure/get-keyvault-secrets@v1
  with:
    keyvault: "my-keyvault"
    secrets: 'DataverseUrl, ClientId, ClientSecret'
  id: keyvault
```

### Least Privilege Access

- CI/CD service principals have minimal required permissions
- Separate credentials per environment
- Rotate secrets regularly

---

## 📊 Monitoring and Notifications

### Build Status

- GitHub Actions dashboard shows pipeline status
- Branch protection rules enforce passing checks

### Notifications

- **Slack/Teams**: Post to channel on deployment success/failure
- **Email**: Notify on production deployments
- **GitHub**: PR comments with build results

### Metrics

Track key metrics:
- Build duration
- Test pass rate
- Deployment frequency
- Mean time to recovery (MTTR)

---

## 🛠️ Reusable Workflows

### Shared Workflow Components

Common steps are extracted into reusable workflows:

**`.github/workflows/_reusable-build.yml`**:
- Standard build/test steps
- Called by specific component pipelines
- Reduces duplication

**Example usage**:
```yaml
jobs:
  build:
    uses: ./.github/workflows/_reusable-build.yml
    with:
      component: plugins
      dotnet-version: '6.0.x'
```

---

## 🧪 Testing Strategy

### Unit Tests

- Run on every push and PR
- Fast feedback (< 5 minutes)
- Block merge if tests fail

### Integration Tests

- Run on merge to `main`
- Test against dev environment
- Can be slower (10-30 minutes)

### End-to-End Tests

- Run on deployment to test environment
- Validate full workflows
- Optional for dev, required for prod

---

## 📦 Artifact Management

### Storage

- Artifacts stored in GitHub Actions artifacts (30-90 days retention)
- Production releases stored in Azure Blob Storage (long-term)

### Versioning

- Artifacts tagged with commit SHA
- Release artifacts tagged with semantic version (v1.2.3)

---

## 🔄 Deployment Environments

### Environment Configuration

GitHub Environments are configured for:

1. **Dev**
   - Auto-deploy on merge to `main`
   - No approval required
   - Used for integration testing

2. **Test**
   - Deploy on merge to `release/*`
   - Approval from QA team
   - Pre-production validation

3. **Prod**
   - Manual deployment trigger
   - Approval from product owner + ops
   - Deployment windows (e.g., Tue/Thu only)

### Environment Protection Rules

```yaml
environments:
  production:
    url: https://prod.example.com
    deployment_branch_policy:
      protected_branches: true
    reviewers:
      - team: platform-ops
      - user: john-doe
```

---

## 🐛 Troubleshooting CI/CD

### Pipeline Fails: "Resource not found"

**Cause**: Missing GitHub Secret or environment variable

**Solution**: 
1. Check repository secrets at Settings → Secrets
2. Verify secret names match workflow references
3. Ensure service principal has correct permissions

### Pipeline Fails: "Tests failed"

**Cause**: Actual test failures or environment issues

**Solution**: 
1. Check test logs in GitHub Actions
2. Run tests locally to reproduce
3. Fix failing tests, push, and re-run

### Deployment Fails: "Unauthorized"

**Cause**: Service principal credentials expired or insufficient permissions

**Solution**: 
1. Verify Azure credentials are current
2. Check service principal has Contributor role on target resources
3. Rotate secrets if needed

### Build Takes Too Long

**Cause**: Not using cached dependencies

**Solution**: 
Add caching steps:
```yaml
- uses: actions/cache@v3
  with:
    path: ~/.nuget/packages
    key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj') }}
```

---

## 📚 Additional Resources

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Azure DevOps Build Pipelines](https://learn.microsoft.com/en-us/azure/devops/pipelines/)
- [Terraform in CI/CD](https://developer.hashicorp.com/terraform/tutorials/automation/github-actions)

---

## 🗺️ Future Improvements

- [ ] Add automated rollback on deployment failure
- [ ] Implement blue-green deployments for Functions
- [ ] Add performance testing stage
- [ ] Integrate security scanning (SAST/DAST)
- [ ] Add dependency vulnerability scanning

---

For questions about CI/CD, contact the DevOps team or open a [GitHub Discussion](https://github.com/ivoarnet/test-msd-monorepo/discussions).
