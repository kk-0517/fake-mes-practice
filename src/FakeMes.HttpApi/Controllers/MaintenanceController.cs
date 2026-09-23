using FakeMes.Application.Contracts.Maintenance;
using Microsoft.AspNetCore.Mvc;

namespace FakeMes.HttpApi.Controllers;

[ApiController]
[Route("api/maintenance")]
[Tags("维护")]
public class MaintenanceController(IMaintenanceAppService maintenanceAppService) : ControllerBase
{
    [HttpGet("status")]
    [EndpointSummary("维护状态")]
    public Task<MaintenanceStatusDto> Status(CancellationToken ct)
        => maintenanceAppService.GetStatusAsync(ct);

    [HttpPost("archive")]
    [EndpointSummary("立即归档过期记录")]
    public Task<MaintenanceRunResultDto> Archive(CancellationToken ct)
        => maintenanceAppService.ArchiveExpiredAsync("manual", ct);
}
