# Copilot Instructions – .NET Project Rules
You are working in a C#/.NET solution.

General rules:

* Follow clean architecture principles.
* Prefer async/await.
* Use dependency injection.
* Avoid static services.
* Follow existing folder structure.
* Use consistent naming conventions.
* Use interfaces for services and repositories.
* Use DTOs for data transfer between layers.
* Use logging and exception handling best practices.
* Write unit tests for new code and ensure existing tests pass when refactoring.
* Use configuration files for settings and secrets, not hardcoded values.
* Use cancellation tokens for long-running operations and API endpoints.
* Use appropriate HTTP status codes in API responses.
* Use Swagger annotations for API documentation.
* Use IDateTimeProvider for time-based operations to allow for testing.
* Use controllers and not minimal API.
* Validate inputs.

When refactoring:

* Suggest multi-file changes.
* Keep namespaces consistent.
* Avoid breaking public contracts.


## Architecture

The project follows clean architecture principles with clear separation of concerns:

- **Controllers**: MetObsController - REST API endpoints
- **Application Services**: Business logic orchestration layer
  - ClimateDataApplicationService
  - EdrApplicationService
  - MetObsApplicationService
  - WeatherForecastService
- **Domain Services**: External integrations and utilities
  - ClimateDataService, EdrService, MetObsService
  - WebScrapeService, NtfyService
  - TimeSpanCalculator, RequestCache
- **DTOs**: Contract objects for request/response models
- **Business Entities**: Core domain models

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

* See [TEST_STRUCTURE.md](TEST_STRUCTURE.md) for test organization and patterns.
