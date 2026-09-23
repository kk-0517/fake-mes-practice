using FakeMes.Application.Contracts.Maintenance;
using FakeMes.Domain.Maintenance;
using FakeMes.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FakeMes.Application.Maintenance;

public class MaintenanceAppService(
    ITrackRecordRepository tracks,
    IMaintenanceLogRepository logs,
    IUnitOfWork uow,
    IOptions<MaintenanceOptions> options,
    ILogger<MaintenanceAppService> logger) : IMaintenanceAppService
{
    public async Task<MaintenanceStatusDto> GetStatusAsync(CancellationToken ct = default)
    {
        var opt = options.Value;
        var hotCount = await tracks.CountHotAsync(ct);
        var archiveCount = await tracks.CountArchiveAsync(ct);
        var last = await logs.GetLatestAsync(ct);

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

        var openInIds = (await tracks.GetOpenInRecordIdsAsync(ct)).ToHashSet();
        var totalArchived = 0;

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            var batch = await tracks.TakeExpiredExcludingAsync(cutoff, openInIds, batchSize, ct);
            if (batch.Count == 0)
                break;

            await tracks.MoveToArchiveAsync(batch, DateTime.Now, ct);
            await uow.SaveChangesAsync(ct);

            totalArchived += batch.Count;
            logger.LogInformation("Maintenance archived {Count} records (cutoff {Cutoff:u})", batch.Count, cutoff);
        }

        var finished = DateTime.Now;
        var message = totalArchived == 0
            ? $"无需归档（保留 {retentionDays} 天，截止 {cutoff:yyyy-MM-dd HH:mm}）"
            : $"已归档 {totalArchived} 条到 TrackRecordArchives（保留 {retentionDays} 天）";

        await logs.AddAsync(new MaintenanceLog
        {
            StartedAt = started,
            FinishedAt = finished,
            Trigger = trigger,
            ArchivedCount = totalArchived,
            DeletedFromHotCount = totalArchived,
            Message = message
        }, ct);
        await uow.SaveChangesAsync(ct);

        return new MaintenanceRunResultDto(started, finished, trigger, totalArchived, cutoff, message);
    }
}
