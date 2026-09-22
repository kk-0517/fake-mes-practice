using FakeMes.Api.Dtos;
using FakeMes.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FakeMes.Api.Controllers;

[ApiController]
[Route("api/maintenance")]
[Tags("维护")]
public class MaintenanceController(MaintenanceService maintenanceService) : ControllerBase
{
    [HttpGet("status")]
    [EndpointSummary("维护状态")]
    public Task<MaintenanceStatusDto> Status(CancellationToken ct)
        => maintenanceService.GetStatusAsync(ct);

    [HttpPost("archive")]
    [EndpointSummary("立即归档过期记录")]
    public Task<MaintenanceRunResult> Archive(CancellationToken ct)
        => maintenanceService.ArchiveExpiredAsync("manual", ct);
}
