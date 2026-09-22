using FakeMes.Api.Dtos;
using FakeMes.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FakeMes.Api.Controllers;

[ApiController]
[Route("api/trace")]
[Tags("追溯")]
public class TraceController(StationService stationService) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("按条码追溯")]
    public Task<IReadOnlyList<TraceItemDto>> Get([FromQuery] string barcode, CancellationToken ct)
        => stationService.GetTraceAsync(barcode, ct);
}
