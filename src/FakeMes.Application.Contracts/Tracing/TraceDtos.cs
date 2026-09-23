namespace FakeMes.Application.Contracts.Tracing;

public record TraceItemDto(string Type, string StationCode, string Barcode, DateTime Time);
