using FakeMes.Domain.Shared;

namespace FakeMes.Domain.Tracking;

/// <summary>
/// 过期进站/出站记录归档（热库 TrackRecords 只保留近期数据）。
/// </summary>
public class TrackRecordArchive
{
    public long Id { get; set; }
    public int OriginalId { get; set; }
    public string StationCode { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public TrackType Type { get; set; }
    public DateTime Time { get; set; }
    public DateTime ArchivedAt { get; set; }
}
