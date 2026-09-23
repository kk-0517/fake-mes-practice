namespace FakeMes.Domain.Maintenance;

public class MaintenanceLog
{
    public int Id { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime FinishedAt { get; set; }
    public string Trigger { get; set; } = string.Empty;
    public int ArchivedCount { get; set; }
    public int DeletedFromHotCount { get; set; }
    public string Message { get; set; } = string.Empty;
}
