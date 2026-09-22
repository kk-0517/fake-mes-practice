using FakeMes.Api.Dtos;
using FakeMes.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FakeMes.Api.Controllers;

[ApiController]
[Route("api/trace")]
public class TraceController(StationService stationService) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<TraceItemDto>> Get([FromQuery] string barcode, CancellationToken ct)
        => stationService.GetTraceAsync(barcode, ct);
}
