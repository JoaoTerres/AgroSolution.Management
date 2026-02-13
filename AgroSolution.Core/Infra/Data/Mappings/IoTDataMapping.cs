using AgroSolution.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroSolution.Core.Infra.Data.Mappings;

/// <summary>
/// Configuração de mapeamento da entidade IoTData para Entity Framework Core
/// </summary>
public class IoTDataMapping : IEntityTypeConfiguration<IoTData>
{
    public void Configure(EntityTypeBuilder<IoTData> builder)
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

        builder.Property(x => x.DeviceType)
            .HasColumnName("device_type")
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(x => x.RawData)
            .HasColumnName("raw_data")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.DeviceTimestamp)
            .HasColumnName("device_timestamp")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.ReceivedAt)
            .HasColumnName("received_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.ProcessingStatus)
            .HasColumnName("processing_status")
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(x => x.ProcessingQueueId)
            .HasColumnName("processing_queue_id")
            .HasColumnType("text");

        builder.Property(x => x.ProcessingStartedAt)
            .HasColumnName("processing_started_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.ProcessingCompletedAt)
            .HasColumnName("processing_completed_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.ErrorMessage)
            .HasColumnName("error_message")
            .HasColumnType("text");

        // Índices para queries frequentes
        builder.HasIndex(x => x.PlotId)
            .HasDatabaseName("ix_iot_data_plot_id");

        builder.HasIndex(x => x.ProcessingStatus)
            .HasDatabaseName("ix_iot_data_processing_status");

        builder.HasIndex(x => new { x.PlotId, x.ReceivedAt })
            .HasDatabaseName("ix_iot_data_plot_timestamp");

        builder.HasIndex(x => x.ReceivedAt)
            .HasDatabaseName("ix_iot_data_received_at");

        builder.ToTable("iot_data");
    }
}
