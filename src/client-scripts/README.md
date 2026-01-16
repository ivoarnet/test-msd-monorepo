# Client Scripts

JavaScript/TypeScript for Dynamics 365 form and ribbon customizations.

---

## 📋 Overview

Client scripts enable:
- Form event handlers (onLoad, onChange, onSave)
- Field validations
- Dynamic UI behavior
- Ribbon button actions
- Business rules in code

---

## 🏗️ Structure

```
client-scripts/
├── src/
│   ├── forms/
│   │   ├── account.ts           # Account form logic
│   │   ├── contact.ts           # Contact form logic
│   │   └── opportunity.ts       # Opportunity form logic
│   ├── ribbons/
│   │   ├── accountRibbon.ts     # Account ribbon actions
│   │   └── globalRibbon.ts      # Global ribbon actions
│   ├── utils/
│   │   ├── webApiHelper.ts      # Xrm.WebApi wrappers
│   │   ├── navigationHelper.ts  # Xrm.Navigation helpers
│   │   └── validation.ts        # Common validation functions
│   └── types/
│       └── xrm.d.ts             # Custom Xrm type extensions
├── tests/
│   └── forms/
│       └── account.test.ts
├── dist/                        # Compiled JavaScript (gitignored)
├── tsconfig.json
├── package.json
└── README.md
```

---

## 🚀 Getting Started

### Prerequisites

- Node.js 18+
- TypeScript 4.9+
- Dataverse environment

### Install Dependencies

```bash
cd client-scripts
npm install
```

### Build

```bash
npm run build
```

Output files are in `dist/` directory.

### Watch Mode (Development)

```bash
npm run watch
```

Automatically rebuilds on file changes.

---

## 📝 Creating Form Scripts

### 1. Create TypeScript File

```typescript
// src/forms/account.ts
/**
 * Form scripts for Account entity.
 * @module AccountForm
 */

namespace Contoso.Forms.Account {
    
    /**
     * Executes when the form loads.
     * @param executionContext - The execution context
     */
    export function onLoad(executionContext: Xrm.Events.EventContext): void {
        const formContext = executionContext.getFormContext();
        
        console.log("Account form loaded");
        
        // Initialize form
        configureFieldRequirements(formContext);
        setFieldVisibility(formContext);
        loadRelatedData(formContext);
    }

    /**
     * Executes when Account Type changes.
     * @param executionContext - The execution context
     */
    export function onAccountTypeChange(executionContext: Xrm.Events.EventContext): void {
        const formContext = executionContext.getFormContext();
        const accountType = formContext.getAttribute<Xrm.Attributes.OptionSetAttribute>("accounttype");
        
        if (!accountType) return;

        const typeValue = accountType.getValue();
        
        // Show/hide fields based on account type
        if (typeValue === 1) { // Commercial
            showCommercialFields(formContext);
        } else if (typeValue === 2) { // Residential
            showResidentialFields(formContext);
        }
    }

    /**
     * Executes before the form saves.
     * @param executionContext - The execution context
     */
    export function onSave(executionContext: Xrm.Events.SaveEventContext): void {
        const formContext = executionContext.getFormContext();
        
        // Validate form data
        if (!validateAccountData(formContext)) {
            executionContext.getEventArgs().preventDefault();
            Xrm.Navigation.openAlertDialog({
                text: "Please correct the validation errors before saving."
            });
        }
    }

    /**
     * Validates account data before saving.
     */
    function validateAccountData(formContext: Xrm.FormContext): boolean {
        const name = formContext.getAttribute<Xrm.Attributes.StringAttribute>("name");
        const revenue = formContext.getAttribute<Xrm.Attributes.NumberAttribute>("revenue");
        
        // Name is required
        if (!name || !name.getValue()) {
            Xrm.Navigation.openErrorDialog({ message: "Account name is required" });
            return false;
        }

        // Revenue must be positive
        if (revenue && revenue.getValue() && revenue.getValue()! < 0) {
            Xrm.Navigation.openErrorDialog({ message: "Revenue cannot be negative" });
            return false;
        }

        return true;
    }

    function configureFieldRequirements(formContext: Xrm.FormContext): void {
        formContext.getAttribute("name")?.setRequiredLevel("required");
        formContext.getAttribute("telephone1")?.setRequiredLevel("recommended");
    }

    function setFieldVisibility(formContext: Xrm.FormContext): void {
        const formType = formContext.ui.getFormType();
        
        // Hide certain fields on create
        if (formType === 1) { // Create
            formContext.getControl("revenue")?.setVisible(false);
        }
    }

    async function loadRelatedData(formContext: Xrm.FormContext): Promise<void> {
        const accountId = formContext.data.entity.getId();
        
        if (!accountId) return;

        try {
            // Query related contacts
            const contacts = await Xrm.WebApi.retrieveMultipleRecords(
                "contact",
                `?$select=fullname,emailaddress1&$filter=_parentcustomerid_value eq ${accountId.replace(/[{}]/g, '')}&$top=5`
            );

            // Display count
            if (contacts.entities.length > 0) {
                formContext.ui.setFormNotification(
                    `This account has ${contacts.entities.length} related contacts`,
                    "INFO",
                    "relatedContactsInfo"
                );
            }
        } catch (error) {
            console.error("Error loading related data:", error);
        }
    }

    function showCommercialFields(formContext: Xrm.FormContext): void {
        formContext.getControl("industrycode")?.setVisible(true);
        formContext.getControl("sic")?.setVisible(true);
        formContext.getControl("revenue")?.setVisible(true);
    }

    function showResidentialFields(formContext: Xrm.FormContext): void {
        formContext.getControl("industrycode")?.setVisible(false);
        formContext.getControl("sic")?.setVisible(false);
        formContext.getControl("revenue")?.setVisible(false);
    }
}
```

### 2. Register in Dataverse

1. Build the TypeScript: `npm run build`
2. Upload `dist/forms/account.js` as a web resource in Dataverse
3. Configure form events:
   - **Form Libraries**: Add the web resource
   - **Event Handlers**:
     - onLoad: `Contoso.Forms.Account.onLoad`
     - onChange (accounttype): `Contoso.Forms.Account.onAccountTypeChange`
     - onSave: `Contoso.Forms.Account.onSave`

---

## 🎯 Common Patterns

### Field Validation

```typescript
export function validatePhoneNumber(executionContext: Xrm.Events.EventContext): void {
    const formContext = executionContext.getFormContext();
    const phoneAttr = formContext.getAttribute<Xrm.Attributes.StringAttribute>("telephone1");
    
    if (!phoneAttr) return;

    const phone = phoneAttr.getValue();
    const phoneRegex = /^\+?[\d\s\-()]+$/;

    if (phone && !phoneRegex.test(phone)) {
        formContext.getControl("telephone1")?.setNotification(
            "Invalid phone number format",
            "PHONE_FORMAT_ERROR"
        );
        phoneAttr.setValue(null); // Clear invalid value
    } else {
        formContext.getControl("telephone1")?.clearNotification("PHONE_FORMAT_ERROR");
    }
}
```

### Dynamic Lookups

```typescript
export function filterContactLookup(executionContext: Xrm.Events.EventContext): void {
    const formContext = executionContext.getFormContext();
    const accountType = formContext.getAttribute<Xrm.Attributes.OptionSetAttribute>("accounttype")?.getValue();

    if (accountType === 1) { // Commercial
        // Filter primary contact lookup to only show business contacts
        const contactControl = formContext.getControl<Xrm.Controls.LookupControl>("primarycontactid");
        
        contactControl?.addPreSearch(() => {
            const fetchXml = `
                <filter>
                    <condition attribute="contacttype" operator="eq" value="1" />
                </filter>
            `;
            contactControl.addCustomFilter(fetchXml, "contact");
        });
    }
}
```

### Web API Queries

```typescript
async function getAccountRevenue(accountId: string): Promise<number | null> {
    try {
        const account = await Xrm.WebApi.retrieveRecord(
            "account",
            accountId,
            "?$select=revenue"
        );

        return account.revenue ?? null;
    } catch (error) {
        console.error("Error retrieving account revenue:", error);
        return null;
    }
}
```

---

## 🧪 Testing

### Unit Tests with Jest

```typescript
// tests/forms/account.test.ts
import { onLoad, onAccountTypeChange } from "../../src/forms/account";

describe("Account Form", () => {
    let mockFormContext: Xrm.FormContext;
    let mockExecutionContext: Xrm.Events.EventContext;

    beforeEach(() => {
        // Mock form context
        mockFormContext = {
            getAttribute: jest.fn(),
            getControl: jest.fn(),
            data: {
                entity: {
                    getId: jest.fn().mockReturnValue("{123-456}")
                }
            },
            ui: {
                getFormType: jest.fn().mockReturnValue(2), // Update form
                setFormNotification: jest.fn()
            }
        } as any;

        mockExecutionContext = {
            getFormContext: jest.fn().mockReturnValue(mockFormContext)
        } as any;
    });

    test("onLoad should configure required fields", () => {
        const mockNameAttr = {
            setRequiredLevel: jest.fn()
        };

        mockFormContext.getAttribute = jest.fn().mockReturnValue(mockNameAttr);

        onLoad(mockExecutionContext);

        expect(mockNameAttr.setRequiredLevel).toHaveBeenCalledWith("required");
    });

    test("onAccountTypeChange should show commercial fields when type is 1", () => {
        const mockAccountTypeAttr = {
            getValue: jest.fn().mockReturnValue(1)
        };

        const mockControl = {
            setVisible: jest.fn()
        };

        mockFormContext.getAttribute = jest.fn().mockReturnValue(mockAccountTypeAttr);
        mockFormContext.getControl = jest.fn().mockReturnValue(mockControl);

        onAccountTypeChange(mockExecutionContext);

        expect(mockControl.setVisible).toHaveBeenCalledWith(true);
    });
});
```

Run tests:
```bash
npm test
```

---

## 📦 Deployment

### Build for Production

```bash
npm run build
```

### Upload to Dataverse

1. Navigate to **Solutions** in Power Apps
2. Open your solution
3. Add existing **Web Resource**
4. Upload files from `dist/` folder
5. Publish customizations

---

## 📚 Best Practices

✅ **Do**:
- Use TypeScript for type safety
- Always get formContext from executionContext
- Handle null/undefined values
- Use async/await for Web API calls
- Clear notifications after fixing issues
- Use namespaces to avoid conflicts
- Log errors to console

❌ **Don't**:
- Use `alert()` (use `Xrm.Navigation.openAlertDialog()`)
- Access `window.parent.Xrm` directly
- Use `Xrm.Page` (deprecated - use formContext)
- Make synchronous AJAX calls (use async/await)
- Store sensitive data in client-side code
- Modify DOM directly (use Xrm API)
- Use global variables
- Ignore TypeScript errors

---

## ⚠️ Deprecated Patterns

### 1. Xrm.Page (Deprecated)

**Old Pattern (Deprecated)**:
```typescript
// ❌ Deprecated: Direct Xrm.Page access
function onLoad() {
    var name = Xrm.Page.getAttribute("name");
    name.setValue("New Value");
}
```

**Modern Pattern (Recommended)**:
```typescript
// ✅ Modern: Get formContext from executionContext
export function onLoad(executionContext: Xrm.Events.EventContext): void {
    const formContext = executionContext.getFormContext();
    const name = formContext.getAttribute("name");
    name?.setValue("New Value");
}
```

### 2. window.parent.Xrm (Deprecated)

**Old Pattern (Deprecated)**:
```typescript
// ❌ Deprecated: Accessing parent window
var xrm = window.parent.Xrm;
xrm.Page.getAttribute("name");
```

**Modern Pattern (Recommended)**:
```typescript
// ✅ Modern: Use executionContext passed to event handlers
export function onLoad(executionContext: Xrm.Events.EventContext): void {
    const formContext = executionContext.getFormContext();
    const name = formContext.getAttribute("name");
}
```

### 3. Synchronous XMLHttpRequest (Deprecated)

**Old Pattern (Deprecated)**:
```typescript
// ❌ Deprecated: Synchronous AJAX
var xhr = new XMLHttpRequest();
xhr.open("GET", url, false); // false = synchronous
xhr.send();
var result = xhr.responseText;
```

**Modern Pattern (Recommended)**:
```typescript
// ✅ Modern: Use async/await with Xrm.WebApi
async function getData(entityId: string): Promise<any> {
    try {
        const result = await Xrm.WebApi.retrieveRecord("account", entityId, "?$select=name");
        return result;
    } catch (error) {
        console.error("Error retrieving data:", error);
        throw error;
    }
}
```

### 4. alert() / confirm() / prompt() (Deprecated)

**Old Pattern (Deprecated)**:
```typescript
// ❌ Deprecated: Browser alert dialogs
alert("Record saved successfully!");
var result = confirm("Are you sure?");
```

**Modern Pattern (Recommended)**:
```typescript
// ✅ Modern: Use Xrm.Navigation dialogs
await Xrm.Navigation.openAlertDialog({ 
    text: "Record saved successfully!" 
});

const confirmResult = await Xrm.Navigation.openConfirmDialog({ 
    text: "Are you sure?",
    title: "Confirm Action"
});

if (confirmResult.confirmed) {
    // User clicked OK
}
```

### 5. Direct DOM Manipulation (Deprecated)

**Old Pattern (Deprecated)**:
```typescript
// ❌ Deprecated: Direct DOM manipulation
document.getElementById("name").style.display = "none";
```

**Modern Pattern (Recommended)**:
```typescript
// ✅ Modern: Use Xrm API
export function hideField(formContext: Xrm.FormContext): void {
    const control = formContext.getControl("name");
    control?.setVisible(false);
}
```

### Benefits of Modern Patterns

1. **Cross-browser compatibility**: Xrm API works consistently across browsers
2. **Future-proof**: Microsoft maintains the Xrm API for backward compatibility
3. **Better error handling**: Modern APIs provide better error information
4. **Type safety**: TypeScript definitions available for Xrm API
5. **Unified experience**: Consistent with Power Platform development patterns

---

## 🔗 Resources

- [Client API Reference](https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/clientapi/reference)
- [TypeScript Standards](/docs/standards/typescript-coding-standards.md)
- [Xrm TypeScript Definitions](https://github.com/DefinitelyTyped/DefinitelyTyped/tree/master/types/xrm)
- [Deprecated Client API](https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/clientapi/client-api-deprecated)

---

**Questions? Contact the Frontend team or open a [GitHub Discussion](https://github.com/ivoarnet/test-msd-monorepo/discussions).**
