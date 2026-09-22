namespace FakeMes.Api.Dtos;

public record TrackRequest(string StationCode, string Barcode);

public record TrackResponse(bool Allow, string Message);

public record TraceItemDto(string Type, string StationCode, string Barcode, DateTime Time);

public record StationDto(string Code, string Name);

public record CreateStationRequest(string Code, string Name);

public record CreateStationResponse(bool Ok, string Message, StationDto? Station);

public record DashboardDto(int TodayTrackInCount, IReadOnlyList<TraceItemDto> Recent);
