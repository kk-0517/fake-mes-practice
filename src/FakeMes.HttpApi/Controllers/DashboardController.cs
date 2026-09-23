using FakeMes.Application.Contracts.Dashboard;
using FakeMes.Application.Contracts.Stations;
using Microsoft.AspNetCore.Mvc;

namespace FakeMes.HttpApi.Controllers;

[ApiController]
[Route("api/dashboard")]
[Tags("看板")]
public class DashboardController(IStationAppService stationAppService) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("今日进站数 + 最近记录")]
    public Task<DashboardDto> Get([FromQuery] int recent = 10, CancellationToken ct = default)
        => stationAppService.GetDashboardAsync(recent, ct);
}
