using FakeMes.Domain.Maintenance;

namespace FakeMes.Domain.Repositories;

public interface IMaintenanceLogRepository
{
    Task<MaintenanceLog?> GetLatestAsync(CancellationToken ct = default);
    Task AddAsync(MaintenanceLog log, CancellationToken ct = default);
}
