using BessIntelligence.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BessIntelligence.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<EngineConfig> EngineConfigs => Set<EngineConfig>();
    public DbSet<Battery> Batteries => Set<Battery>();
    public DbSet<MarketPrice> MarketPrices => Set<MarketPrice>();
    public DbSet<WeatherForecast> WeatherForecasts => Set<WeatherForecast>();
    public DbSet<BatteryTelemetry> BatteryTelemetries => Set<BatteryTelemetry>();
    public DbSet<BatteryHistory> BatteryHistories => Set<BatteryHistory>();
    public DbSet<AiRecommendation> AiRecommendations => Set<AiRecommendation>();
    public DbSet<BatteryAction> BatteryActions => Set<BatteryAction>();
    public DbSet<SolarInstallation> SolarInstallations => Set<SolarInstallation>();
    public DbSet<SolarProduction> SolarProductions => Set<SolarProduction>();
    public DbSet<SolarForecast> SolarForecasts => Set<SolarForecast>();
    public DbSet<EngineRunStatus> EngineRunStatuses => Set<EngineRunStatus>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Battery>(e =>
        {
            e.HasIndex(b => b.Code).IsUnique();
        });

        modelBuilder.Entity<BatteryTelemetry>(e =>
        {
            e.HasOne(t => t.Battery).WithMany().HasForeignKey(t => t.BatteryId);
            e.HasIndex(t => t.BatteryId);
        });

        modelBuilder.Entity<BatteryHistory>(e =>
        {
            e.HasOne(h => h.Battery).WithMany().HasForeignKey(h => h.BatteryId);
            e.HasIndex(h => new { h.BatteryId, h.Timestamp });
        });

        modelBuilder.Entity<MarketPrice>(e =>
        {
            e.HasIndex(p => p.HourStart);
        });

        modelBuilder.Entity<WeatherForecast>(e =>
        {
            e.HasIndex(w => new { w.SiteId, w.HourStart });
        });

        modelBuilder.Entity<AiRecommendation>(e =>
        {
            e.HasMany(r => r.BatteryActions).WithOne(a => a.Recommendation).HasForeignKey(a => a.RecommendationId);
        });

        modelBuilder.Entity<BatteryAction>(e =>
        {
            e.HasOne(a => a.Battery).WithMany().HasForeignKey(a => a.BatteryId);
        });

        modelBuilder.Entity<SolarInstallation>(e =>
        {
            e.HasIndex(s => s.SiteId).IsUnique();
        });

        modelBuilder.Entity<SolarProduction>(e =>
        {
            e.HasOne(p => p.SolarInstallation).WithMany().HasForeignKey(p => p.SolarInstallationId);
            e.HasIndex(p => new { p.SolarInstallationId, p.Timestamp });
        });

        modelBuilder.Entity<SolarForecast>(e =>
        {
            e.HasOne(f => f.SolarInstallation).WithMany().HasForeignKey(f => f.SolarInstallationId);
            e.HasIndex(f => new { f.SolarInstallationId, f.HourStart });
        });

        modelBuilder.Entity<EngineRunStatus>(e =>
        {
            e.HasIndex(s => s.Date).IsUnique();
        });
    }

    public static void Seed(AppDbContext context)
    {
        var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger("Seed");

        if (context.Database.IsRelational())
        {
            logger.LogInformation("Applying EF Core migrations...");
            context.Database.Migrate();
            logger.LogInformation("Migrations applied.");
        }
        else
        {
            context.Database.EnsureCreated();
        }

        logger.LogInformation("Generating seed data...");
        SeedData.Generate(context);
        logger.LogInformation("Seed data generation complete.");
    }
}
