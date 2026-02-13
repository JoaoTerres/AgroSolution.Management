using AgroSolution.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroSolution.Core.Infra.Data.Mappings;

public class PropertyMapping : IEntityTypeConfiguration<Property>
{
    public void Configure(EntityTypeBuilder<Property> builder)
    {
        builder.ToTable("Properties");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(150)
            .HasColumnType("varchar(150)");

        builder.Property(p => p.Location)
            .IsRequired()
            .HasMaxLength(250)
            .HasColumnType("varchar(250)");
        
        builder.Property(p => p.ProducerId)
            .IsRequired();

        builder.HasMany(p => p.Plots)
            .WithOne()
            .HasForeignKey(p => p.PropertyId)
            .OnDelete(DeleteBehavior.Cascade); 
    }
}