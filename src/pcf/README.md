# PowerApps Component Framework (PCF)

Custom UI controls for Microsoft Dynamics 365 model-driven apps.

---

## 📋 Overview

PCF enables you to create reusable custom controls:
- Data grids with advanced features
- File uploaders
- Charts and visualizations
- Integration with third-party libraries
- Custom input components

---

## 🏗️ Structure

```
pcf/
├── components/
│   ├── DataGrid/                  # Example: Data grid component
│   │   ├── DataGrid/              # Generated PCF project
│   │   │   ├── index.ts           # Main component logic
│   │   │   ├── ControlManifest.Input.xml
│   │   │   └── css/
│   │   ├── package.json
│   │   └── README.md
│   └── FileUploader/              # Example: File upload component
├── shared/
│   └── utils/                     # Shared TypeScript utilities
├── package.json                   # Root workspace package.json
└── README.md
```

---

## 🚀 Getting Started

### Prerequisites

- Node.js 18+ and npm
- Power Platform CLI (`pac`)
- Dataverse environment

Install Power Platform CLI:
```bash
npm install -g @microsoft/powerplatform-cli
```

### Initialize Workspace

```bash
cd pcf
npm install
```

---

## 📝 Creating a New PCF Component

### 1. Initialize Component

```bash
cd pcf/components
pac pcf init --namespace Contoso --name DataGrid --template field --framework React
cd DataGrid
npm install
```

### 2. Implement Component

Edit `DataGrid/index.ts`:

```typescript
import { IInputs, IOutputs } from "./generated/ManifestTypes";
import * as React from "react";
import * as ReactDOM from "react-dom";
import { DataGridComponent } from "./DataGridComponent";

export class DataGrid implements ComponentFramework.StandardControl<IInputs, IOutputs> {
    private _context: ComponentFramework.Context<IInputs>;
    private _container: HTMLDivElement;
    private _notifyOutputChanged: () => void;
    private _selectedRecordId: string | null = null;

    /**
     * Initializes the component.
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
     * Called when any value in the property bag has changed.
     */
    public updateView(context: ComponentFramework.Context<IInputs>): void {
        this._context = context;
        this.renderComponent();
    }

    /**
     * Returns outputs for the framework.
     */
    public getOutputs(): IOutputs {
        return {
            selectedRecordId: this._selectedRecordId
        };
    }

    /**
     * Called when the control is to be removed from the DOM tree.
     */
    public destroy(): void {
        ReactDOM.unmountComponentAtNode(this._container);
    }

    private renderComponent(): void {
        ReactDOM.render(
            React.createElement(DataGridComponent, {
                context: this._context,
                onRowSelect: (recordId: string) => {
                    this._selectedRecordId = recordId;
                    this._notifyOutputChanged();
                }
            }),
            this._container
        );
    }
}
```

Create React component `DataGridComponent.tsx`:

```typescript
import * as React from "react";
import { IInputs } from "./generated/ManifestTypes";

interface DataGridProps {
    context: ComponentFramework.Context<IInputs>;
    onRowSelect: (recordId: string) => void;
}

export const DataGridComponent: React.FC<DataGridProps> = ({ context, onRowSelect }) => {
    const [data, setData] = React.useState<any[]>([]);

    React.useEffect(() => {
        loadData();
    }, []);

    const loadData = async () => {
        // Use context.webAPI to query data
        const result = await context.webAPI.retrieveMultipleRecords("account", "?$select=name,accountnumber&$top=10");
        setData(result.entities);
    };

    return (
        <div style={{ padding: "10px" }}>
            <h3>Accounts</h3>
            <table style={{ width: "100%", borderCollapse: "collapse" }}>
                <thead>
                    <tr>
                        <th style={{ border: "1px solid #ccc", padding: "8px" }}>Name</th>
                        <th style={{ border: "1px solid #ccc", padding: "8px" }}>Account Number</th>
                    </tr>
                </thead>
                <tbody>
                    {data.map(record => (
                        <tr key={record.accountid} onClick={() => onRowSelect(record.accountid)} style={{ cursor: "pointer" }}>
                            <td style={{ border: "1px solid #ccc", padding: "8px" }}>{record.name}</td>
                            <td style={{ border: "1px solid #ccc", padding: "8px" }}>{record.accountnumber}</td>
                        </tr>
                    ))}
                </tbody>
            </table>
        </div>
    );
};
```

### 3. Update Manifest

Edit `ControlManifest.Input.xml`:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<manifest>
  <control namespace="Contoso" constructor="DataGrid" version="1.0.0" display-name-key="DataGrid" description-key="DataGrid_Desc" control-type="standard">
    <property name="entityName" display-name-key="EntityName" description-key="EntityName_Desc" of-type="SingleLine.Text" usage="bound" required="true" />
    <property name="selectedRecordId" display-name-key="SelectedRecordId" description-key="SelectedRecordId_Desc" of-type="SingleLine.Text" usage="output" />
    <resources>
      <code path="index.ts" order="1"/>
      <css path="css/DataGrid.css" order="1" />
    </resources>
  </control>
</manifest>
```

---

## 🧪 Testing

### Test Harness

Run the component in a test harness:

```bash
cd pcf/components/DataGrid
npm start watch
```

This opens a browser with your component running in a test environment.

### Manual Testing in Dataverse

1. Build the component:
   ```bash
   npm run build
   ```

2. Push to Dataverse:
   ```bash
   pac pcf push --publisher-prefix contoso
   ```

3. Add the component to a form in the form designer

---

## 🎨 Styling

### CSS

Add styles in `css/DataGrid.css`:

```css
.dataGridContainer {
    padding: 10px;
    font-family: "Segoe UI", sans-serif;
}

.dataGridTable {
    width: 100%;
    border-collapse: collapse;
}

.dataGridTable th {
    background-color: #0078d4;
    color: white;
    padding: 10px;
    text-align: left;
}

.dataGridTable td {
    border: 1px solid #ddd;
    padding: 8px;
}

.dataGridTable tr:hover {
    background-color: #f5f5f5;
    cursor: pointer;
}
```

### Fluent UI Integration

Use Microsoft's Fluent UI React library:

```bash
npm install @fluentui/react
```

```typescript
import { DetailsList, IColumn } from "@fluentui/react/lib/DetailsList";

const columns: IColumn[] = [
    { key: "name", name: "Name", fieldName: "name", minWidth: 100, maxWidth: 200 },
    { key: "accountnumber", name: "Account Number", fieldName: "accountnumber", minWidth: 100, maxWidth: 150 }
];

return (
    <DetailsList
        items={data}
        columns={columns}
        onItemInvoked={(item) => onRowSelect(item.accountid)}
    />
);
```

---

## 📦 Building and Packaging

### Build Component

```bash
cd pcf/components/DataGrid
npm run build
```

Output: `out/controls/DataGrid.js`

### Create Solution

Package the component as a Dataverse solution:

```bash
pac solution init --publisher-name Contoso --publisher-prefix contoso
pac solution add-reference --path ../DataGrid
msbuild /t:build /restore
```

Import the solution ZIP into Dataverse.

---

## 🚀 Deployment

### Development

Push directly to dev environment:

```bash
pac auth create --url https://yourorg.crm.dynamics.com
pac pcf push --publisher-prefix contoso
```

### CI/CD

See [pcf-ci.yml](/.github/workflows/pcf-ci.yml) for automated builds and deployments.

---

## 🔧 Configuration

### Using Component Properties

Define properties in `ControlManifest.Input.xml`:

```xml
<property name="pageSize" display-name-key="PageSize" description-key="PageSize_Desc" of-type="Whole.None" usage="input" default-value="10" />
```

Access in code:

```typescript
const pageSize = context.parameters.pageSize.raw ?? 10;
```

### Using Resources

Reference external libraries:

```xml
<resources>
  <code path="index.ts" order="1"/>
  <platform-library name="React" version="16.8.6" />
  <platform-library name="Fluent" version="8.29.0" />
</resources>
```

---

## 📚 Best Practices

✅ **Do**:
- Use React for complex UIs
- Leverage Fluent UI for consistency
- Optimize rendering (React.memo, useMemo)
- Handle loading states
- Provide error messages
- Test with different screen sizes
- Use context.webAPI for data access

❌ **Don't**:
- Make direct fetch() calls (use context.webAPI)
- Store large state in component (use Dataverse)
- Ignore accessibility
- Hard-code strings (use resource strings)
- Block the UI thread
- Forget to clean up event listeners

---

## 🔗 Resources

- [PCF Documentation](https://learn.microsoft.com/en-us/power-apps/developer/component-framework/overview)
- [PCF Gallery](https://pcf.gallery/)
- [Fluent UI React](https://developer.microsoft.com/en-us/fluentui#/controls/web)
- [Power Platform CLI](https://learn.microsoft.com/en-us/power-platform/developer/cli/introduction)
- [TypeScript Standards](/docs/standards/typescript-coding-standards.md)

---

## 📋 Examples

Check out these example components:
- [DataGrid](./components/DataGrid/) - Sortable, filterable data grid
- [FileUploader](./components/FileUploader/) - Drag-and-drop file upload

---

**Questions? Contact the Frontend team or open a [GitHub Discussion](https://github.com/ivoarnet/test-msd-monorepo/discussions).**
