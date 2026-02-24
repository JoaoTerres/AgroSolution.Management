using AgroSolution.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgroSolution.Identity.Infra.Data;

public class IdentityDbContext : DbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options) { }

    public DbSet<Producer> Producers { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
        base.OnModelCreating(builder);
    }
}
