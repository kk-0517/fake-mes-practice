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

    [HttpPost]
    [EndpointSummary("新增工位")]
    public async Task<ActionResult<CreateStationResponse>> Create(
        [FromBody] CreateStationRequest request,
        CancellationToken ct)
    {
        var result = await stationService.CreateStationAsync(request, ct);
        if (!result.Ok)
            return BadRequest(result);
        return Ok(result);
    }
}
