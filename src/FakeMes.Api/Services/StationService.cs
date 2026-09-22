using FakeMes.Api.Domain;
using FakeMes.Api.Dtos;
using Microsoft.EntityFrameworkCore;

namespace FakeMes.Api.Services;

public class StationService(AppDbContext db)
{
    public async Task<TrackResponse> TrackInAsync(TrackRequest request, CancellationToken ct = default)
    {
        var stationCode = request.StationCode?.Trim() ?? string.Empty;
        var barcode = request.Barcode?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(barcode))
            return new TrackResponse(false, "条码为空");

        if (string.IsNullOrWhiteSpace(stationCode))
            return new TrackResponse(false, "工位码为空");

        var stationExists = await db.Stations.AnyAsync(x => x.Code == stationCode, ct);
        if (!stationExists)
            return new TrackResponse(false, $"工位不存在: {stationCode}");

        if (await IsInStationAsync(stationCode, barcode, ct))
            return new TrackResponse(false, "已在站内");

        db.TrackRecords.Add(new TrackRecord
        {
            StationCode = stationCode,
            Barcode = barcode,
            Type = TrackType.In,
            Time = DateTime.Now
        });
        await db.SaveChangesAsync(ct);

        return new TrackResponse(true, "OK");
    }

    public async Task<TrackResponse> TrackOutAsync(TrackRequest request, CancellationToken ct = default)
    {
        var stationCode = request.StationCode?.Trim() ?? string.Empty;
        var barcode = request.Barcode?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(barcode))
            return new TrackResponse(false, "条码为空");

        if (string.IsNullOrWhiteSpace(stationCode))
            return new TrackResponse(false, "工位码为空");

        if (!await IsInStationAsync(stationCode, barcode, ct))
            return new TrackResponse(false, "未进站，不能出站");

        db.TrackRecords.Add(new TrackRecord
        {
            StationCode = stationCode,
            Barcode = barcode,
            Type = TrackType.Out,
            Time = DateTime.Now
        });
        await db.SaveChangesAsync(ct);

        return new TrackResponse(true, "OK");
    }

    public async Task<IReadOnlyList<TraceItemDto>> GetTraceAsync(string barcode, CancellationToken ct = default)
    {
        barcode = barcode?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(barcode))
            return [];

        var hot = await db.TrackRecords
            .AsNoTracking()
            .Where(x => x.Barcode == barcode)
            .Select(x => new TraceItemDto(
                x.Type == TrackType.In ? "In" : "Out",
                x.StationCode,
                x.Barcode,
                x.Time))
            .ToListAsync(ct);

        var cold = await db.TrackRecordArchives
            .AsNoTracking()
            .Where(x => x.Barcode == barcode)
            .Select(x => new TraceItemDto(
                x.Type == TrackType.In ? "In" : "Out",
                x.StationCode,
                x.Barcode,
                x.Time))
            .ToListAsync(ct);

        return hot.Concat(cold)
            .OrderBy(x => x.Time)
            .ToList();
    }

    public async Task<IReadOnlyList<StationDto>> GetStationsAsync(CancellationToken ct = default)
    {
        return await db.Stations
            .AsNoTracking()
            .OrderBy(x => x.Code)
            .Select(x => new StationDto(x.Code, x.Name))
            .ToListAsync(ct);
    }

    public async Task<CreateStationResponse> CreateStationAsync(CreateStationRequest request, CancellationToken ct = default)
    {
        var code = request.Code?.Trim() ?? string.Empty;
        var name = request.Name?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(code))
            return new CreateStationResponse(false, "工位码不能为空", null);

        if (string.IsNullOrWhiteSpace(name))
            return new CreateStationResponse(false, "工位名称不能为空", null);

        if (code.Length > 32)
            return new CreateStationResponse(false, "工位码最长 32 字符", null);

        if (name.Length > 64)
            return new CreateStationResponse(false, "工位名称最长 64 字符", null);

        var exists = await db.Stations.AnyAsync(x => x.Code == code, ct);
        if (exists)
            return new CreateStationResponse(false, $"工位码已存在: {code}", null);

        db.Stations.Add(new Station { Code = code, Name = name });
        await db.SaveChangesAsync(ct);

        return new CreateStationResponse(true, "OK", new StationDto(code, name));
    }

    public async Task<DashboardDto> GetDashboardAsync(int recentCount = 10, CancellationToken ct = default)
    {
        if (recentCount < 1) recentCount = 10;
        if (recentCount > 50) recentCount = 50;

        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        var hotIn = await db.TrackRecords
            .AsNoTracking()
            .CountAsync(x => x.Type == TrackType.In && x.Time >= today && x.Time < tomorrow, ct);
        var coldIn = await db.TrackRecordArchives
            .AsNoTracking()
            .CountAsync(x => x.Type == TrackType.In && x.Time >= today && x.Time < tomorrow, ct);

        var recent = await db.TrackRecords
            .AsNoTracking()
            .OrderByDescending(x => x.Time)
            .ThenByDescending(x => x.Id)
            .Take(recentCount)
            .Select(x => new TraceItemDto(
                x.Type == TrackType.In ? "In" : "Out",
                x.StationCode,
                x.Barcode,
                x.Time))
            .ToListAsync(ct);

        return new DashboardDto(hotIn + coldIn, recent);
    }

    private async Task<bool> IsInStationAsync(string stationCode, string barcode, CancellationToken ct)
    {
        var last = await db.TrackRecords
            .AsNoTracking()
            .Where(x => x.StationCode == stationCode && x.Barcode == barcode)
            .OrderByDescending(x => x.Time)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(ct);

        return last is { Type: TrackType.In };
    }
}
