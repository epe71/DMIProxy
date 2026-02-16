# DMIProxy - Test Structure Overview

## Testing Framework & Standards
- **Test Framework**: MSTest (using `[TestClass]` and `[TestMethod]` attributes)
- **.NET Version**: .NET 10
- **C# Version**: 14.0
- **Mocking**: Moq library for unit test mocks
- **Caching**: FusionCache for distributed caching
- **Resilience**: Polly for retry policies and circuit breaker patterns
- **Test Builders**: Custom builder pattern used for creating test data

## Test Organization

### 1. **Controller Tests** (`MetObsControllerTests.cs`)
Tests for the `MetObsController` class covering:
- **GetRain()**: Retrieving rain data and returning it as DTO
- **GetEdrForecast()**: Retrieving EDR forecast data
- **Mock Usage**: Uses mocked services (`IMetObsApplicationService`, `IEdrApplicationService`, `IWeatherForecastService`, `IClimateDataApplicationService`, `ILogger`)
- Returns `JsonResult` with properly formatted DTOs

### 2. **ApplicationService Tests** 

#### **ClimateDataApplicationServiceTests.cs**
Tests for the `ClimateDataApplicationService` class covering:
- **GetHeatingDegreeDays()**: Retrieving and transforming heating degree day data
- Integration with `IClimateDataService` for climate data operations
- Cache operations with `FusionCache`
- Data transformation from API format to DTO format

### 3. **DomainService Tests**

#### **RequestCacheTests.cs**
Tests for the `RequestCache` caching behavior:
- **EdrKeyUpdated()**: Saving and updating EDR keys
- **GetAllEdrKeys()**: Retrieving stored keys
- Testing in-memory cache operations with `IMemoryCache`
- Time-based cache expiration logic with mocked `IDateTimeProvider`

#### **TimeSpanCalculatorTests.cs**
Tests for the `TimeSpanCalculator` utility class:
- **FixTime()**: Calculating time span to next scheduled update time
- Parametrized tests with `[DataRow]` attribute for multiple hour scenarios
- Uses mocked `IDateTimeProvider` for consistent testing

#### **ForecastDataCalculatorTests.cs**
Tests for the `AdjustList` data transformation class:
- **Divide()**: Dividing all values in a list by a factor
- **Multiply()**: Multiplying all values in a list by a factor
- Chaining operations with fluent builder pattern

#### **PollyPolicyTests.cs**
Tests for resilience and retry policies:
- **RetryPolicy**: Testing retry behavior on specific HTTP status codes (408, 500, 503)
- Mock HTTP handler to simulate transient failures
- Parametrized tests with `[DataRow]` for different status codes
- Verifies circuit breaker and retry strategies work correctly

### 4. **Utility Service Tests**

#### **MSTestSettings.cs**
Global test configuration for MSTest framework.

## Test Data Builders (`Builders/`)
Reusable test fixtures following the builder pattern:
- **MockDateTimeProviderBuilder**: Creates mocked `IDateTimeProvider` instances for time-dependent tests
  - Supports single `DateTime` setup with `WithDateTime()`
  - Supports time sequence with `WithUtcTimeSequnce()`
  - Used across multiple test classes for consistent time mocking

## Testing Best Practices Observed

### Test Naming Convention
Test method names are descriptive and action-focused:
- Format: `{MethodName}_Description_ExpectedBehavior` or `{MethodName}_ShouldBehavior`
- Examples from codebase:
  - `GetRain_ReturnsExpectedRainDTO`
  - `GetEdrForecast_ReturnsExpectedForecastDTO`
  - `SaveEdrKeys_TwoDistinctKeys_ShouldStoreBoth`
  - `RetryPolicy_ShouldRetry_OnStatusCode`
  - `DivideTest`, `MultiplyTest`, `TimespanToFixTime`

### Arrange-Act-Assert (AAA) Pattern
All tests follow the **Arrange-Act-Assert** structure for clear and consistent organization:

1. **Arrange**: Setup test data, mocks, and preconditions
   - Create test fixtures using builders (e.g., `ElectricityDataBuilder`)
   - Initialize mock objects (e.g., `Mock<IFileRepository>`)
   - Establish the test context

2. **Act**: Execute the code being tested
   - Call the method under test
   - Capture the result

3. **Assert**: Verify the results
   - Use `Assert` statements to validate expected behavior
   - Verify mock method calls with `Verify()`
   - Check state changes and return values

### Additional Best Practices
- **Mocking**: Uses Moq for external dependencies (file system, logging, time)
- **Isolation**: Tests don't depend on actual file system or system time
- **Parameterized Tests**: Uses `[DataRow]` attribute for testing multiple scenarios
- **Builder Pattern**: Uses custom builders for complex test data setup
