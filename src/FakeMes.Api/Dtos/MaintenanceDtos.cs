namespace FakeMes.Api.Dtos;

public record MaintenanceRunResult(
    DateTime StartedAt,
    DateTime FinishedAt,
    string Trigger,
    int ArchivedCount,
    DateTime CutoffTime,
    string Message);

public record MaintenanceStatusDto(
    bool Enabled,
    int RetentionDays,
    int IntervalHours,
    int HotRecordCount,
    int ArchiveRecordCount,
    DateTime? LastRunAt,
    string? LastRunMessage);
