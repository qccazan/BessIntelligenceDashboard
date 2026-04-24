using Microsoft.EntityFrameworkCore;

namespace BessIntelligence.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }

    public static void Seed(AppDbContext context)
    {
        context.Database.EnsureCreated();

        // Add seed data here as entities are created
        // Example:
        // if (!context.Batteries.Any())
        // {
        //     context.Batteries.AddRange(...);
        //     context.SaveChanges();
        // }
    }
}
