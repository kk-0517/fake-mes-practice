using FakeMes.Api.Dtos;
using FakeMes.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FakeMes.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Tags("看板")]
public class DashboardController(StationService stationService) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("今日进站数 + 最近记录")]
    public Task<DashboardDto> Get([FromQuery] int recent = 10, CancellationToken ct = default)
        => stationService.GetDashboardAsync(recent, ct);
}
