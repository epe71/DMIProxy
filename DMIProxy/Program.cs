using DMIProxy;
using DMIProxy.ApplicationService;
using DMIProxy.BusinessEntity;
using DMIProxy.DomainService;
using DMIProxy.HealthCheck;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;
using System.Reflection;

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

builder.Services.AddHealthChecks()
    .AddCheck<RequestCacheHealthCheck>("Request_cache_check")
    .AddCheck<MetObsHealthCheck>("MetObs_data_check")
    .AddCheck<EDRHealthCheck>("EDR_data_check")
    .AddProcessAllocatedMemoryHealthCheck(60)
    .AddPrivateMemoryHealthCheck(350000000);

builder.Services.AddExceptionHandler<DefaultExceptionHandler>();

builder.Services.AddScoped<IMetObsApplicationService, MetObsApplicationService>();
builder.Services.AddScoped<IMetObsService, MetObsService>();
builder.Services.AddScoped<IEdrApplicationService, EdrApplicationService>();
builder.Services.AddScoped<IEdrService, EdrService>();
builder.Services.AddScoped<IWeatherForcastService, WeatherForcastService>();
builder.Services.AddScoped<IWebScrapeService, WebScrapeService>();
builder.Services.AddScoped<IRequestCache, RequestCache>();
builder.Services.AddScoped<IDateTimeProvider, DateTimeProvider>();
builder.Services.AddScoped<ITimeSpanCalculator, TimeSpanCalculator>();

builder.Services.AddMemoryCache(option => { option.TrackStatistics = true; });
builder.Services.AddHttpClient("LongTimeOutClient", client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
})
.AddPolicyHandler(PollyConfiguration.GetRetryPolicy())
.AddPolicyHandler(PollyConfiguration.GetRateLimitAndCircuitBreakerPolicy());

builder.Host.UseSerilog((hostingContext, loggerConfiguration) => loggerConfiguration
                .ReadFrom.Configuration(hostingContext.Configuration));

var app = builder.Build();

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
