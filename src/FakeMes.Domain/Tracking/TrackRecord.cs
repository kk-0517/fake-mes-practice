using FakeMes.Domain.Shared;

namespace FakeMes.Domain.Tracking;

public class TrackRecord
{
    public int Id { get; set; }
    public string StationCode { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public TrackType Type { get; set; }
    public DateTime Time { get; set; }
}
