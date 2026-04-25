using BessIntelligence.Api.Data;
using BessIntelligence.Api.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BessIntelligence.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SolarController : ControllerBase
{
    private readonly AppDbContext _context;

    public SolarController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<SolarSummaryDto>> GetSummary()
    {
        var installations = await _context.SolarInstallations.ToListAsync();
        var totalCapacityMwp = Math.Round(installations.Sum(i => i.CapacityKwp) / 1000.0, 2);
        var totalPanelCount = installations.Sum(i => i.PanelCount);

        var tz = TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "W. Europe Standard Time" : "Europe/Amsterdam");
        var localNow = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tz);
        var midnightToday = new DateTimeOffset(localNow.Year, localNow.Month, localNow.Day, 0, 0, 0, localNow.Offset);
        var midnightYesterday = midnightToday.AddDays(-1);

        var yesterdayProductionKwh = await _context.SolarProductions
            .Where(p => p.Timestamp >= midnightYesterday && p.Timestamp < midnightToday)
            .SumAsync(p => p.ProductionKw * 0.25);

        var yesterdayProductionMwh = Math.Round(yesterdayProductionKwh / 1000.0, 2);

        return Ok(new SolarSummaryDto(totalCapacityMwp, totalPanelCount, yesterdayProductionMwh));
    }

    [HttpGet("forecast")]
    public async Task<ActionResult<List<SolarForecastDto>>> GetForecast([FromQuery] DateOnly? date)
    {
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        // Seed data uses CET (+02:00); use a wide UTC range to capture any offset
        var dayStartUtc = targetDate.ToDateTime(TimeOnly.MinValue).AddHours(-14);
        var dayEndUtc = targetDate.ToDateTime(TimeOnly.MinValue).AddHours(38);

        var forecasts = await _context.SolarForecasts
            .Include(f => f.SolarInstallation)
            .ToListAsync();

        var filtered = forecasts
            .Where(f => f.HourStart.Date == targetDate.ToDateTime(TimeOnly.MinValue))
            .OrderBy(f => f.SolarInstallationId)
            .ThenBy(f => f.HourStart)
            .ToList();

        return Ok(filtered.Select(f => new SolarForecastDto(
            f.Id,
            f.SolarInstallation.SiteId,
            f.HourStart,
            f.ForecastProductionKw,
            f.ConfidenceLowKw,
            f.ConfidenceHighKw,
            f.GeneratedAt
        )).ToList());
    }

    [HttpGet("production")]
    public async Task<ActionResult<List<SolarProductionDto>>> GetProduction([FromQuery] DateOnly? date)
    {
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var productions = await _context.SolarProductions
            .Include(p => p.SolarInstallation)
            .ToListAsync();

        var filtered = productions
            .Where(p => p.Timestamp.Date == targetDate.ToDateTime(TimeOnly.MinValue))
            .OrderBy(p => p.SolarInstallationId)
            .ThenBy(p => p.Timestamp)
            .ToList();

        return Ok(filtered.Select(p => new SolarProductionDto(
            p.Id,
            p.SolarInstallation.SiteId,
            p.Timestamp,
            p.ProductionKw,
            p.IrradianceWm2,
            p.PanelTempC,
            p.CapacityFactorPct
        )).ToList());
    }
}
