namespace AgroSolution.Core.Domain.Entities;

/// <summary>
/// Representa um alerta gerado pelo motor de alertas.
/// Criado quando uma regra de negócio é violada (ex: seca prolongada).
/// </summary>
public class Alert
{
    protected Alert() { }

    public Alert(Guid plotId, AlertType type, string message)
    {
        AssertValidation.NotEmpty(plotId, nameof(plotId));
        AssertValidation.NotNull(message, nameof(message));
        AssertValidation.IsValidEnum(type, nameof(type));

        Id          = Guid.NewGuid();
        PlotId      = plotId;
        Type        = type;
        Message     = message;
        TriggeredAt = DateTime.UtcNow;
        IsActive    = true;
    }

    /// <summary>ID único do alerta.</summary>
    public Guid Id { get; private set; }

    /// <summary>Talhão que originou o alerta.</summary>
    public Guid PlotId { get; private set; }

    /// <summary>Tipo do alerta (ex: droga, geada, excesso de chuva).</summary>
    public AlertType Type { get; private set; }

    /// <summary>Descrição legível do motivo do alerta.</summary>
    public string Message { get; private set; } = string.Empty;

    /// <summary>Quando o alerta foi criado.</summary>
    public DateTime TriggeredAt { get; private set; }

    /// <summary>Quando o alerta foi resolvido. Null = ainda ativo.</summary>
    public DateTime? ResolvedAt { get; private set; }

    /// <summary>True enquanto o alerta não foi resolvido.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Resolve o alerta (encerra a vigência).</summary>
    public void Resolve()
    {
        IsActive   = false;
        ResolvedAt = DateTime.UtcNow;
    }
}

/// <summary>Tipos de alerta suportados pelo motor de alertas.</summary>
public enum AlertType
{
    /// <summary>Umidade abaixo de 30% por mais de 24 horas consecutivas.</summary>
    Drought = 1,

    /// <summary>Temperatura acima de 40°C por mais de 4 horas consecutivas.</summary>
    ExtremeHeat = 2,

    /// <summary>Precipitação acumulada acima de 100 mm em 24 horas.</summary>
    HeavyRain = 3
}
