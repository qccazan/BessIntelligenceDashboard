using Azure.Monitor.OpenTelemetry.AspNetCore;
using BessIntelligence.Api.Data;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// OpenTelemetry — Azure Monitor / Application Insights
if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
{
    builder.Services.AddOpenTelemetry().UseAzureMonitor(options =>
    {
        options.ConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
    });
}
else
{
    builder.Logging.AddOpenTelemetry(logging => logging.AddConsoleExporter());
}

// EF Core — InMemory for CI, SQL Server for dev (LocalDB) and production (Azure SQL)
if (builder.Environment.EnvironmentName == "CI")
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseInMemoryDatabase("BessIntelligenceTestDb"));
}
else
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
}

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// CORS for local React dev server
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Seed database with mock data
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        logger.LogInformation("Starting database seed...");
        AppDbContext.Seed(dbContext);
        logger.LogInformation("Database seed completed successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database seed failed.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();

// Serve React static files in production
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

// In production, fallback to index.html for React Router client-side routing
app.MapFallbackToFile("index.html");

app.Run();
