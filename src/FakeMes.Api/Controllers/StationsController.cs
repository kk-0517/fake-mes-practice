using FakeMes.Api.Dtos;
using FakeMes.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FakeMes.Api.Controllers;

[ApiController]
[Route("api/stations")]
[Tags("工位")]
public class StationsController(StationService stationService) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("工位列表")]
    public Task<IReadOnlyList<StationDto>> Get(CancellationToken ct)
        => stationService.GetStationsAsync(ct);
}
