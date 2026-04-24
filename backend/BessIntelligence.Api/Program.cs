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

// EF Core — SQL Server for both dev (LocalDB) and production (Azure SQL)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register repositories here as they are created
// builder.Services.AddScoped<IBatteryRepository, BatteryRepository>();

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

// Seed database in development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    AppDbContext.Seed(dbContext);

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
