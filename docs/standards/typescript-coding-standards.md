# TypeScript Coding Standards

TypeScript standards for PCF components and client scripts in Dynamics 365 development.

---

## 📋 General Principles

1. **Type Safety**: Leverage TypeScript's type system fully
2. **Readability**: Code is read more than written
3. **Modern JavaScript**: Use ES6+ features
4. **Xrm API First**: Use Dataverse TypeScript definitions
5. **Error Handling**: Gracefully handle failures in browser environment

---

## 🏗️ File Structure

### Client Scripts

```typescript
// accountForm.ts
/**
 * Form scripts for Account entity.
 * Handles onLoad, onChange, and onSave events.
 */

namespace Contoso.Account {
    
    /**
     * Form onLoad event handler.
     */
    export function onLoad(executionContext: Xrm.Events.EventContext): void {
        const formContext = executionContext.getFormContext();
        
        // Initialize form
        configureFields(formContext);
        loadRelatedData(formContext);
    }

    /**
     * Field onChange event handler.
     */
    export function onAccountTypeChange(executionContext: Xrm.Events.EventContext): void {
        const formContext = executionContext.getFormContext();
        const accountType = formContext.getAttribute<Xrm.Attributes.OptionSetAttribute>("accounttype");
        
        if (accountType.getValue() === 2) { // Commercial
            showCommercialFields(formContext);
        }
    }

    /**
     * Form onSave event handler.
     */
    export function onSave(executionContext: Xrm.Events.SaveEventContext): void {
        const formContext = executionContext.getFormContext();
        
        if (!validateForm(formContext)) {
            executionContext.getEventArgs().preventDefault();
        }
    }

    // Private helper functions
    function configureFields(formContext: Xrm.FormContext): void {
        // Implementation
    }
}
```

### PCF Components

```typescript
// DataGrid/index.ts
import { IInputs, IOutputs } from "./generated/ManifestTypes";

export class DataGrid implements ComponentFramework.StandardControl<IInputs, IOutputs> {
    private _context: ComponentFramework.Context<IInputs>;
    private _container: HTMLDivElement;
    private _notifyOutputChanged: () => void;

    /**
     * Initializes the component instance.
     */
    public init(
        context: ComponentFramework.Context<IInputs>,
        notifyOutputChanged: () => void,
        state: ComponentFramework.Dictionary,
        container: HTMLDivElement
    ): void {
        this._context = context;
        this._notifyOutputChanged = notifyOutputChanged;
        this._container = container;

        this.renderComponent();
    }

    /**
     * Updates the view when data changes.
     */
    public updateView(context: ComponentFramework.Context<IInputs>): void {
        this._context = context;
        this.renderComponent();
    }

    /**
     * Returns output values to the framework.
     */
    public getOutputs(): IOutputs {
        return {
            selectedRecordId: this._selectedRecordId
        };
    }

    /**
     * Cleans up resources.
     */
    public destroy(): void {
        // Cleanup logic
    }

    private renderComponent(): void {
        // Rendering logic
    }
}
```

---

## 📝 Naming Conventions

| Element | Convention | Example |
|---------|-----------|---------|
| **File** | camelCase.ts | `accountForm.ts`, `webApiHelper.ts` |
| **Namespace** | PascalCase | `Contoso.Account` |
| **Function** | camelCase | `onLoad()`, `validatePhoneNumber()` |
| **Class** | PascalCase | `DataGrid`, `ApiClient` |
| **Interface** | PascalCase with `I` prefix | `IFormContext`, `IApiResponse` |
| **Type** | PascalCase | `AccountType`, `ContactRole` |
| **Constant** | UPPER_SNAKE_CASE | `MAX_RETRY_COUNT`, `API_BASE_URL` |
| **Private field** | _camelCase | `_context`, `_selectedId` |

---

## 🎯 TypeScript Best Practices

### 1. Use Strong Typing

```typescript
// ✅ Good - strongly typed
interface Account {
    id: string;
    name: string;
    accountType: number;
    revenue?: number;
}

function getAccount(id: string): Promise<Account> {
    return Xrm.WebApi.retrieveRecord("account", id, "?$select=name,accounttype,revenue")
        .then(result => result as Account);
}

// ❌ Bad - any type defeats purpose of TypeScript
function getAccount(id: any): Promise<any> {
    return Xrm.WebApi.retrieveRecord("account", id);
}
```

### 2. Null/Undefined Checks

```typescript
// ✅ Good - explicit null checks
function getAttributeValue(formContext: Xrm.FormContext, attributeName: string): string | null {
    const attribute = formContext.getAttribute(attributeName);
    
    if (!attribute) {
        console.warn(`Attribute ${attributeName} not found`);
        return null;
    }

    const value = attribute.getValue();
    return value ?? null;
}

// ❌ Bad - assuming attribute exists
function getAttributeValue(formContext: Xrm.FormContext, attributeName: string): string {
    return formContext.getAttribute(attributeName).getValue();
}
```

### 3. Use Async/Await

```typescript
// ✅ Good - async/await is cleaner
async function loadAccountDetails(accountId: string): Promise<void> {
    try {
        const account = await Xrm.WebApi.retrieveRecord("account", accountId, "?$select=name,revenue");
        const contacts = await Xrm.WebApi.retrieveMultipleRecords("contact", `?$filter=_parentcustomerid_value eq ${accountId}`);
        
        displayAccountInfo(account, contacts.entities);
    } catch (error) {
        handleError(error);
    }
}

// ❌ Bad - callback hell
function loadAccountDetails(accountId: string): void {
    Xrm.WebApi.retrieveRecord("account", accountId, "?$select=name,revenue")
        .then(account => {
            Xrm.WebApi.retrieveMultipleRecords("contact", `?$filter=_parentcustomerid_value eq ${accountId}`)
                .then(contacts => {
                    displayAccountInfo(account, contacts.entities);
                })
                .catch(error => handleError(error));
        })
        .catch(error => handleError(error));
}
```

### 4. Error Handling

```typescript
// ✅ Good - comprehensive error handling
async function updateRecord(entityName: string, id: string, data: any): Promise<void> {
    try {
        await Xrm.WebApi.updateRecord(entityName, id, data);
        Xrm.Navigation.openAlertDialog({ text: "Record updated successfully" });
    } catch (error) {
        console.error("Update failed:", error);
        
        let errorMessage = "An unexpected error occurred";
        if (error.message) {
            errorMessage = error.message;
        }
        
        Xrm.Navigation.openErrorDialog({ message: errorMessage });
    }
}

// ❌ Bad - no error handling
async function updateRecord(entityName: string, id: string, data: any): Promise<void> {
    await Xrm.WebApi.updateRecord(entityName, id, data);
    alert("Updated"); // Never use alert()
}
```

---

## 🔍 Xrm API Patterns

### Form Context

```typescript
// Always get form context from execution context
export function onLoad(executionContext: Xrm.Events.EventContext): void {
    const formContext = executionContext.getFormContext();
    
    // Work with form context
    const nameAttribute = formContext.getAttribute<Xrm.Attributes.StringAttribute>("name");
    nameAttribute.setRequiredLevel("required");
}
```

### Attribute Access

```typescript
// ✅ Good - type-safe attribute access
function getOptionSetValue(formContext: Xrm.FormContext, attributeName: string): number | null {
    const attribute = formContext.getAttribute<Xrm.Attributes.OptionSetAttribute>(attributeName);
    
    if (!attribute) {
        return null;
    }

    return attribute.getValue();
}

// Set attribute value with validation
function setAttributeValue(
    formContext: Xrm.FormContext,
    attributeName: string,
    value: any
): void {
    const attribute = formContext.getAttribute(attributeName);
    
    if (attribute) {
        attribute.setValue(value);
        attribute.setSubmitMode("always");
    }
}
```

### Web API Queries

```typescript
// ✅ Good - parameterized query
async function getActiveContacts(accountId: string): Promise<any[]> {
    const query = `?$select=firstname,lastname,emailaddress1` +
                  `&$filter=_parentcustomerid_value eq ${accountId} and statecode eq 0` +
                  `&$orderby=lastname asc`;
    
    const result = await Xrm.WebApi.retrieveMultipleRecords("contact", query);
    return result.entities;
}

// ✅ Good - using FetchXML for complex queries
async function getAccountsWithRevenue(): Promise<any[]> {
    const fetchXml = `
        <fetch>
            <entity name="account">
                <attribute name="name" />
                <attribute name="revenue" />
                <filter>
                    <condition attribute="revenue" operator="gt" value="1000000" />
                </filter>
                <order attribute="revenue" descending="true" />
            </entity>
        </fetch>
    `;
    
    const result = await Xrm.WebApi.retrieveMultipleRecords("account", `?fetchXml=${encodeURIComponent(fetchXml)}`);
    return result.entities;
}
```

---

## 🎨 Code Style

### Formatting

- **Indentation**: 2 spaces (TypeScript/JavaScript standard)
- **Line length**: 120 characters max
- **Semicolons**: Always use
- **Quotes**: Single quotes for strings (unless JSX)
- **Trailing commas**: Use in multi-line objects/arrays

```typescript
// ✅ Good
const config = {
  apiUrl: 'https://api.example.com',
  timeout: 5000,
  retryCount: 3,
};

// ❌ Bad
const config = {
  apiUrl: "https://api.example.com",
  timeout: 5000,
  retryCount: 3
}
```

### Function Structure

```typescript
// ✅ Good - clear, single responsibility
export async function validateAndSaveForm(formContext: Xrm.FormContext): Promise<boolean> {
    if (!validateRequiredFields(formContext)) {
        showValidationError("Please fill in all required fields");
        return false;
    }

    if (!validateBusinessRules(formContext)) {
        showValidationError("Business rule validation failed");
        return false;
    }

    await formContext.data.save();
    return true;
}

function validateRequiredFields(formContext: Xrm.FormContext): boolean {
    // Validation logic
    return true;
}

function validateBusinessRules(formContext: Xrm.FormContext): boolean {
    // Business rule logic
    return true;
}

// ❌ Bad - does too much, hard to test
export async function doEverything(formContext: Xrm.FormContext): Promise<void> {
    // Validation + saving + navigation + notifications all mixed together
}
```

---

## 📖 Documentation

### JSDoc Comments

```typescript
/**
 * Calculates the discount based on customer type and order amount.
 * 
 * @param customerType - The type of customer (1 = Standard, 2 = Premium)
 * @param orderAmount - The total order amount in USD
 * @returns The discount percentage (0-100)
 * 
 * @example
 * ```typescript
 * const discount = calculateDiscount(2, 1000); // Returns 15 for premium customer
 * ```
 */
export function calculateDiscount(customerType: number, orderAmount: number): number {
    if (customerType === 2 && orderAmount > 500) {
        return 15; // Premium customers get 15% on orders over $500
    }
    
    return 5; // Standard 5% discount
}
```

---

## 🧪 Testing

### Unit Tests (Jest)

```typescript
// accountForm.test.ts
import { onLoad, onAccountTypeChange } from './accountForm';

describe('Account Form Tests', () => {
    let mockFormContext: Xrm.FormContext;
    let mockExecutionContext: Xrm.Events.EventContext;

    beforeEach(() => {
        // Setup mocks
        mockFormContext = createMockFormContext();
        mockExecutionContext = {
            getFormContext: () => mockFormContext,
        } as Xrm.Events.EventContext;
    });

    test('onLoad should configure fields correctly', () => {
        onLoad(mockExecutionContext);

        const nameAttribute = mockFormContext.getAttribute('name');
        expect(nameAttribute.getRequiredLevel()).toBe('required');
    });

    test('onAccountTypeChange should show commercial fields when type is 2', () => {
        const accountTypeAttribute = mockFormContext.getAttribute('accounttype');
        accountTypeAttribute.getValue.mockReturnValue(2);

        onAccountTypeChange(mockExecutionContext);

        const commercialField = mockFormContext.getControl('commercialinfo');
        expect(commercialField.getVisible()).toBe(true);
    });
});
```

---

## ✅ Code Review Checklist

- [ ] All functions have type annotations
- [ ] Null/undefined checks present
- [ ] Async/await used (not callbacks)
- [ ] Error handling implemented
- [ ] JSDoc comments on public functions
- [ ] No `any` types (use specific types or `unknown`)
- [ ] No `console.log` in production code (use proper logging)
- [ ] Xrm API accessed via executionContext (not `Xrm.Page` or `window.parent.Xrm`)
- [ ] No deprecated patterns (see Deprecated Patterns section below)
- [ ] ESLint passes with no warnings
- [ ] Prettier formatting applied
- [ ] Unit tests added/updated

---

## ⚠️ Deprecated Patterns

### Xrm.Page is Deprecated

Microsoft deprecated `Xrm.Page` in Dynamics 365 v9.0. Always use `formContext` from `executionContext`.

```typescript
// ❌ Deprecated
function onLoad() {
    var name = Xrm.Page.getAttribute("name");
    Xrm.Page.ui.setFormNotification("Message", "INFO", "1");
}

// ✅ Modern
export function onLoad(executionContext: Xrm.Events.EventContext): void {
    const formContext = executionContext.getFormContext();
    const name = formContext.getAttribute("name");
    formContext.ui.setFormNotification("Message", "INFO", "1");
}
```

### window.parent.Xrm is Not Recommended

Accessing the parent window's Xrm object can cause issues and is not future-proof.

```typescript
// ❌ Not recommended
var xrm = window.parent.Xrm;

// ✅ Modern - use executionContext
export function onLoad(executionContext: Xrm.Events.EventContext): void {
    const formContext = executionContext.getFormContext();
    // Use formContext methods
}
```

### Synchronous XMLHttpRequest is Deprecated

Browsers are deprecating synchronous XHR. Use async/await with Xrm.WebApi.

```typescript
// ❌ Deprecated
var xhr = new XMLHttpRequest();
xhr.open("GET", url, false); // Synchronous
xhr.send();

// ✅ Modern
async function getData(): Promise<any> {
    const result = await Xrm.WebApi.retrieveRecord("account", id, "?$select=name");
    return result;
}
```

### alert(), confirm(), prompt() Should Not Be Used

Use Xrm.Navigation dialogs for consistency with Dynamics 365 UI.

```typescript
// ❌ Deprecated
alert("Success!");
if (confirm("Continue?")) { }

// ✅ Modern
await Xrm.Navigation.openAlertDialog({ text: "Success!" });
const result = await Xrm.Navigation.openConfirmDialog({ text: "Continue?" });
if (result.confirmed) { }
```

### Key Resources on Deprecated APIs

- [Deprecated Client API](https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/clientapi/client-api-deprecated)
- [Best Practices: Client Scripting](https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/best-practices/business-logic/interact-http-https-resources-asynchronously)

---

## 🛠️ Tools

### ESLint Configuration

```json
{
  "extends": [
    "eslint:recommended",
    "plugin:@typescript-eslint/recommended"
  ],
  "rules": {
    "@typescript-eslint/explicit-function-return-type": "error",
    "@typescript-eslint/no-explicit-any": "error",
    "@typescript-eslint/no-unused-vars": "error",
    "no-console": ["warn", { "allow": ["warn", "error"] }]
  }
}
```

### Prettier Configuration

```json
{
  "semi": true,
  "singleQuote": true,
  "tabWidth": 2,
  "trailingComma": "es5",
  "printWidth": 120
}
```

---

## 📚 Resources

- [TypeScript Handbook](https://www.typescriptlang.org/docs/handbook/intro.html)
- [Xrm TypeScript Definitions](https://github.com/DefinitelyTyped/DefinitelyTyped/tree/master/types/xrm)
- [Client API Reference](https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/clientapi/reference)
- [Deprecated Client API](https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/clientapi/client-api-deprecated)

---

**Questions? Open a GitHub Discussion or contact the Frontend team.**
