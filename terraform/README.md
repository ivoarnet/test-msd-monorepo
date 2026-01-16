# Infrastructure as Code (Terraform)

Terraform configuration for Azure resources supporting Dynamics 365 development.

---

## 📋 Overview

This folder contains Terraform modules and configurations for:
- Azure Functions hosting
- API Management
- Key Vault for secrets
- Storage Accounts
- Service Bus
- Monitoring (Application Insights, Log Analytics)

---

## 🏗️ Structure

```
terraform/
├── environments/
│   ├── dev/
│   │   ├── main.tf               # Main configuration for dev
│   │   ├── variables.tf          # Variable declarations
│   │   ├── terraform.tfvars      # Dev-specific values (gitignored)
│   │   └── backend.tf            # Remote state configuration
│   ├── test/
│   └── prod/
├── modules/
│   ├── function-app/             # Reusable Function App module
│   │   ├── main.tf
│   │   ├── variables.tf
│   │   └── outputs.tf
│   ├── api-management/           # API Management module
│   ├── key-vault/                # Key Vault module
│   └── monitoring/               # Application Insights module
└── README.md
```

---

## 🚀 Getting Started

### Prerequisites

- Terraform 1.5+
- Azure CLI logged in
- Azure subscription with appropriate permissions

### Install Terraform

```bash
# macOS
brew tap hashicorp/tap
brew install hashicorp/tap/terraform

# Windows (Chocolatey)
choco install terraform

# Linux
wget -O- https://apt.releases.hashicorp.com/gpg | sudo gpg --dearmor -o /usr/share/keyrings/hashicorp-archive-keyring.gpg
echo "deb [signed-by=/usr/share/keyrings/hashicorp-archive-keyring.gpg] https://apt.releases.hashicorp.com $(lsb_release -cs) main" | sudo tee /etc/apt/sources.list.d/hashicorp.list
sudo apt update && sudo apt install terraform
```

### Azure CLI Login

```bash
az login
az account set --subscription "Your Subscription Name"
```

---

## 📝 Deploying Infrastructure

### 1. Initialize Terraform

```bash
cd terraform/environments/dev
terraform init
```

### 2. Create `terraform.tfvars`

```hcl
# terraform.tfvars (gitignored)
environment             = "dev"
location                = "East US"
resource_group_name     = "rg-dynamics-dev"
function_app_name       = "func-dynamics-dev"
storage_account_name    = "stdynamicsdev"
key_vault_name          = "kv-dynamics-dev"
dataverse_url           = "https://yourorg.crm.dynamics.com"
```

### 3. Plan Changes

```bash
terraform plan -out=tfplan
```

Review the proposed changes.

### 4. Apply Changes

```bash
terraform apply tfplan
```

---

## 🏗️ Example Module: Function App

### Module Structure

```hcl
# modules/function-app/main.tf
resource "azurerm_storage_account" "function_storage" {
  name                     = var.storage_account_name
  resource_group_name      = var.resource_group_name
  location                 = var.location
  account_tier             = "Standard"
  account_replication_type = "LRS"

  tags = var.tags
}

resource "azurerm_service_plan" "function_plan" {
  name                = var.app_service_plan_name
  resource_group_name = var.resource_group_name
  location            = var.location
  os_type             = "Linux"
  sku_name            = "Y1" # Consumption plan

  tags = var.tags
}

resource "azurerm_linux_function_app" "function_app" {
  name                       = var.function_app_name
  resource_group_name        = var.resource_group_name
  location                   = var.location
  service_plan_id            = azurerm_service_plan.function_plan.id
  storage_account_name       = azurerm_storage_account.function_storage.name
  storage_account_access_key = azurerm_storage_account.function_storage.primary_access_key

  site_config {
    application_stack {
      dotnet_version = "6.0"
    }

    application_insights_connection_string = var.application_insights_connection_string
  }

  app_settings = {
    "FUNCTIONS_WORKER_RUNTIME"       = "dotnet"
    "Dataverse__ConnectionString"    = "@Microsoft.KeyVault(SecretUri=${var.dataverse_connection_secret_uri})"
    "ServiceBus__ConnectionString"   = "@Microsoft.KeyVault(SecretUri=${var.servicebus_connection_secret_uri})"
  }

  identity {
    type = "SystemAssigned"
  }

  tags = var.tags
}

# Grant Function App access to Key Vault
resource "azurerm_key_vault_access_policy" "function_app_policy" {
  key_vault_id = var.key_vault_id
  tenant_id    = azurerm_linux_function_app.function_app.identity[0].tenant_id
  object_id    = azurerm_linux_function_app.function_app.identity[0].principal_id

  secret_permissions = [
    "Get",
    "List"
  ]
}
```

### Using the Module

```hcl
# environments/dev/main.tf
module "integration_function_app" {
  source = "../../modules/function-app"

  function_app_name                      = "func-integration-dev"
  resource_group_name                    = azurerm_resource_group.main.name
  location                               = var.location
  storage_account_name                   = "stintegrationdev"
  app_service_plan_name                  = "asp-integration-dev"
  key_vault_id                           = module.key_vault.key_vault_id
  application_insights_connection_string = module.monitoring.app_insights_connection_string
  dataverse_connection_secret_uri        = "${module.key_vault.key_vault_url}secrets/DataverseConnection"
  servicebus_connection_secret_uri       = "${module.key_vault.key_vault_url}secrets/ServiceBusConnection"

  tags = local.common_tags
}
```

---

## 🔒 Security Best Practices

### 1. Store Secrets in Key Vault

```hcl
resource "azurerm_key_vault_secret" "dataverse_connection" {
  name         = "DataverseConnection"
  value        = var.dataverse_connection_string
  key_vault_id = azurerm_key_vault.main.id
}
```

### 2. Use Managed Identity

Enable System-Assigned Managed Identity for Azure resources to avoid storing credentials.

### 3. Remote State Backend

Store Terraform state remotely in Azure Storage:

```hcl
# backend.tf
terraform {
  backend "azurerm" {
    resource_group_name  = "rg-terraform-state"
    storage_account_name = "sttfstate"
    container_name       = "tfstate"
    key                  = "dev.terraform.tfstate"
  }
}
```

### 4. Ignore Sensitive Files

Ensure `.gitignore` includes:
```
*.tfvars
*.tfstate
*.tfstate.backup
.terraform/
```

---

## 🧪 Testing

### Validate Configuration

```bash
terraform validate
```

### Format Code

```bash
terraform fmt -recursive
```

### Security Scanning

Use tools like [tfsec](https://github.com/aquasecurity/tfsec):

```bash
tfsec .
```

---

## 📦 Common Resources

### Resource Group

```hcl
resource "azurerm_resource_group" "main" {
  name     = "rg-dynamics-${var.environment}"
  location = var.location

  tags = local.common_tags
}
```

### Key Vault

```hcl
resource "azurerm_key_vault" "main" {
  name                = "kv-dynamics-${var.environment}"
  resource_group_name = azurerm_resource_group.main.name
  location            = var.location
  tenant_id           = data.azurerm_client_config.current.tenant_id
  sku_name            = "standard"

  purge_protection_enabled = var.environment == "prod" ? true : false

  tags = local.common_tags
}
```

### Application Insights

```hcl
resource "azurerm_application_insights" "main" {
  name                = "appi-dynamics-${var.environment}"
  resource_group_name = azurerm_resource_group.main.name
  location            = var.location
  application_type    = "web"

  tags = local.common_tags
}
```

---

## 🔄 CI/CD Integration

See [terraform-plan.yml](/.github/workflows/terraform-plan.yml) for automated planning and applying.

### Workflow Steps

1. **PR**: Run `terraform plan` and post results as PR comment
2. **Merge to main**: Run `terraform apply` with manual approval
3. **Drift detection**: Scheduled runs to detect manual changes

---

## 📚 Best Practices

✅ **Do**:
- Use modules for reusable components
- Separate environments (dev/test/prod)
- Use remote state backend
- Tag all resources
- Use variables for configurability
- Document module inputs/outputs
- Enable state locking

❌ **Don't**:
- Commit `*.tfvars` files with secrets
- Hard-code values
- Share state files via Git
- Mix environments in same state
- Ignore `terraform plan` output
- Apply changes without review

---

## 🔗 Resources

- [Terraform Documentation](https://www.terraform.io/docs)
- [Azure Provider Reference](https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs)
- [Terraform Best Practices](https://www.terraform-best-practices.com/)

---

**Questions? Contact the DevOps team or open a [GitHub Discussion](https://github.com/ivoarnet/test-msd-monorepo/discussions).**
