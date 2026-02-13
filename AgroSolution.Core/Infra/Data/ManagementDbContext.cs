using AgroSolution.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgroSolution.Core.Infra.Data;

public class ManagementDbContext : DbContext
{
    public ManagementDbContext(DbContextOptions<ManagementDbContext> options) 
        : base(options)
    {
    }

    public DbSet<Property> Properties { get; set; }
    public DbSet<Plot> Plots { get; set; }
    public DbSet<IoTData> IoTData { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(typeof(ManagementDbContext).Assembly);
        base.OnModelCreating(builder);
    }
}