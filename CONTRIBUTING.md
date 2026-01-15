# Contributing to Dynamics 365 Monorepo

Thank you for considering contributing to this project! This document outlines the process and guidelines.

---

## 📋 Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [How to Contribute](#how-to-contribute)
- [Coding Standards](#coding-standards)
- [Commit Messages](#commit-messages)
- [Pull Request Process](#pull-request-process)
- [Issue Reporting](#issue-reporting)

---

## 🤝 Code of Conduct

We are committed to providing a welcoming and inclusive environment. Please:

- Be respectful and considerate
- Welcome newcomers and help them get started
- Focus on constructive feedback
- Respect differing viewpoints and experiences

Unacceptable behavior will not be tolerated.

---

## 🚀 Getting Started

1. **Fork the repository** (if external contributor)
2. **Clone your fork**:
   ```bash
   git clone https://github.com/your-username/test-msd-monorepo.git
   cd test-msd-monorepo
   ```
3. **Set up your environment**: Follow the [Getting Started Guide](/docs/developer-guide/getting-started.md)
4. **Create a branch**:
   ```bash
   git checkout -b feature/your-feature-name
   ```

---

## 💡 How to Contribute

### Types of Contributions

- **Bug fixes**: Fix issues reported in GitHub Issues
- **New features**: Add functionality (discuss in an issue first)
- **Documentation**: Improve READMEs, guides, and comments
- **Tests**: Add or improve test coverage
- **Refactoring**: Improve code quality without changing behavior

### Before You Start

1. **Check existing issues**: Avoid duplicate work
2. **Discuss major changes**: Open an issue to discuss significant changes before coding
3. **Review documentation**: Understand the architecture and standards

---

## 📝 Coding Standards

Follow the established coding standards:

- **Plugins (.NET)**: [.NET Coding Standards](/docs/standards/dotnet-coding-standards.md)
- **TypeScript**: [TypeScript Standards](/docs/standards/typescript-coding-standards.md)
- **Terraform**: [Terraform Best Practices](/docs/standards/terraform-best-practices.md)

### Key Principles

- Write clean, readable code
- Add comments for complex logic
- Write unit tests for new code
- Keep functions small and focused
- Use meaningful variable and function names

---

## ✍️ Commit Messages

Use [Conventional Commits](https://www.conventionalcommits.org/) format:

```
<type>(<scope>): <description>

[optional body]

[optional footer(s)]
```

### Types

- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `style`: Code style changes (formatting, no logic change)
- `refactor`: Code refactoring
- `test`: Adding or updating tests
- `chore`: Maintenance tasks

### Examples

```
feat(plugins): add validation for account phone numbers

Implements phone number validation in AccountCreatePlugin.
Validates format and length before saving.

Closes #123
```

```
fix(pcf): resolve data grid sorting issue

Data grid was not sorting correctly when column header clicked.
Fixed by updating sort handler logic.
```

```
docs(readme): update installation instructions

Added Node.js version requirement and clarified setup steps.
```

---

## 🔄 Pull Request Process

### 1. Prepare Your PR

- Ensure all tests pass locally
- Run linters and fix any warnings
- Update documentation if needed
- Add or update tests for your changes

### 2. Create Pull Request

- Push your branch to GitHub
- Open a pull request against `main` branch
- Fill out the PR template completely
- Link related issues (e.g., "Closes #123")

### 3. PR Description Should Include

- **What**: Brief description of changes
- **Why**: Reason for the changes
- **How**: Technical approach (if complex)
- **Testing**: How you tested the changes
- **Screenshots**: If UI changes

### 4. Review Process

- **Automated checks**: CI/CD pipelines must pass
- **Code review**: At least one approval from CODEOWNERS
- **Address feedback**: Respond to all review comments
- **Keep PR updated**: Resolve merge conflicts if they arise

### 5. After Approval

- Maintainer will merge using "Squash and merge"
- Delete your branch after merge

---

## 🐛 Issue Reporting

### Before Creating an Issue

1. **Search existing issues**: Check if already reported
2. **Reproduce the bug**: Ensure it's consistently reproducible
3. **Collect information**: Error messages, logs, environment details

### Creating a Good Issue

Include:

- **Clear title**: Summarize the problem
- **Description**: Detailed explanation
- **Steps to reproduce**: Numbered list
- **Expected behavior**: What should happen
- **Actual behavior**: What actually happens
- **Environment**: OS, .NET version, Node version, etc.
- **Logs**: Relevant error messages or stack traces
- **Screenshots**: If applicable

### Issue Template Example

```markdown
**Describe the bug**
A clear and concise description of what the bug is.

**To Reproduce**
Steps to reproduce the behavior:
1. Go to '...'
2. Click on '...'
3. See error

**Expected behavior**
A clear description of what you expected to happen.

**Environment:**
- OS: [e.g., Windows 11]
- .NET Version: [e.g., 6.0.1]
- Node Version: [e.g., 18.16.0]

**Additional context**
Add any other context about the problem here.
```

---

## 🏷️ Issue Labels

- `bug`: Something isn't working
- `enhancement`: New feature or request
- `documentation`: Improvements to documentation
- `good-first-issue`: Good for newcomers
- `help-wanted`: Extra attention needed
- `question`: Further information requested
- `duplicate`: This issue already exists
- `wontfix`: This will not be worked on

---

## 🧪 Testing Guidelines

### Required Tests

- **Unit tests**: For all business logic
- **Integration tests**: For API endpoints and database interactions
- **End-to-end tests**: For critical workflows (optional)

### Test Quality

- Tests should be isolated and independent
- Use meaningful test names
- Follow AAA pattern (Arrange, Act, Assert)
- Mock external dependencies

---

## 📚 Additional Resources

- [Getting Started Guide](/docs/developer-guide/getting-started.md)
- [Architecture Decisions](/docs/architecture/)
- [Coding Standards](/docs/standards/)
- [CI/CD Overview](/docs/developer-guide/cicd-overview.md)

---

## 💬 Questions?

- Open a [GitHub Discussion](https://github.com/ivoarnet/test-msd-monorepo/discussions)
- Check existing [Issues](https://github.com/ivoarnet/test-msd-monorepo/issues)
- Review [documentation](/docs/)

---

**Thank you for contributing! Your efforts help make this project better for everyone.** 🎉
