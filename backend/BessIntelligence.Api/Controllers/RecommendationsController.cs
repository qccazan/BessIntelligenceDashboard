using BessIntelligence.Api.Data;
using BessIntelligence.Api.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BessIntelligence.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RecommendationsController : ControllerBase
{
    private readonly AppDbContext _context;

    public RecommendationsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("latest")]
    public async Task<ActionResult<RecommendationDto>> GetLatest()
    {
        var rec = await _context.AiRecommendations
            .Include(r => r.BatteryActions)
            .OrderByDescending(r => r.GeneratedAt)
            .FirstOrDefaultAsync();

        if (rec == null) return NotFound();

        return Ok(MapToDto(rec));
    }

    [HttpGet]
    public async Task<ActionResult<RecommendationDto>> GetByDate([FromQuery] DateOnly? date)
    {
        if (date == null)
        {
            var recs = await _context.AiRecommendations
                .Include(r => r.BatteryActions)
                .OrderByDescending(r => r.GeneratedAt)
                .Take(30)
                .ToListAsync();

            return Ok(recs.Select(MapToDto));
        }

        var targetDay = date.Value.ToDateTime(TimeOnly.MinValue);
        var allRecs = await _context.AiRecommendations
            .Include(r => r.BatteryActions)
            .ToListAsync();

        var rec = allRecs.FirstOrDefault(r => r.GeneratedAt.Date == targetDay);

        if (rec == null) return NotFound();

        return Ok(MapToDto(rec));
    }

    private RecommendationDto MapToDto(Models.AiRecommendation rec)
    {
        var batteryLookup = _context.Batteries
            .ToDictionary(b => b.Id, b => b.Code);

        return new RecommendationDto(
            rec.Id,
            rec.GeneratedAt,
            rec.PortfolioAction,
            rec.ChargeWindowStart,
            rec.ChargeWindowEnd,
            rec.DischargeWindowStart,
            rec.DischargeWindowEnd,
            rec.ChargePrice,
            rec.DischargePrice,
            rec.PriceSpreadMultiplier,
            rec.Avg30dSpreadMultiplier,
            rec.ConfidencePct,
            rec.Explanation,
            rec.EstimatedCaptureEur,
            rec.BatteryActions.Select(a => new BatteryActionDto(
                a.Id,
                a.BatteryId,
                batteryLookup.GetValueOrDefault(a.BatteryId, $"BESS-{a.BatteryId:D2}"),
                a.Action,
                a.WindowStart,
                a.WindowEnd,
                a.Reason
            )).ToList()
        );
    }
}
