namespace FakeMes.Api.Options;

public class MaintenanceOptions
{
    public const string SectionName = "Maintenance";

    /// <summary>热库保留天数；更早的记录归档到 TrackRecordArchives。</summary>
    public int RetentionDays { get; set; } = 90;

    /// <summary>是否启用后台定时归档。</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>定时归档间隔（小时）。</summary>
    public int IntervalHours { get; set; } = 24;

    /// <summary>单次最多归档条数，避免长事务锁表。</summary>
    public int BatchSize { get; set; } = 1000;
}
