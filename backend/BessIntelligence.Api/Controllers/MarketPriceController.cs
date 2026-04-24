using BessIntelligence.Api.Data;
using BessIntelligence.Api.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BessIntelligence.Api.Controllers;

[ApiController]
[Route("api/market-prices")]
public class MarketPriceController : ControllerBase
{
    private readonly AppDbContext _context;

    public MarketPriceController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<MarketPriceDto>>> GetByDate([FromQuery] DateOnly? date)
    {
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

        var allPrices = await _context.MarketPrices.ToListAsync();

        var filtered = allPrices
            .Where(p => DateOnly.FromDateTime(p.HourStart.Date) == targetDate)
            .OrderBy(p => p.HourStart)
            .Select(p => new MarketPriceDto(
                p.Id,
                p.HourStart,
                p.PriceEurMwh,
                p.Market,
                p.Currency
            ))
            .ToList();

        return Ok(filtered);
    }
}
