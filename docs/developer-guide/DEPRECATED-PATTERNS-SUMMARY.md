# Deprecated Patterns Summary

This document provides a quick reference for deprecated patterns found and addressed in the repository documentation.

---

## 🔍 Overview

The repository has been audited for deprecated Dynamics 365 / Dataverse patterns. All documentation has been updated to reflect modern, recommended approaches.

---

## 📊 Findings Summary

### Plugins (.NET)

| Pattern | Status | Impact | Location |
|---------|--------|--------|----------|
| Direct `GetService(typeof(...))` calls | **Deprecated** | Medium | Multiple plugin examples |
| Passing individual services to ExecutePlugin | **Deprecated** | Low | PluginBase pattern |
| Not using `IPluginExecutionContext4` | **Outdated** | Low | Context access examples |

### Client Scripts (TypeScript/JavaScript)

| Pattern | Status | Impact | Location |
|---------|--------|--------|----------|
| `Xrm.Page` | **Deprecated** | High | None found (✅) |
| `window.parent.Xrm` | **Not Recommended** | Medium | Documented as anti-pattern |
| `alert()`, `confirm()`, `prompt()` | **Deprecated** | Medium | None found (✅) |
| Synchronous XMLHttpRequest | **Deprecated** | High | None found (✅) |
| Direct DOM manipulation | **Not Recommended** | Low | Documented as anti-pattern |

---

## ✅ Actions Taken

### Documentation Updates

1. **Updated .NET Coding Standards** (`/docs/standards/dotnet-coding-standards.md`)
   - Added LocalPluginContext pattern as the recommended approach
   - Included IPluginExecutionContext4 and ContextSummary documentation
   - Added deprecation warnings for old patterns
   - Updated all code examples to use modern pattern

2. **Updated Plugin README** (`/src/plugins/README.md`)
   - Replaced old plugin examples with modern LocalPluginContext pattern
   - Added dedicated "Deprecated Patterns" section
   - Included migration benefits and comparison

3. **Updated TypeScript Coding Standards** (`/docs/standards/typescript-coding-standards.md`)
   - Added comprehensive "Deprecated Patterns" section
   - Documented Xrm.Page deprecation
   - Included examples for all deprecated client-side patterns

4. **Updated Client Scripts README** (`/src/client-scripts/README.md`)
   - Added "Deprecated Patterns" section with 5 major anti-patterns
   - Provided side-by-side comparisons of old vs. new patterns
   - Included benefits of modern approaches

5. **Created Migration Guide** (`/docs/developer-guide/deprecated-patterns-migration.md`)
   - Comprehensive guide for migrating from old to new patterns
   - Step-by-step migration instructions
   - Timeline and priority recommendations
   - Complete code examples for both plugins and client scripts

---

## 📋 Deprecated Patterns Documented

### Plugin Patterns

#### 1. Direct GetService Calls (Deprecated)

**Old:**
```csharp
var tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
```

**New:**
```csharp
var localContext = new LocalPluginContext(serviceProvider);
localContext.Trace("Message");
var context = localContext.PluginExecutionContext;
```

#### 2. Individual Service Parameters (Outdated)

**Old:**
```csharp
protected abstract void ExecutePlugin(
    IServiceProvider serviceProvider,
    ITracingService tracingService,
    IPluginExecutionContext context);
```

**New:**
```csharp
protected abstract void ExecutePlugin(LocalPluginContext localContext);
```

#### 3. Not Leveraging IPluginExecutionContext4

**New Addition:**
```csharp
if (localContext.PluginExecutionContext is IPluginExecutionContext4 context4)
{
    localContext.Trace($"Context: {context4.ContextSummary}");
}
```

### Client Script Patterns

#### 1. Xrm.Page (Deprecated since v9.0)

**Old:**
```typescript
Xrm.Page.getAttribute("name").setValue("value");
```

**New:**
```typescript
const formContext = executionContext.getFormContext();
formContext.getAttribute("name")?.setValue("value");
```

#### 2. window.parent.Xrm (Not Recommended)

**Old:**
```typescript
var xrm = window.parent.Xrm;
```

**New:**
```typescript
export function onLoad(executionContext: Xrm.Events.EventContext): void {
    const formContext = executionContext.getFormContext();
}
```

#### 3. Browser Dialogs (Deprecated)

**Old:**
```typescript
alert("Message");
confirm("Question?");
```

**New:**
```typescript
await Xrm.Navigation.openAlertDialog({ text: "Message" });
await Xrm.Navigation.openConfirmDialog({ text: "Question?" });
```

#### 4. Synchronous AJAX (Deprecated)

**Old:**
```typescript
var xhr = new XMLHttpRequest();
xhr.open("GET", url, false); // Synchronous
xhr.send();
```

**New:**
```typescript
const result = await Xrm.WebApi.retrieveRecord("account", id, "?$select=name");
```

#### 5. Direct DOM Manipulation (Not Recommended)

**Old:**
```typescript
document.getElementById("name").style.display = "none";
```

**New:**
```typescript
formContext.getControl("name")?.setVisible(false);
```

---

## 🎯 Current State

### ✅ Clean State Confirmed

The repository documentation and examples:
- ✅ No instances of `Xrm.Page` in examples
- ✅ No `window.parent.Xrm` usage
- ✅ No browser `alert()`, `confirm()`, or `prompt()` calls
- ✅ No synchronous XMLHttpRequest examples
- ✅ Modern patterns used throughout new documentation

### 📚 Documentation Coverage

All major documentation files now include:
- Deprecated pattern warnings
- Modern pattern examples
- Migration guidance
- Benefits of modern approaches
- Links to official Microsoft documentation

---

## 🔗 Key Resources Added

1. **Migration Guide**: `/docs/developer-guide/deprecated-patterns-migration.md`
   - Comprehensive guide for developers
   - Step-by-step migration instructions
   - Timeline recommendations

2. **Updated Standards**:
   - `.NET Coding Standards`: Modern LocalPluginContext pattern
   - `TypeScript Coding Standards`: Deprecated pattern warnings
   - Component READMEs: Modern examples and anti-patterns

3. **External References**:
   - Microsoft's deprecated client API documentation
   - Plugin best practices
   - Understanding the data context

---

## 📅 Recommendations

### For New Development
- ✅ Always use LocalPluginContext pattern for plugins
- ✅ Always get FormContext from ExecutionContext in client scripts
- ✅ Use async/await with Xrm.WebApi
- ✅ Use Xrm.Navigation dialogs instead of browser dialogs

### For Existing Code
- 🔄 Migrate high-priority plugins first (see migration guide)
- 🔄 Update client scripts during maintenance windows
- 🔄 Document any legacy patterns that cannot be immediately updated

### For Team Training
- 📖 Share the migration guide with all developers
- 📖 Review modern patterns in code reviews
- 📖 Update internal training materials

---

## 🎓 Benefits of Migration

### Technical Benefits
- **Better debugging**: Access to IPluginExecutionContext4.ContextSummary
- **Improved testability**: LocalPluginContext can be mocked
- **Type safety**: Full TypeScript support for modern APIs
- **Performance**: Optimized for modern browsers and Dataverse versions
- **Future-proof**: Microsoft actively maintains modern APIs

### Business Benefits
- **Reduced maintenance**: Cleaner, more maintainable code
- **Faster onboarding**: New developers learn modern patterns
- **Lower risk**: Avoid deprecated functionality issues
- **Better support**: Microsoft focuses on modern approaches

---

## 📞 Support

For questions about deprecated patterns:
- Review the [Migration Guide](/docs/developer-guide/deprecated-patterns-migration.md)
- Check [.NET Coding Standards](/docs/standards/dotnet-coding-standards.md)
- Check [TypeScript Coding Standards](/docs/standards/typescript-coding-standards.md)
- Open a GitHub Discussion for team guidance

---

**Audit Completed**: January 2024  
**Status**: ✅ All documentation updated with modern patterns  
**Next Review**: Recommended in 6 months or when Microsoft releases new SDK versions
