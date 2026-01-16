# Dataverse Solutions

Source-controlled Dataverse solution exports for customizations.

---

## 📋 Overview

This folder contains exported Dataverse solutions including:
- Tables (entities) and columns (attributes)
- Forms and views
- Business rules and workflows
- Security roles
- Canvas and model-driven apps
- Connection references and environment variables

---

## 🏗️ Structure

```
solutions/
├── managed/                   # Managed solution ZIPs (for deployment)
│   └── ContosoSolution_1_0_0_0_managed.zip
├── unmanaged/                 # Unmanaged solution exports (source control)
│   └── ContosoSolution/
│       ├── Other/
│       ├── Entities/
│       │   ├── Account/
│       │   └── Contact/
│       ├── OptionSets/
│       ├── WebResources/
│       ├── Workflows/
│       └── [Content_Types].xml
└── README.md
```

---

## 🚀 Exporting Solutions

### Using Power Platform CLI

#### 1. Export Unmanaged Solution

```bash
# Connect to environment
pac auth create --url https://yourorg.crm.dynamics.com

# Export solution
pac solution export \
  --name ContosoSolution \
  --path ./unmanaged/ContosoSolution.zip \
  --managed false

# Unpack solution to source control
pac solution unpack \
  --zipfile ./unmanaged/ContosoSolution.zip \
  --folder ./unmanaged/ContosoSolution \
  --packagetype Both
```

#### 2. Export Managed Solution

```bash
pac solution export \
  --name ContosoSolution \
  --path ./managed/ContosoSolution_managed.zip \
  --managed true
```

### Using Solution Packager (Legacy)

```bash
SolutionPackager.exe /action:Extract \
  /zipfile:ContosoSolution.zip \
  /folder:unmanaged/ContosoSolution \
  /packagetype:Both
```

---

## 📦 Importing Solutions

### Development Environment

Import unmanaged solution for ongoing development:

```bash
pac solution import \
  --path ./unmanaged/ContosoSolution.zip \
  --activate-plugins \
  --force-overwrite
```

### Test/Production Environments

Import managed solution:

```bash
pac solution import \
  --path ./managed/ContosoSolution_managed.zip \
  --activate-plugins \
  --force-overwrite
```

---

## 🔄 Version Control Workflow

### 1. Make Changes in Dev Environment

- Create/modify tables, forms, views via Power Apps
- Register plugins using Plugin Registration Tool
- Test thoroughly

### 2. Export and Unpack Solution

```bash
# Export
pac solution export --name ContosoSolution --path temp.zip --managed false

# Unpack to source control
pac solution unpack --zipfile temp.zip --folder unmanaged/ContosoSolution --packagetype Both

# Clean up
rm temp.zip
```

### 3. Commit Changes

```bash
git add solutions/unmanaged/ContosoSolution/
git commit -m "feat(solution): add new email validation field to Contact"
git push
```

### 4. CI/CD Builds Managed Solution

GitHub Actions workflow automatically:
1. Packs unmanaged solution
2. Creates managed solution
3. Stores artifact for deployment

---

## 🎯 Solution Components

### Tables (Entities)

Located in `unmanaged/ContosoSolution/Entities/`

**Example: Custom table definition**

```xml
<!-- Entity.xml -->
<Entity>
  <Name>contoso_project</Name>
  <EntityInfo>
    <entity Name="contoso_project">
      <LocalizedNames>
        <LocalizedName description="Project" languagecode="1033" />
      </LocalizedNames>
      <LocalizedCollectionNames>
        <LocalizedCollectionName description="Projects" languagecode="1033" />
      </LocalizedCollectionNames>
      <Attributes>
        <attribute PhysicalName="contoso_name">
          <Type>nvarchar</Type>
          <Name>contoso_name</Name>
          <LogicalName>contoso_name</LogicalName>
        </attribute>
      </Attributes>
    </entity>
  </EntityInfo>
</Entity>
```

### Web Resources

Located in `unmanaged/ContosoSolution/WebResources/`

- JavaScript files from `client-scripts/`
- CSS files
- HTML pages
- Images

### Workflows

Located in `unmanaged/ContosoSolution/Workflows/`

- Cloud flows
- Classic workflows
- Business process flows

---

## 🧪 Testing Solutions

### Validation Checks

Before committing:

```bash
# Pack solution to verify no errors
pac solution pack \
  --zipfile test.zip \
  --folder unmanaged/ContosoSolution \
  --packagetype Both

# Clean up
rm test.zip
```

### Solution Checker

Run solution checker to detect issues:

```bash
pac solution check \
  --path unmanaged/ContosoSolution.zip \
  --geo UnitedStates
```

---

## 🔧 Best Practices

### Solution Structure

✅ **Do**:
- Use semantic versioning (e.g., 1.2.3)
- Keep solutions focused (don't mix unrelated customizations)
- Always unpack solutions for source control
- Document solution dependencies
- Use solution layers appropriately

❌ **Don't**:
- Commit packed ZIP files to Git (except managed in `managed/`)
- Mix multiple solutions without dependencies
- Manually edit unpacked XML (use Power Apps maker portal)
- Include default Dataverse tables unless customized

### Environment Variables

Use environment variables for environment-specific values:

```bash
# Set environment variable value
pac solution online-version \
  --name EnvironmentVariableName \
  --value "dev-specific-value"
```

### Connection References

Define connection references for external integrations:
- Keep connection references in solution
- Set actual connections per environment
- Document required connections in README

---

## 📚 Solution Dependencies

If your solution depends on others, document them:

### Dependencies

- **Contoso.Core** (>= 1.0.0): Base tables and security roles
- **Contoso.Integration** (>= 2.1.0): Azure Functions integration

### Installing Dependencies

```bash
# Install core solution first
pac solution import --path ContosoCore_managed.zip

# Then install dependent solution
pac solution import --path ContosoSolution_managed.zip
```

---

## 🚢 Deployment Pipeline

See [solutions-ci.yml](/.github/workflows/solutions-ci.yml) for automated solution deployment.

### Pipeline Stages

1. **Pack**: Pack unmanaged solution from source
2. **Build Managed**: Create managed solution
3. **Validate**: Run solution checker
4. **Deploy Dev**: Import to dev environment
5. **Deploy Test**: Import to test (with approval)
6. **Deploy Prod**: Import to prod (with approval)

---

## 🔗 Solution Layers

Dataverse uses solution layers. View layers in the environment:

```bash
# List solutions in environment
pac solution list

# View solution history
pac solution history --name ContosoSolution
```

**Layer Order** (top to bottom):
1. Unmanaged Active
2. Unmanaged Base
3. Managed 1 (most recent)
4. Managed 2
5. System

---

## 📝 Solution Manifest Example

```xml
<!-- Solution.xml -->
<ImportExportXml version="9.2.24021.215" SolutionPackageVersion="9.2" 
  languagecode="1033" generatedBy="CrmLive" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <SolutionManifest>
    <UniqueName>ContosoSolution</UniqueName>
    <LocalizedNames>
      <LocalizedName description="Contoso Business Solution" languagecode="1033" />
    </LocalizedNames>
    <Descriptions>
      <Description description="Core business logic and customizations" languagecode="1033" />
    </Descriptions>
    <Version>1.0.0.0</Version>
    <Managed>0</Managed>
    <Publisher>
      <UniqueName>contoso</UniqueName>
      <LocalizedNames>
        <LocalizedName description="Contoso Ltd" languagecode="1033" />
      </LocalizedNames>
      <CustomizationPrefix>contoso</CustomizationPrefix>
    </Publisher>
  </SolutionManifest>
</ImportExportXml>
```

---

## 🔗 Resources

- [Solution Concepts](https://learn.microsoft.com/en-us/power-platform/alm/solution-concepts-alm)
- [Power Platform CLI](https://learn.microsoft.com/en-us/power-platform/developer/cli/introduction)
- [Solution Packager](https://learn.microsoft.com/en-us/power-platform/alm/solution-packager-tool)
- [ALM for Power Platform](https://learn.microsoft.com/en-us/power-platform/alm/)

---

**Questions? Contact the Platform team or open a [GitHub Discussion](https://github.com/ivoarnet/test-msd-monorepo/discussions).**
