using AgroSolution.Core.Domain.Entities;
using AgroSolution.Core.Domain.Interfaces;
using AgroSolution.Core.Infra.Data;
using Microsoft.EntityFrameworkCore;

namespace AgroSolution.Core.Infra.Repositories;

public class AlertRepository : IAlertRepository
{
    private readonly ManagementDbContext _ctx;

    public AlertRepository(ManagementDbContext ctx) => _ctx = ctx;

    public async Task<bool> AddAsync(Alert alert)
    {
        await _ctx.Alerts.AddAsync(alert);
        return await _ctx.SaveChangesAsync() > 0;
    }

    public async Task<IEnumerable<Alert>> GetByPlotIdAsync(Guid plotId)
        => await _ctx.Alerts
            .Where(a => a.PlotId == plotId)
            .OrderByDescending(a => a.TriggeredAt)
            .ToListAsync();

    public async Task<Alert?> GetActiveByPlotIdAndTypeAsync(Guid plotId, AlertType type)
        => await _ctx.Alerts
            .Where(a => a.PlotId == plotId && a.Type == type && a.IsActive)
            .OrderByDescending(a => a.TriggeredAt)
            .FirstOrDefaultAsync();

    public async Task<bool> UpdateAsync(Alert alert)
    {
        _ctx.Alerts.Update(alert);
        return await _ctx.SaveChangesAsync() > 0;
    }
}
