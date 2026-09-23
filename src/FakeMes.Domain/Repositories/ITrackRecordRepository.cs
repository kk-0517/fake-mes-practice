using FakeMes.Domain.Tracking;

namespace FakeMes.Domain.Repositories;

public interface ITrackRecordRepository
{
    Task AddAsync(TrackRecord record, CancellationToken ct = default);
    Task<TrackRecord?> GetLatestAsync(string stationCode, string barcode, CancellationToken ct = default);
    Task<IReadOnlyList<TrackRecord>> GetByBarcodeHotAsync(string barcode, CancellationToken ct = default);
    Task<IReadOnlyList<TrackRecordArchive>> GetByBarcodeArchiveAsync(string barcode, CancellationToken ct = default);
    Task<int> CountTrackInBetweenAsync(DateTime fromInclusive, DateTime toExclusive, CancellationToken ct = default);
    Task<int> CountArchiveTrackInBetweenAsync(DateTime fromInclusive, DateTime toExclusive, CancellationToken ct = default);
    Task<IReadOnlyList<TrackRecord>> GetRecentHotAsync(int count, CancellationToken ct = default);
    Task<int> CountHotAsync(CancellationToken ct = default);
    Task<int> CountArchiveAsync(CancellationToken ct = default);
    Task<IReadOnlyList<int>> GetOpenInRecordIdsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<TrackRecord>> TakeExpiredExcludingAsync(
        DateTime cutoff,
        IReadOnlyCollection<int> excludeIds,
        int take,
        CancellationToken ct = default);
    Task MoveToArchiveAsync(IReadOnlyList<TrackRecord> records, DateTime archivedAt, CancellationToken ct = default);
}
