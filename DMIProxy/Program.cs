using DMIProxy;
using DMIProxy.ApplicationService;
using DMIProxy.BusinessEntity;
using DMIProxy.DomainService;
using DMIProxy.HealthCheck;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;
using SerilogTracing;
using System.Reflection;
using ZiggyCreatures.Caching.Fusion;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // Set the comments path for the Swagger JSON and UI.
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

// Health checks
builder.Services.AddHealthChecks()
    .AddCheck<MetObsHealthCheck>("MetObs_data_check")
    .AddCheck<EDRHealthCheck>("EDR_data_check")
    .AddCheck<ClimateDataHealthCheck>("Climate_data_check")
    .AddCheck<WeatherForcastHealthCheck>("Weather_forecast_check")
    .AddProcessAllocatedMemoryHealthCheck(60)
    .AddPrivateMemoryHealthCheck(360000000);

builder.Services.AddExceptionHandler<DefaultExceptionHandler>();

// Domain services
builder.Services.AddScoped<IClimateDataService, ClimateDataService>();
builder.Services.AddScoped<IEdrService, EdrService>();
builder.Services.AddScoped<IMetObsService, MetObsService>();
builder.Services.AddScoped<IWebScrapeService, WebScrapeService>();
builder.Services.AddScoped<ITimeSpanCalculator, TimeSpanCalculator>();

// Application services
builder.Services.AddScoped<IClimateDataApplicationService, ClimateDataApplicationService>();
builder.Services.AddScoped<IEdrApplicationService, EdrApplicationService>();
builder.Services.AddScoped<IMetObsApplicationService, MetObsApplicationService>();
builder.Services.AddScoped<IWeatherForecastService, WeatherForecastService>();

builder.Services.AddScoped<IDateTimeProvider, DateTimeProvider>();

// Fusion cache
builder.Services.AddFusionCache()
    .WithOptions(options => {
        options.FailSafeActivationLogLevel = LogLevel.Debug;
        options.SerializationErrorsLogLevel = LogLevel.Warning;
        options.DistributedCacheSyntheticTimeoutsLogLevel = LogLevel.Debug;
        options.DistributedCacheErrorsLogLevel = LogLevel.Error;
        options.FactorySyntheticTimeoutsLogLevel = LogLevel.Debug;
        options.FactoryErrorsLogLevel = LogLevel.Error;
    })
    .WithDefaultEntryOptions(new FusionCacheEntryOptions {
        Duration = TimeSpan.FromHours(20),

        FactorySoftTimeout = TimeSpan.FromMilliseconds(300),
        FactoryHardTimeout = TimeSpan.FromMinutes(3),

        IsFailSafeEnabled = true,
        FailSafeMaxDuration = TimeSpan.FromHours(24),
        FailSafeThrottleDuration = TimeSpan.FromSeconds(30),

        EagerRefreshThreshold = 0.9f,
        JitterMaxDuration = TimeSpan.FromSeconds(2)
    });

// HttpClient configuration with Polly
builder.Services.AddHttpClient("LongTimeOutClient", client =>
{
    client.Timeout = TimeSpan.FromSeconds(120);
})
.AddPolicyHandler((sp, request) => {
    var logger = sp.GetRequiredService<ILogger<PollyConfiguration>>();
    return PollyConfiguration.GetRateLimitAndCircuitBreakerPolicy(logger: logger);
})
.AddPolicyHandler(PollyConfiguration.GetRetryPolicy());

// Serilog configuration with trace enrichment
builder.Host.UseSerilog((hostingContext, loggerConfiguration) => loggerConfiguration
                .ReadFrom.Configuration(hostingContext.Configuration)
                .Enrich.FromLogContext());

var app = builder.Build();

// SerilogTracing configuration - captures traces as structured logs
//using var listener = new ActivityListenerConfiguration()
//    .Instrument.AspNetCoreRequests()
//    .Instrument.HttpClientRequests()
//    .Instrument.WithDefaultInstrumentation(true)
//    .TraceToSharedLogger();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks("/healthcheck", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.UseExceptionHandler(opt => { });

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
