# Contributing to BackOps Simulator

Thank you for your interest in contributing! This document provides guidelines for contributing to the project.

## 🤝 Code of Conduct

By participating, you agree to abide by our [Code of Conduct](CODE_OF_CONDUCT.md).

## 🚀 Getting Started

1. Fork the repository
2. Clone your fork: `git clone https://github.com/YOUR_USERNAME/Simulador-para-Back-End-DevOps.git`
3. Create a branch: `git checkout -b feature/amazing-feature`
4. Make your changes
5. Run tests: `dotnet test BackOpsSimulator.sln`
6. Commit your changes: `git commit -m 'Add amazing feature'`
7. Push to your fork: `git push origin feature/amazing-feature`
7. Open a Pull Request

## 📋 Pull Request Process

1. Ensure all tests pass
2. Update documentation if needed
3. Add tests for new functionality
4. Follow the existing code style
5. Keep PRs focused and atomic
6. Link related issues

## 🏗️ Development Setup

### Prerequisites
- .NET 9 SDK
- Node.js 20+
- Docker Desktop
- Git

### Running Locally

```bash
# Start infrastructure
docker compose up -d postgres redis jaeger seq

# Run migrations
dotnet ef database update --project src/BackOps.Infrastructure --startup-project apps/api

# Start API
dotnet run --project apps/api

# Start Worker (separate terminal)
dotnet run --project apps/worker

# Start Frontend (separate terminal)
cd apps/web && npm install && npm run dev
```

## 🧪 Testing

### Unit Tests
```bash
dotnet test tests/UnitTests/BackOps.UnitTests.csproj
```

### Integration Tests
```bash
dotnet test tests/IntegrationTests/BackOps.IntegrationTests.csproj
```

### Architecture Tests
```bash
dotnet test tests/ArchitectureTests/BackOps.ArchitectureTests.csproj
```

### Load Tests (k6)
```bash
k6 run load-tests/k6/baseline.js
k6 run load-tests/k6/stress.js
k6 run load-tests/k6/spike.js
```

## 📝 Code Style

### C# (.NET)
- Follow [Microsoft C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use `dotnet format` before committing
- Nullable reference types enabled
- Prefer `record` over `class` for DTOs
- Use primary constructors

### TypeScript/React
- Follow [TypeScript Best Practices](https://typescript-eslint.io/rules/)
- Use functional components with hooks
- Prefer `interface` over `type` for object shapes
- Use TanStack Query for server state
- Tailwind CSS for styling

### Git Commits
Follow [Conventional Commits](https://www.conventionalcommits.org/):
```
feat: add new payment retry logic
fix: resolve race condition in ticket reservation
docs: update API reference for webhooks
test: add integration test for circuit breaker
refactor: extract queue provider interface
```

## 🏷️ Issue Labels

- `good first issue` - Good for newcomers
- `help wanted` - Needs community help
- `bug` - Something isn't working
- `feature` - New functionality
- `enhancement` - Improvement to existing feature
- `documentation` - Documentation updates
- `architecture` - Architecture discussions

## 🔍 Code Review Guidelines

### For Authors
- Keep PRs small (< 400 lines changed)
- Write clear commit messages
- Add tests for new code
- Update documentation
- Self-review before requesting review

### For Reviewers
- Be constructive and respectful
- Focus on correctness, readability, maintainability
- Check for tests and documentation
- Verify architecture consistency
- Approve when ready

## 🐛 Reporting Bugs

Use the bug report template and include:
- Clear title and description
- Steps to reproduce
- Expected vs actual behavior
- Environment details
- Screenshots/logs if applicable

## 💡 Suggesting Features

Use the feature request template and include:
- Problem statement
- Proposed solution
- Alternatives considered
- Implementation approach

## 📚 Documentation

- Update README for user-facing changes
- Add XML comments for public APIs
- Update ADRs for architectural decisions
- Add runbooks for operational procedures

## 🏷️ Versioning

We use [Semantic Versioning](https://semver.org/):
- MAJOR: Breaking changes
- MINOR: New features (backward compatible)
- PATCH: Bug fixes (backward compatible)

## 📄 License

By contributing, you agree that your contributions will be licensed under the MIT License.

---

Thank you for contributing to BackOps Simulator! 🎉