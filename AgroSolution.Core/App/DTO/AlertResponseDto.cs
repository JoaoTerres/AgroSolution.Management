using AgroSolution.Core.Domain.Entities;

namespace AgroSolution.Core.App.DTO;

/// <summary>DTO de resposta para um alerta agronômico.</summary>
public sealed class AlertResponseDto
{
    public Guid   Id          { get; init; }
    public Guid   PlotId      { get; init; }
    public string Type        { get; init; } = string.Empty;
    public string Message     { get; init; } = string.Empty;
    public bool   IsActive    { get; init; }
    public DateTime TriggeredAt { get; init; }
    public DateTime? ResolvedAt { get; init; }

    public static AlertResponseDto From(Alert alert) => new()
    {
        Id          = alert.Id,
        PlotId      = alert.PlotId,
        Type        = alert.Type.ToString(),
        Message     = alert.Message,
        IsActive    = alert.IsActive,
        TriggeredAt = alert.TriggeredAt,
        ResolvedAt  = alert.ResolvedAt
    };
}
