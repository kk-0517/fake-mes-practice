namespace FakeMes.Application.Contracts.Stations;

public record StationDto(string Code, string Name);

public record CreateStationRequestDto(string Code, string Name);

public record CreateStationResponseDto(bool Ok, string Message, StationDto? Station);
