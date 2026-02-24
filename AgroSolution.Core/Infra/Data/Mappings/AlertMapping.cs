using AgroSolution.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroSolution.Core.Infra.Data.Mappings;

public class AlertMapping : IEntityTypeConfiguration<Alert>
{
    public void Configure(EntityTypeBuilder<Alert> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(x => x.PlotId)
            .HasColumnName("plot_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(x => x.Type)
            .HasColumnName("type")
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(x => x.Message)
            .HasColumnName("message")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.TriggeredAt)
            .HasColumnName("triggered_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.ResolvedAt)
            .HasColumnName("resolved_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .HasColumnType("boolean")
            .IsRequired();

        builder.HasIndex(x => x.PlotId)
            .HasDatabaseName("ix_alerts_plot_id");

        builder.HasIndex(x => new { x.PlotId, x.Type, x.IsActive })
            .HasDatabaseName("ix_alerts_plot_type_active");

        builder.ToTable("alerts");
    }
}
