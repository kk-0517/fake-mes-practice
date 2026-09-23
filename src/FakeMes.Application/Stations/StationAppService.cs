using FakeMes.Application.Contracts.Daq;
using FakeMes.Application.Contracts.Dashboard;
using FakeMes.Application.Contracts.Stations;
using FakeMes.Application.Contracts.Tracing;
using FakeMes.Domain.Repositories;
using FakeMes.Domain.Shared;
using FakeMes.Domain.Stations;
using FakeMes.Domain.Tracking;

namespace FakeMes.Application.Stations;

public class StationAppService(
    IStationRepository stations,
    ITrackRecordRepository tracks,
    IUnitOfWork uow) : IStationAppService
{
    public async Task<TrackResponseDto> TrackInAsync(TrackRequestDto request, CancellationToken ct = default)
    {
        var stationCode = request.StationCode?.Trim() ?? string.Empty;
        var barcode = request.Barcode?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(barcode))
            return new TrackResponseDto(false, "条码为空");

        if (string.IsNullOrWhiteSpace(stationCode))
            return new TrackResponseDto(false, "工位码为空");

        if (!await stations.ExistsByCodeAsync(stationCode, ct))
            return new TrackResponseDto(false, $"工位不存在: {stationCode}");

        if (await IsInStationAsync(stationCode, barcode, ct))
            return new TrackResponseDto(false, "已在站内");

        await tracks.AddAsync(new TrackRecord
        {
            StationCode = stationCode,
            Barcode = barcode,
            Type = TrackType.In,
            Time = DateTime.Now
        }, ct);
        await uow.SaveChangesAsync(ct);

        return new TrackResponseDto(true, "OK");
    }

    public async Task<TrackResponseDto> TrackOutAsync(TrackRequestDto request, CancellationToken ct = default)
    {
        var stationCode = request.StationCode?.Trim() ?? string.Empty;
        var barcode = request.Barcode?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(barcode))
            return new TrackResponseDto(false, "条码为空");

        if (string.IsNullOrWhiteSpace(stationCode))
            return new TrackResponseDto(false, "工位码为空");

        if (!await IsInStationAsync(stationCode, barcode, ct))
            return new TrackResponseDto(false, "未进站，不能出站");

        await tracks.AddAsync(new TrackRecord
        {
            StationCode = stationCode,
            Barcode = barcode,
            Type = TrackType.Out,
            Time = DateTime.Now
        }, ct);
        await uow.SaveChangesAsync(ct);

        return new TrackResponseDto(true, "OK");
    }

    public async Task<IReadOnlyList<TraceItemDto>> GetTraceAsync(string barcode, CancellationToken ct = default)
    {
        barcode = barcode?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(barcode))
            return [];

        var hot = await tracks.GetByBarcodeHotAsync(barcode, ct);
        var cold = await tracks.GetByBarcodeArchiveAsync(barcode, ct);

        return hot.Select(x => ToTrace(x.Type, x.StationCode, x.Barcode, x.Time))
            .Concat(cold.Select(x => ToTrace(x.Type, x.StationCode, x.Barcode, x.Time)))
            .OrderBy(x => x.Time)
            .ToList();
    }

    public async Task<IReadOnlyList<StationDto>> GetStationsAsync(CancellationToken ct = default)
    {
        var list = await stations.GetAllOrderedAsync(ct);
        return list.Select(x => new StationDto(x.Code, x.Name)).ToList();
    }

    public async Task<CreateStationResponseDto> CreateStationAsync(CreateStationRequestDto request, CancellationToken ct = default)
    {
        var code = request.Code?.Trim() ?? string.Empty;
        var name = request.Name?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(code))
            return new CreateStationResponseDto(false, "工位码不能为空", null);

        if (string.IsNullOrWhiteSpace(name))
            return new CreateStationResponseDto(false, "工位名称不能为空", null);

        if (code.Length > 32)
            return new CreateStationResponseDto(false, "工位码最长 32 字符", null);

        if (name.Length > 64)
            return new CreateStationResponseDto(false, "工位名称最长 64 字符", null);

        if (await stations.ExistsByCodeAsync(code, ct))
            return new CreateStationResponseDto(false, $"工位码已存在: {code}", null);

        await stations.AddAsync(new Station { Code = code, Name = name }, ct);
        await uow.SaveChangesAsync(ct);

        return new CreateStationResponseDto(true, "OK", new StationDto(code, name));
    }

    public async Task<DashboardDto> GetDashboardAsync(int recentCount = 10, CancellationToken ct = default)
    {
        if (recentCount < 1) recentCount = 10;
        if (recentCount > 50) recentCount = 50;

        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        var hotIn = await tracks.CountTrackInBetweenAsync(today, tomorrow, ct);
        var coldIn = await tracks.CountArchiveTrackInBetweenAsync(today, tomorrow, ct);
        var recent = await tracks.GetRecentHotAsync(recentCount, ct);

        return new DashboardDto(
            hotIn + coldIn,
            recent.Select(x => ToTrace(x.Type, x.StationCode, x.Barcode, x.Time)).ToList());
    }

    private async Task<bool> IsInStationAsync(string stationCode, string barcode, CancellationToken ct)
    {
        var last = await tracks.GetLatestAsync(stationCode, barcode, ct);
        return last is { Type: TrackType.In };
    }

    private static TraceItemDto ToTrace(TrackType type, string stationCode, string barcode, DateTime time)
        => new(type == TrackType.In ? "In" : "Out", stationCode, barcode, time);
}
