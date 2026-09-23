namespace FakeMes.Application.Contracts.Daq;

public record TrackRequestDto(string StationCode, string Barcode);

public record TrackResponseDto(bool Allow, string Message);
