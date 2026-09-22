using System.Net.Http.Json;
using System.Text.Json;

namespace FakeMes.Simulator;

/// <summary>
/// 假数采：轮询假 PLC 信号，调后端 API，再写回 Allow / NotAllow。
/// 对应现场：OPC 订阅 → HTTP/MQ → 写回 PLC。
/// </summary>
public sealed class DaqWorker(HttpClient http, FakePlc plc, JsonSerializerOptions jsonOptions)
{
    private bool _handlingOnline;
    private bool _handlingDownline;

    public async Task PollOnceAsync(CancellationToken ct = default)
    {
        var snap = plc.Snapshot();

        if (snap.OnlineRequest && !_handlingOnline)
        {
            _handlingOnline = true;
            try
            {
                await HandleOnlineAsync(snap.Barcode, ct);
            }
            finally
            {
                _handlingOnline = false;
            }
        }

        if (snap.DownlineRequest && !_handlingDownline)
        {
            _handlingDownline = true;
            try
            {
                await HandleDownlineAsync(snap.Barcode, ct);
            }
            finally
            {
                _handlingDownline = false;
            }
        }
    }

    private async Task HandleOnlineAsync(string barcode, CancellationToken ct)
    {
        Log($"[数采] 发现 OnlineRequest=1, barcode={barcode}");
        Log($"[数采] → POST api/daq/track-in ({plc.StationCode})");

        var result = await PostAsync("api/daq/track-in", plc.StationCode, barcode, ct);
        plc.WriteOnlineResult(result.Allow, result.Message);

        if (result.Allow)
            Log($"[数采] ← 写回 AllowOnline=1, message={result.Message}");
        else
            Log($"[数采] ← 写回 NotAllowOnline=1, message={result.Message}");
    }

    private async Task HandleDownlineAsync(string barcode, CancellationToken ct)
    {
        Log($"[数采] 发现 DownlineRequest=1, barcode={barcode}");
        Log($"[数采] → POST api/daq/track-out ({plc.StationCode})");

        var result = await PostAsync("api/daq/track-out", plc.StationCode, barcode, ct);
        plc.WriteDownlineResult(result.Allow, result.Message);

        if (result.Allow)
            Log($"[数采] ← 写回 AllowDownline=1, message={result.Message}");
        else
            Log($"[数采] ← 写回 NotAllowDownline=1, message={result.Message}");
    }

    private async Task<TrackResponse> PostAsync(string path, string station, string barcode, CancellationToken ct)
    {
        var response = await http.PostAsJsonAsync(path, new { stationCode = station, barcode }, ct);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<TrackResponse>(jsonOptions, ct);
        return body ?? new TrackResponse(false, "empty response");
    }

    private static void Log(string message)
        => Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
}

public record TrackResponse(bool Allow, string Message);
