using FakeMes.Application.Contracts.Maintenance;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FakeMes.Application.Maintenance;

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
        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var maintenance = scope.ServiceProvider.GetRequiredService<IMaintenanceAppService>();
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
