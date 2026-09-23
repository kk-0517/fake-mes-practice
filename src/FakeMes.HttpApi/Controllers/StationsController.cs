using FakeMes.Application.Contracts.Stations;
using Microsoft.AspNetCore.Mvc;

namespace FakeMes.HttpApi.Controllers;

[ApiController]
[Route("api/stations")]
[Tags("工位")]
public class StationsController(IStationAppService stationAppService) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("工位列表")]
    public Task<IReadOnlyList<StationDto>> Get(CancellationToken ct)
        => stationAppService.GetStationsAsync(ct);

    [HttpPost]
    [EndpointSummary("新增工位")]
    public async Task<ActionResult<CreateStationResponseDto>> Create(
        [FromBody] CreateStationRequestDto request,
        CancellationToken ct)
    {
        var result = await stationAppService.CreateStationAsync(request, ct);
        if (!result.Ok)
            return BadRequest(result);
        return Ok(result);
    }
}
