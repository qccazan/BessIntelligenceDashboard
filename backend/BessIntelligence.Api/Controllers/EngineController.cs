using BessIntelligence.Api.DTOs;
using BessIntelligence.Api.Jobs;
using Microsoft.AspNetCore.Mvc;

namespace BessIntelligence.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EngineController : ControllerBase
{
    private readonly DailyEngineJob _engineJob;

    public EngineController(DailyEngineJob engineJob)
    {
        _engineJob = engineJob;
    }

    [HttpPost("run")]
    public async Task<ActionResult<EngineRunStatusDto>> Run([FromQuery] DateOnly? date)
    {
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

        var status = await _engineJob.RunAsync(targetDate, forceRegenerate: true);

        return Ok(new EngineRunStatusDto(
            status.Id,
            status.Date,
            status.Status,
            status.StartedAt,
            status.CompletedAt,
            status.Error
        ));
    }
}
