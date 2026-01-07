# Contributing

Thank you for your interest in contributing to the Universal Paperclips AI Controller!

## Getting Started

1. Fork the repository
2. Clone your fork locally
3. Create a feature branch (`git checkout -b feature/my-feature`)
4. Make your changes
5. Run the build to ensure it compiles (`dotnet build`)
6. Commit your changes (`git commit -m 'Add my feature'`)
7. Push to your branch (`git push origin feature/my-feature`)
8. Open a Pull Request

## Development Setup

```bash
# Prerequisites
- .NET 10 SDK
- OpenAI API key

# Build
dotnet build

# Run
export OPENAI_API_KEY="sk-..."
dotnet run --project src/UniversalPaperclipsAI
```

## Code Style

- Use file-scoped namespaces
- Use `sealed` for classes not intended for inheritance
- Prefer expression-bodied members for single-line methods
- Add XML documentation for public APIs
- Follow existing patterns in the codebase

## Areas for Contribution

See [ISSUES.md](ISSUES.md) for a prioritized list of improvements needed.

### Good First Issues
- Adding XML documentation comments
- Extracting magic numbers to constants
- Adding null checks and input validation
- Improving error messages

### Larger Contributions
- Adding unit tests
- Implementing retry logic for API calls
- Adding support for alternative LLM providers
- Creating a Dockerfile

## Reporting Bugs

When reporting bugs, please include:
- .NET version (`dotnet --version`)
- Operating system
- Steps to reproduce
- Expected vs actual behavior
- Relevant log output

## Feature Requests

Feature requests are welcome! Please open an issue describing:
- The problem you're trying to solve
- Your proposed solution
- Any alternatives you've considered

## Questions?

Feel free to open an issue for questions about the codebase or architecture.
