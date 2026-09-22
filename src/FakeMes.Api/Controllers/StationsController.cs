using FakeMes.Api.Dtos;
using FakeMes.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FakeMes.Api.Controllers;

[ApiController]
[Route("api/stations")]
public class StationsController(StationService stationService) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<StationDto>> Get(CancellationToken ct)
        => stationService.GetStationsAsync(ct);
}
