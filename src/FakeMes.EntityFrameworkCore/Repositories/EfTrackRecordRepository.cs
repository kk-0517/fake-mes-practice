using FakeMes.Domain.Repositories;
using FakeMes.Domain.Shared;
using FakeMes.Domain.Tracking;
using Microsoft.EntityFrameworkCore;

namespace FakeMes.EntityFrameworkCore.Repositories;

public class EfTrackRecordRepository(FakeMesDbContext db) : ITrackRecordRepository
{
    public Task AddAsync(TrackRecord record, CancellationToken ct = default)
    {
        db.TrackRecords.Add(record);
        return Task.CompletedTask;
    }

    public Task<TrackRecord?> GetLatestAsync(string stationCode, string barcode, CancellationToken ct = default)
        => db.TrackRecords
            .AsNoTracking()
            .Where(x => x.StationCode == stationCode && x.Barcode == barcode)
            .OrderByDescending(x => x.Time)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<TrackRecord>> GetByBarcodeHotAsync(string barcode, CancellationToken ct = default)
        => await db.TrackRecords
            .AsNoTracking()
            .Where(x => x.Barcode == barcode)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<TrackRecordArchive>> GetByBarcodeArchiveAsync(string barcode, CancellationToken ct = default)
        => await db.TrackRecordArchives
            .AsNoTracking()
            .Where(x => x.Barcode == barcode)
            .ToListAsync(ct);

    public Task<int> CountTrackInBetweenAsync(DateTime fromInclusive, DateTime toExclusive, CancellationToken ct = default)
        => db.TrackRecords.AsNoTracking()
            .CountAsync(x => x.Type == TrackType.In && x.Time >= fromInclusive && x.Time < toExclusive, ct);

    public Task<int> CountArchiveTrackInBetweenAsync(DateTime fromInclusive, DateTime toExclusive, CancellationToken ct = default)
        => db.TrackRecordArchives.AsNoTracking()
            .CountAsync(x => x.Type == TrackType.In && x.Time >= fromInclusive && x.Time < toExclusive, ct);

    public async Task<IReadOnlyList<TrackRecord>> GetRecentHotAsync(int count, CancellationToken ct = default)
        => await db.TrackRecords
            .AsNoTracking()
            .OrderByDescending(x => x.Time)
            .ThenByDescending(x => x.Id)
            .Take(count)
            .ToListAsync(ct);

    public Task<int> CountHotAsync(CancellationToken ct = default)
        => db.TrackRecords.CountAsync(ct);

    public Task<int> CountArchiveAsync(CancellationToken ct = default)
        => db.TrackRecordArchives.CountAsync(ct);

    public async Task<IReadOnlyList<int>> GetOpenInRecordIdsAsync(CancellationToken ct = default)
    {
        var latest = await db.TrackRecords
            .AsNoTracking()
            .GroupBy(x => new { x.StationCode, x.Barcode })
            .Select(g => new
            {
                Id = g.OrderByDescending(x => x.Time).ThenByDescending(x => x.Id).Select(x => x.Id).First(),
                Type = g.OrderByDescending(x => x.Time).ThenByDescending(x => x.Id).Select(x => x.Type).First()
            })
            .ToListAsync(ct);

        return latest.Where(x => x.Type == TrackType.In).Select(x => x.Id).ToList();
    }

    public async Task<IReadOnlyList<TrackRecord>> TakeExpiredExcludingAsync(
        DateTime cutoff,
        IReadOnlyCollection<int> excludeIds,
        int take,
        CancellationToken ct = default)
        => await db.TrackRecords
            .Where(x => x.Time < cutoff && !excludeIds.Contains(x.Id))
            .OrderBy(x => x.Time)
            .ThenBy(x => x.Id)
            .Take(take)
            .ToListAsync(ct);

    public Task MoveToArchiveAsync(IReadOnlyList<TrackRecord> records, DateTime archivedAt, CancellationToken ct = default)
    {
        db.TrackRecordArchives.AddRange(records.Select(x => new TrackRecordArchive
        {
            OriginalId = x.Id,
            StationCode = x.StationCode,
            Barcode = x.Barcode,
            Type = x.Type,
            Time = x.Time,
            ArchivedAt = archivedAt
        }));
        db.TrackRecords.RemoveRange(records);
        return Task.CompletedTask;
    }
}
