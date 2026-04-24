namespace BessIntelligence.Api.Models;

public class EngineRunStatus
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? Error { get; set; }
}
