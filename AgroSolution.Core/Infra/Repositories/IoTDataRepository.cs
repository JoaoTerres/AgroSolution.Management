using AgroSolution.Core.Domain.Entities;
using AgroSolution.Core.Domain.Interfaces;
using AgroSolution.Core.Infra.Data;
using Microsoft.EntityFrameworkCore;

namespace AgroSolution.Core.Infra.Repositories;

/// <summary>
/// Repositório para dados IoT
/// </summary>
public class IoTDataRepository : IIoTDataRepository
{
    private readonly ManagementDbContext _context;

    public IoTDataRepository(ManagementDbContext context)
    {
        _context = context;
    }

    public async Task<bool> AddAsync(IoTData iotData)
    {
        await _context.IoTData.AddAsync(iotData);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<IoTData?> GetByIdAsync(Guid id)
    {
        return await _context.IoTData.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<IEnumerable<IoTData>> GetByPlotIdAsync(Guid plotId)
    {
        return await _context.IoTData
            .Where(x => x.PlotId == plotId)
            .OrderByDescending(x => x.ReceivedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<IoTData>> GetPendingAsync(int limit = 100)
    {
        return await _context.IoTData
            .Where(x => x.ProcessingStatus == IoTProcessingStatus.Pending)
            .OrderBy(x => x.ReceivedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<bool> UpdateAsync(IoTData iotData)
    {
        _context.IoTData.Update(iotData);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<IEnumerable<IoTData>> GetByPlotIdAndDateRangeAsync(Guid plotId, DateTime startDate, DateTime endDate)
    {
        return await _context.IoTData
            .Where(x => x.PlotId == plotId && 
                        x.ReceivedAt >= startDate && 
                        x.ReceivedAt <= endDate)
            .OrderBy(x => x.ReceivedAt)
            .ToListAsync();
    }
}
