using FakeMes.Application.Contracts.Tracing;

namespace FakeMes.Application.Contracts.Dashboard;

public record DashboardDto(int TodayTrackInCount, IReadOnlyList<TraceItemDto> Recent);
