using FakeMes.Application.Contracts.Stations;
using FakeMes.Application.Contracts.Tracing;
using Microsoft.AspNetCore.Mvc;

namespace FakeMes.HttpApi.Controllers;

[ApiController]
[Route("api/trace")]
[Tags("追溯")]
public class TraceController(IStationAppService stationAppService) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("按条码追溯")]
    public Task<IReadOnlyList<TraceItemDto>> Get([FromQuery] string barcode, CancellationToken ct)
        => stationAppService.GetTraceAsync(barcode, ct);
}
