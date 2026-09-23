using FakeMes.Application.Contracts.Daq;
using FakeMes.Application.Contracts.Stations;
using Microsoft.AspNetCore.Mvc;

namespace FakeMes.HttpApi.Controllers;

[ApiController]
[Route("api/daq")]
[Tags("数采")]
public class DaqController(IStationAppService stationAppService) : ControllerBase
{
    [HttpPost("track-in")]
    [EndpointSummary("进站")]
    public Task<TrackResponseDto> TrackIn([FromBody] TrackRequestDto request, CancellationToken ct)
        => stationAppService.TrackInAsync(request, ct);

    [HttpPost("track-out")]
    [EndpointSummary("出站")]
    public Task<TrackResponseDto> TrackOut([FromBody] TrackRequestDto request, CancellationToken ct)
        => stationAppService.TrackOutAsync(request, ct);
}
