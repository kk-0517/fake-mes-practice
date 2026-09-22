using FakeMes.Api.Options;
using Microsoft.Extensions.Options;

namespace FakeMes.Api.Services;

/// <summary>
/// 定时把过期热数据归档到冷表，模拟正式 MES 日维护。
/// </summary>
public class MaintenanceBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<MaintenanceOptions> options,
    ILogger<MaintenanceBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opt = options.Value;
        if (!opt.Enabled)
        {
            logger.LogInformation("Maintenance background job disabled");
            return;
        }

        var interval = TimeSpan.FromHours(Math.Max(1, opt.IntervalHours));
        // 启动后稍等再跑，避免和 EnsureCreated 抢连接
        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var maintenance = scope.ServiceProvider.GetRequiredService<MaintenanceService>();
                var result = await maintenance.ArchiveExpiredAsync("scheduler", stoppingToken);
                logger.LogInformation("Scheduled maintenance: {Message}", result.Message);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Scheduled maintenance failed");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }
}
