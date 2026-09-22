using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .Build();

var apiBaseUrl = config["ApiBaseUrl"] ?? "http://localhost:5251";
var stationCode = config["StationCode"] ?? "OP10";
var processSeconds = int.TryParse(config["ProcessSeconds"], out var p) ? p : 2;
var idleSeconds = int.TryParse(config["IdleSeconds"], out var i) ? i : 3;

using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/") };
var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

Console.WriteLine("FakeMes Simulator（自动模式）");
Console.WriteLine($"API: {apiBaseUrl}");
Console.WriteLine($"Station: {stationCode}");
Console.WriteLine("Ctrl+C to stop");
Console.WriteLine();

while (true)
{
    var barcode = $"SN{DateTime.Now:HHmmss}";
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] barcode={barcode}");

    try
    {
        var trackIn = await PostAsync("api/daq/track-in", stationCode, barcode);
        Console.WriteLine($"  track-in  => allow={trackIn.Allow}, message={trackIn.Message}");

        if (trackIn.Allow)
        {
            await Task.Delay(TimeSpan.FromSeconds(processSeconds));
            var trackOut = await PostAsync("api/daq/track-out", stationCode, barcode);
            Console.WriteLine($"  track-out => allow={trackOut.Allow}, message={trackOut.Message}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  请求失败: {ex.Message}");
        Console.WriteLine("  请确认 FakeMes.Api 已在运行（http://localhost:5251）");
    }

    Console.WriteLine();
    await Task.Delay(TimeSpan.FromSeconds(idleSeconds));
}

async Task<TrackResponse> PostAsync(string path, string station, string barcode)
{
    var response = await http.PostAsJsonAsync(path, new { stationCode = station, barcode });
    response.EnsureSuccessStatusCode();
    var body = await response.Content.ReadFromJsonAsync<TrackResponse>(jsonOptions);
    return body ?? new TrackResponse(false, "empty response");
}

record TrackResponse(bool Allow, string Message);
