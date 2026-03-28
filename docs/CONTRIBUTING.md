# Contributing to PredictLottoNZ

Thank you for your interest in contributing to PredictLottoNZ! This document provides guidelines and information for contributors.

## 🚀 Getting Started

### Prerequisites
- Docker and Docker Compose
- Git
- Basic knowledge of .NET, Vue.js, or Python (depending on your contribution area)

### Development Setup
1. Fork the repository
2. Clone your fork:
   ```bash
   git clone https://github.com/your-username/predict-lotto-nz.git
   cd predict-lotto-nz
   ```
3. Set up development environment:
   ```bash
   cp .env.example .env
   docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d
   ```

## 📋 How to Contribute

### Reporting Bugs
1. Check existing issues to avoid duplicates
2. Use the bug report template
3. Include:
   - Environment details (OS, Docker version)
   - Steps to reproduce
   - Expected vs actual behavior
   - Logs and error messages

### Suggesting Features
1. Check existing feature requests
2. Use the feature request template
3. Provide:
   - Clear description of the feature
   - Use cases and benefits
   - Possible implementation approach

### Code Contributions

#### Branch Naming
- `feature/description` - New features
- `bugfix/description` - Bug fixes
- `docs/description` - Documentation updates
- `refactor/description` - Code refactoring

#### Commit Messages
Follow conventional commits format:
```
type(scope): description

[optional body]

[optional footer]
```

Examples:
- `feat(backend): add CSV duplicate detection`
- `fix(frontend): resolve upload progress bar issue`
- `docs(readme): update deployment instructions`

## 🧪 Testing Requirements

### All Contributions Must Include Tests
- **Backend**: Unit tests and property-based tests
- **Frontend**: Component tests and integration tests
- **Predictor**: Unit tests and API tests

### Running Tests
```bash
# Backend
cd backend && dotnet test

# Frontend
cd frontend && npm run test:run

# Predictor
cd predictor && python -m pytest
```

### Test Coverage
- Maintain or improve existing test coverage
- New features require comprehensive test coverage
- Property-based tests for core algorithms

## 📝 Code Style Guidelines

### .NET Backend
- Follow Microsoft C# coding conventions
- Use async/await for I/O operations
- Implement comprehensive error handling
- Add XML documentation for public APIs
- Use dependency injection patterns

### Vue.js Frontend
- Use TypeScript for type safety
- Follow Vue 3 Composition API patterns
- Use Pinia for state management
- Implement proper component props validation
- Follow ESLint configuration

### Python Predictor
- Follow PEP 8 style guidelines
- Use type hints for function signatures
- Implement proper async patterns
- Add docstrings for all functions
- Use Black for code formatting

## 🔍 Code Review Process

### Pull Request Requirements
1. **Clear description** of changes
2. **Reference related issues** using keywords (fixes #123)
3. **All tests passing** in CI/CD
4. **Code review approval** from maintainers
5. **Documentation updates** if needed

### Review Criteria
- Code quality and maintainability
- Test coverage and quality
- Performance considerations
- Security implications
- Documentation completeness

## 🏗️ Architecture Guidelines

### Backend (.NET Core)
- Follow clean architecture principles
- Use service layer for business logic
- Implement repository pattern for data access
- Use DTOs for API contracts
- Implement proper logging

### Frontend (Vue.js)
- Component-based architecture
- Separation of concerns
- Reactive state management
- Proper error handling
- Accessibility compliance

### Predictor (Python FastAPI)
- RESTful API design
- Async request handling
- Proper error responses
- Input validation
- Performance optimization

## 📚 Documentation Standards

### Code Documentation
- XML documentation for .NET public APIs
- JSDoc comments for TypeScript functions
- Python docstrings for all functions
- Inline comments for complex logic

### README Updates
- Update relevant sections for new features
- Include usage examples
- Update API documentation
- Add troubleshooting information

## 🚦 CI/CD Pipeline

### Automated Checks
- Code compilation and building
- Unit and integration tests
- Code style and linting
- Security vulnerability scanning
- Docker image building

### Quality Gates
- All tests must pass
- Code coverage thresholds
- No critical security issues
- Successful Docker builds

## 🤝 Community Guidelines

### Code of Conduct
- Be respectful and inclusive
- Provide constructive feedback
- Help newcomers learn
- Focus on technical merit

### Communication
- Use GitHub issues for bug reports and feature requests
- Use pull request comments for code discussions
- Be clear and concise in communications
- Provide context and examples

## 📋 Checklist for Contributors

Before submitting a pull request:

- [ ] Code follows project style guidelines
- [ ] Tests are written and passing
- [ ] Documentation is updated
- [ ] Commit messages follow conventions
- [ ] Branch is up to date with main
- [ ] No merge conflicts
- [ ] CI/CD pipeline passes

## 🎯 Areas for Contribution

### High Priority
- Performance optimizations
- Additional prediction algorithms
- Enhanced error handling
- Mobile responsiveness improvements

### Medium Priority
- Additional file format support
- User interface enhancements
- API documentation improvements
- Deployment automation

### Low Priority
- Code refactoring
- Additional tests
- Documentation improvements
- Development tooling

## 📞 Getting Help

### Resources
- Project documentation in README.md
- API documentation at runtime endpoints
- Existing issues and pull requests
- Code comments and documentation

### Contact
- Create GitHub issues for questions
- Use discussions for general questions
- Tag maintainers for urgent issues

Thank you for contributing to PredictLottoNZ! 🎉