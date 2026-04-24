using BessIntelligence.Api.Data;
using BessIntelligence.Api.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BessIntelligence.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BatteriesController : ControllerBase
{
    private readonly AppDbContext _context;

    public BatteriesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("fleet")]
    public async Task<ActionResult<FleetSummaryDto>> GetFleet()
    {
        var batteries = await _context.Batteries
            .OrderBy(b => b.Code)
            .ToListAsync();

        var latestTelemetry = await _context.BatteryTelemetries
            .GroupBy(t => t.BatteryId)
            .Select(g => g.OrderByDescending(t => t.Timestamp).First())
            .ToListAsync();

        var telemetryMap = latestTelemetry.ToDictionary(t => t.BatteryId);

        var assets = batteries.Select(b =>
        {
            var t = telemetryMap.GetValueOrDefault(b.Id);
            return new FleetAssetDto(
                b.Id,
                b.Code,
                b.SiteName,
                b.Location,
                t?.Mode ?? "unknown",
                Math.Round(t?.PowerKw ?? 0, 1),
                Math.Round(t?.SocPct ?? 0, 1),
                Math.Round(t?.SohPct ?? 0, 1),
                Math.Round(t?.TemperatureC ?? 0, 1),
                t?.NextAction ?? "—",
                t?.NextActionWindow ?? "—",
                t?.FaultCode
            );
        }).ToList();

        var nonFaultAssets = assets.Where(a => a.Mode != "fault").ToList();
        var totalCapacityMwh = Math.Round(batteries.Sum(b => b.CapacityKwh) / 1000.0, 2);
        var availableNowMwh = Math.Round(
            batteries.Sum(b =>
            {
                var a = assets.First(a => a.Id == b.Id);
                if (a.Mode == "fault") return 0.0;
                return a.SocPct / 100.0 * b.CapacityKwh;
            }) / 1000.0, 2);
        var netPowerMwh = Math.Round(assets.Sum(a => a.PowerKw) / 1000.0, 2);

        return Ok(new FleetSummaryDto(
            totalCapacityMwh,
            availableNowMwh,
            netPowerMwh,
            assets.Count,
            assets
        ));
    }
}
