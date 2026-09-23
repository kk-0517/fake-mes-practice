using FakeMes.Application.Contracts.Daq;
using FakeMes.Application.Contracts.Dashboard;
using FakeMes.Application.Contracts.Stations;
using FakeMes.Application.Contracts.Tracing;

namespace FakeMes.Application.Contracts.Stations;

public interface IStationAppService
{
    Task<TrackResponseDto> TrackInAsync(TrackRequestDto request, CancellationToken ct = default);
    Task<TrackResponseDto> TrackOutAsync(TrackRequestDto request, CancellationToken ct = default);
    Task<IReadOnlyList<TraceItemDto>> GetTraceAsync(string barcode, CancellationToken ct = default);
    Task<IReadOnlyList<StationDto>> GetStationsAsync(CancellationToken ct = default);
    Task<CreateStationResponseDto> CreateStationAsync(CreateStationRequestDto request, CancellationToken ct = default);
    Task<DashboardDto> GetDashboardAsync(int recentCount = 10, CancellationToken ct = default);
}
