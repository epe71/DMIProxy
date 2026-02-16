# DMIProxy

A .NET 10 ASP.NET Core API service that acts as a proxy for Danish Meteorological Institute (DMI) weather data. DMIProxy aggregates weather data from multiple sources including DMI's MetObs API, EDR (Environmental Data Retrieval) API, and web scraping, providing unified endpoints for weather observations, forecasts, and climate data.

## Features

- **Rain Data Retrieval**: Get rain measurements from the last hour, day, and month from DMI MetObs stations
- **EDR Forecasts**: Access EDR API forecasts via a simplified proxy interface
- **Weather Forecasts**: Retrieve Danish weather forecasts for specified locations
- **Climate Data**: Fetch heating degree days and other climate metrics
- **Caching**: Built-in distributed caching with FusionCache for optimized performance
- **Resilience**: Polly-based retry policies and circuit breaker patterns for reliable external API calls
- **Health Checks**: Comprehensive health check endpoints for all integrated services
- **Swagger Documentation**: Interactive API documentation via Swagger UI


## API Endpoints

### MetObs Controller

- GET /MetObs/Rain/{stationId} - Get rain statistics (1 hour, day, month)
- GET /MetObs/EDR/{forecastParameter} - Get EDR forecasts
- GET /MetObs/WeatherForecast/{stationId} - Get weather forecast text

## Health Checks

The service includes health checks for:
- MetObs data availability
- EDR API availability
- Climate data service
- Weather forecast service
- Process memory allocation

## Running the Application

### Docker

`bash
docker build -t dmiproxy .
docker run -p 8080:8080 dmiproxy
`

### Local Development

`bash
dotnet build
dotnet run --project DMIProxy/DMIProxy.csproj
`

Navigate to http://localhost:8080/swagger for the API documentation.

## Environment

The application uses Copenhagen timezone (Europe/Copenhagen) and is containerized for cloud deployment with proper health monitoring.

## License

See LICENSE file for details.
