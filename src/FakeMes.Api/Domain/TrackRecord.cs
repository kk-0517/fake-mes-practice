namespace FakeMes.Api.Domain;

public enum TrackType
{
    In = 1,
    Out = 2
}

public class TrackRecord
{
    public int Id { get; set; }
    public string StationCode { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public TrackType Type { get; set; }
    public DateTime Time { get; set; }
}
