# DMIProxy

A service that is built for Home Assistant to access Danish Meteorological Institute (DMI) weather data and get data returned in a Home Assistant frendlly 
format. DMIProxy aggregates weather data from multiple sources including DMI's MetObs API, EDR (Environmental Data Retrieval) API, etc., providing 
unified endpoints for weather observations, forecasts, and climate data. The data are cached using FusionCache for optimized performance and reliability.
The service includes comprehensive health checks and is documented with Swagger for easy integration and testing.

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

- GET /MetObs/Rain/{stationId} - Get rain statistics (1 hour, day, month). List of stationId - https://www.dmi.dk/friedata/dokumentation/data/meteorological-observation-data-stations
- GET /MetObs/EDR/{forecastParameter} - Get EDR forecasts for Aarhus. Supported parameters: temperature-2m, relative-humidity-2m, wind-speed, pressure-sealevel, wind-dir, fraction-of-cloud-cover, cloud-transmittance, total-precipitation, global-radiation-flux
- GET /MetObs/WeatherForecast/{stationId} - Get weather forecast text. StationId can be 2618425 (Copenhagen), 2624652 (Aarhus), 2615876 (Odense), 2624886 (Aalborg), 2622447 (Esbjerg)
- GET /MetObs/ClimateData/HeatingDegreeDays - Get heating degree days data for Denmark
- GET /MetObs/ClimateData/AverageHeatingDegreeDays/{numberOfYears} - Get average heating degree days data (1-20 years)

## Health Checks

The service includes health checks for:
- MetObs data availability
- EDR API availability
- Climate data service
- Weather forecast service
- Process memory allocation

## Running the Application

### Docker

`docker run -p 8080:8080 epe71/dmiproxy
`

Navigate to http://localhost:8080/swagger for the API documentation.

## Home Assistant Integration
To integrate DMIProxy with Home Assistant, you can use the RESTful Sensor platform. Below is an example configuration for Home Assistant:
```yaml
sensor:
    - platform: rest
      resource: http://localhost:8080/MetObs/Rain/06072
      unique_id: dmi_MetObs_Rain_06072
      method: GET
      name: DmiAarhus_rain
      verify_ssl: false
      value_template: "OK"
      json_attributes:
        - rain1h
        - rainToday
        - rainThisMonth
      headers:
        User-Agent: Home Assistant
        Accept: 'application/json'
    
    - platform: rest
      resource: http://localhost:8080/MetObs/EDR/temperature-2m
      unique_id: dmi_MetObs_EDR_temperatur-2m
      method: GET
      name: DmiAarhus_EDR_temperatur-2m
      verify_ssl: false
      value_template: "OK"
      json_attributes:
        - data
        - description
      headers:
        User-Agent: Home Assistant
        Accept: 'application/json'
    
    - platform: rest
      resource: http://localhost:8080/MetObs/WeatherForecast/2624652
      unique_id: dmi_MetObs_WeaterForecast_2624652
      method: GET
      name: DmiAarhus_textForecast
      verify_ssl: false
      value_template: "OK"
      json_attributes:
        - time
        - headline
        - message
      headers:
        User-Agent: Home Assistant
        Accept: 'application/json'
```

## Environment

The application uses Copenhagen timezone (Europe/Copenhagen).

## License

See LICENSE file for details.
