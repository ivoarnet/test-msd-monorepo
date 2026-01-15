# ADR-001: Repository Strategy for Dynamics 365 Development

**Status**: Accepted  
**Date**: 2024-01-15  
**Decision Makers**: Solution Architect, DevOps Team  

---

## Context

We need to establish a repository strategy for Microsoft Dynamics 365 / Power Platform development that supports:
- Multiple technology stacks (Plugins, PCF, Azure Functions, JavaScript/TypeScript)
- Infrastructure as Code (Terraform)
- CI/CD automation
- Developer productivity with GitHub Copilot
- Scalability from small team to enterprise

---

## Decision Comparison: Monorepo vs Multi-Repo

### Monorepo Approach

**Definition**: Single repository containing all components (Plugins, PCF, Azure Functions, Terraform, Dataverse customizations)

#### Pros
✅ **Unified versioning**: Single source of truth, atomic commits across components  
✅ **Simplified dependency management**: Shared libraries easily referenced  
✅ **Easier refactoring**: Cross-component changes in single PR  
✅ **Better Copilot context**: All code accessible for AI-assisted development  
✅ **Simplified developer setup**: Clone once, access everything  
✅ **Consistent tooling**: Shared linting, testing, CI/CD patterns  
✅ **Holistic code reviews**: See full impact of changes  

#### Cons
❌ **Larger repository size**: Longer clone times  
❌ **Complex CI/CD**: Need intelligent build detection (what changed?)  
❌ **Access control**: Harder to limit permissions per component  
❌ **Build times**: May need to build unaffected components  
❌ **Cognitive overhead**: Developers see code outside their domain  

---

### Multi-Repo Approach

**Definition**: Separate repositories per logical component or team boundary

#### Pros
✅ **Clear boundaries**: Isolated concerns, easier to reason about  
✅ **Fine-grained access control**: Team-specific permissions  
✅ **Independent deployment**: Each repo has own release cycle  
✅ **Smaller, faster repos**: Quicker clone and build  
✅ **Technology-specific tooling**: Optimized per stack  

#### Cons
❌ **Version coordination**: Complex to sync interdependent changes  
❌ **Dependency hell**: Managing shared libraries across repos  
❌ **Duplicate tooling**: Repeated CI/CD, linting configs  
❌ **Limited Copilot context**: AI can't see cross-repo relationships  
❌ **Complex developer setup**: Multiple clones, documentation scattered  
❌ **Difficult refactoring**: Cross-repo changes require multiple PRs  

---

## Impact on Dynamics 365 Development

### CI/CD Complexity
- **Monorepo**: Requires path filtering (GitHub Actions supports this well)
- **Multi-repo**: Each repo has independent pipelines (simpler per-repo, complex orchestration)

### Dataverse Solutions
- **Monorepo**: Solutions co-located with related code (Plugins with solution XML)
- **Multi-repo**: Solutions separate from implementation code (loose coupling)

### Shared Components
- **Monorepo**: Shared utilities easily referenced (e.g., common plugin base classes)
- **Multi-repo**: Requires NuGet/npm packages or Git submodules

### Developer Onboarding
- **Monorepo**: Single clone, one README hierarchy
- **Multi-repo**: Multiple repos to discover and clone

### GitHub Copilot Effectiveness
- **Monorepo**: ⭐⭐⭐⭐⭐ Excellent - Full context available
- **Multi-repo**: ⭐⭐⭐ Good - Limited to current repo

---

## Decision: **Start with Monorepo**

### Rationale

For **Dynamics 365 / Power Platform development**, we recommend a **monorepo approach** because:

1. **Tight coupling is reality**: Plugins, workflows, and Dataverse customizations are interdependent
2. **Small to medium teams**: Most D365 projects have 3-15 developers who work across stacks
3. **Copilot optimization**: AI-assisted development benefits from full context
4. **Microsoft's direction**: Power Platform solutions naturally bundle components together
5. **Simplified governance**: Single source of truth for compliance and auditing

### When to Consider Multi-Repo Later

Migrate to multi-repo if:
- Team grows beyond 30 developers
- Clear organizational boundaries emerge (e.g., Core Platform vs Integrations)
- Different components have vastly different release cadences
- Compliance requires strict code isolation

---

## Monorepo Structure Design Principles

### 1. **Logical Separation by Technology**
```
/plugins       - .NET Dataverse plugins
/pcf           - PowerApps Component Framework
/functions     - Azure Functions
/client-scripts - JavaScript/TypeScript for forms
/solutions     - Dataverse solution exports
/terraform     - Infrastructure as Code
```

### 2. **Workspaces for Scaling**
Use npm/yarn workspaces, .NET solution files, and Terraform modules for logical grouping

### 3. **Path-Based CI/CD**
GitHub Actions triggers based on file paths:
```yaml
on:
  push:
    paths:
      - 'plugins/**'
      - '.github/workflows/plugins-ci.yml'
```

### 4. **Clear Naming Conventions**
- **Folders**: lowercase-with-dashes (e.g., `client-scripts`)
- **Projects**: PascalCase (e.g., `Contoso.Plugins`)
- **Files**: Match language conventions (.cs = PascalCase, .ts = camelCase)

### 5. **Documentation Hierarchy**
```
/README.md              - Overview + quick start
/docs/architecture/     - Decision records
/docs/developer-guide/  - Getting started
/docs/standards/        - Coding standards
/<component>/README.md  - Component-specific docs
```

---

## Migration Path (If Needed)

### Phase 1: Monorepo (Today)
All components in single repository

### Phase 2: Logical Modules (Future)
Introduce module boundaries via folders and namespace conventions

### Phase 3: Multi-Repo (If Needed)
1. Extract stable, independent components first (e.g., shared libraries)
2. Use Git subtree/filter-branch to preserve history
3. Publish shared code as packages (NuGet, npm)
4. Update CI/CD to orchestrate across repos

### Tools to Aid Migration
- **Git subtree**: Extract folder to new repo with history
- **Git filter-repo**: Advanced history rewriting
- **Lerna/Nx**: Monorepo build orchestration (if staying monorepo)

---

## Governance and Scaling

### Code Ownership (CODEOWNERS)
Even in a monorepo, use GitHub CODEOWNERS for approval requirements:
```
/plugins/           @team-platform
/pcf/               @team-frontend
/terraform/         @team-devops
```

### Branch Strategy
- **main**: Production-ready code
- **develop**: Integration branch (optional for complex projects)
- **feature/***: Feature branches
- **hotfix/***: Production hotfixes

### Release Strategy
- **Monorepo tags**: `v1.2.3` for coordinated releases
- **Component tags**: `plugins/v1.0.0` if needed independently

---

## References

- [Google's Monorepo Philosophy](https://research.google/pubs/pub45424/)
- [Microsoft's Azure DevOps Monorepo Guidance](https://learn.microsoft.com/en-us/azure/devops/repos/git/git-branching-guidance)
- [GitHub Actions Path Filtering](https://docs.github.com/en/actions/using-workflows/workflow-syntax-for-github-actions#onpushpull_requestpull_request_targetpathspaths-ignore)

---

## Conclusion

**Start with a monorepo** optimized for Dynamics 365 development. This provides the best balance of:
- Developer productivity
- GitHub Copilot effectiveness  
- Maintenance simplicity
- Flexibility to evolve

The structure is designed to scale and can be split later if organizational needs change.
