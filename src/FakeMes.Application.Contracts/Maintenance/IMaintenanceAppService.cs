namespace FakeMes.Application.Contracts.Maintenance;

public interface IMaintenanceAppService
{
    Task<MaintenanceStatusDto> GetStatusAsync(CancellationToken ct = default);
    Task<MaintenanceRunResultDto> ArchiveExpiredAsync(string trigger, CancellationToken ct = default);
}
