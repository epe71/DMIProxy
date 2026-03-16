# Copilot Instructions – .NET Project Rules
You are working in a C#/.NET solution.

## Technology Stack

- **.NET**: 10.0
- **Framework**: ASP.NET Core
- **Caching**: FusionCache (distributed caching)
- **Resilience**: Polly (retry policies, circuit breaker)
- **Logging**: Serilog with distributed tracing
- **Testing**: MSTest with Moq
- **API Documentation**: Swagger/OpenAPI
- **Containerization**: Docker with health checks

## Project Structure

`
DMIProxy/
+-- Controllers/          # API endpoints
+-- ApplicationService/   # Business logic orchestration
+-- DomainService/        # External API integrations
+-- BusinessEntity/       # Domain models
+-- Contract/             # DTOs and interfaces
+-- HealthCheck/          # Service health checks
+-- Dockerfile            # Container configuration
+-- Program.cs            # Application setup
`

## Testing

### Test Data Builders (`Builders/`)
Reusable test fixtures following the builder pattern:
- **MockDateTimeProviderBuilder**: Creates mocked `IDateTimeProvider` instances for time-dependent tests
  - Supports single `DateTime` setup with `WithDateTime()`
  - Supports time sequence with `WithUtcTimeSequnce()`
  - Used across multiple test classes for consistent time mocking

### Testing Best Practices Observed

#### Test Naming Convention
Test method names are descriptive and action-focused:
- Format: `{MethodName}_Description_ExpectedBehavior` or `{MethodName}_ShouldBehavior`

#### Arrange-Act-Assert (AAA) Pattern
All tests follow the **Arrange-Act-Assert** structure for clear and consistent organization:

