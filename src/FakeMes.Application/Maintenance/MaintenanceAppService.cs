using FakeMes.Application.Contracts.Maintenance;
using FakeMes.Domain.Maintenance;
using FakeMes.Domain.Shared;
using FakeMes.Domain.Tracking;
using FakeMes.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FakeMes.Application.Maintenance;

public class MaintenanceAppService(
    FakeMesDbContext db,
    IOptions<MaintenanceOptions> options,
    ILogger<MaintenanceAppService> logger) : IMaintenanceAppService
{
    public async Task<MaintenanceStatusDto> GetStatusAsync(CancellationToken ct = default)
    {
        var opt = options.Value;
        var hotCount = await db.TrackRecords.CountAsync(ct);
        var archiveCount = await db.TrackRecordArchives.CountAsync(ct);
        var last = await db.MaintenanceLogs
            .AsNoTracking()
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync(ct);

        return new MaintenanceStatusDto(
            opt.Enabled,
            opt.RetentionDays,
            opt.IntervalHours,
            hotCount,
            archiveCount,
            last?.FinishedAt,
            last?.Message);
    }

    public async Task<MaintenanceRunResultDto> ArchiveExpiredAsync(string trigger, CancellationToken ct = default)
    {
        var opt = options.Value;
        var started = DateTime.Now;
        var retentionDays = Math.Max(0, opt.RetentionDays);
        var batchSize = Math.Clamp(opt.BatchSize, 100, 10000);
        var cutoff = DateTime.Now.AddDays(-retentionDays);

        var openInIds = (await db.TrackRecords
                .AsNoTracking()
                .GroupBy(x => new { x.StationCode, x.Barcode })
                .Select(g => new
                {
                    Id = g.OrderByDescending(x => x.Time).ThenByDescending(x => x.Id).Select(x => x.Id).First(),
                    Type = g.OrderByDescending(x => x.Time).ThenByDescending(x => x.Id).Select(x => x.Type).First()
                })
                .ToListAsync(ct))
            .Where(x => x.Type == TrackType.In)
            .Select(x => x.Id)
            .ToHashSet();

        var totalArchived = 0;

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            var batch = await db.TrackRecords
                .Where(x => x.Time < cutoff && !openInIds.Contains(x.Id))
                .OrderBy(x => x.Time)
                .ThenBy(x => x.Id)
                .Take(batchSize)
                .ToListAsync(ct);

            if (batch.Count == 0)
                break;

            var archivedAt = DateTime.Now;
            db.TrackRecordArchives.AddRange(batch.Select(x => new TrackRecordArchive
            {
                OriginalId = x.Id,
                StationCode = x.StationCode,
                Barcode = x.Barcode,
                Type = x.Type,
                Time = x.Time,
                ArchivedAt = archivedAt
            }));
            db.TrackRecords.RemoveRange(batch);
            await db.SaveChangesAsync(ct);

            totalArchived += batch.Count;
            logger.LogInformation("Maintenance archived {Count} records (cutoff {Cutoff:u})", batch.Count, cutoff);
        }

        var finished = DateTime.Now;
        var message = totalArchived == 0
            ? $"无需归档（保留 {retentionDays} 天，截止 {cutoff:yyyy-MM-dd HH:mm}）"
            : $"已归档 {totalArchived} 条到 TrackRecordArchives（保留 {retentionDays} 天）";

        db.MaintenanceLogs.Add(new MaintenanceLog
        {
            StartedAt = started,
            FinishedAt = finished,
            Trigger = trigger,
            ArchivedCount = totalArchived,
            DeletedFromHotCount = totalArchived,
            Message = message
        });
        await db.SaveChangesAsync(ct);

        return new MaintenanceRunResultDto(started, finished, trigger, totalArchived, cutoff, message);
    }
}
