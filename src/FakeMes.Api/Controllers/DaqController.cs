using FakeMes.Api.Dtos;
using FakeMes.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FakeMes.Api.Controllers;

[ApiController]
[Route("api/daq")]
[Tags("数采")]
public class DaqController(StationService stationService) : ControllerBase
{
    [HttpPost("track-in")]
    [EndpointSummary("进站")]
    public Task<TrackResponse> TrackIn([FromBody] TrackRequest request, CancellationToken ct)
        => stationService.TrackInAsync(request, ct);

    [HttpPost("track-out")]
    [EndpointSummary("出站")]
    public Task<TrackResponse> TrackOut([FromBody] TrackRequest request, CancellationToken ct)
        => stationService.TrackOutAsync(request, ct);
}
