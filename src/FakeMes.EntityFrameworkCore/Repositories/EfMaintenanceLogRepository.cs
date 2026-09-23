using FakeMes.Domain.Maintenance;
using FakeMes.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FakeMes.EntityFrameworkCore.Repositories;

public class EfMaintenanceLogRepository(FakeMesDbContext db) : IMaintenanceLogRepository
{
    public Task<MaintenanceLog?> GetLatestAsync(CancellationToken ct = default)
        => db.MaintenanceLogs
            .AsNoTracking()
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync(ct);

    public Task AddAsync(MaintenanceLog log, CancellationToken ct = default)
    {
        db.MaintenanceLogs.Add(log);
        return Task.CompletedTask;
    }
}
