using System.Net.Http.Json;
using System.Text.Json;
using FakeMes.Simulator;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .Build();

var apiBaseUrl = config["ApiBaseUrl"] ?? "http://localhost:5251";
var defaultStation = config["StationCode"] ?? "OP10";
var cpuIp = config["CpuIp"] ?? "192.168.1.10";
var processSeconds = int.TryParse(config["ProcessSeconds"], out var p) ? p : 2;
var idleSeconds = int.TryParse(config["IdleSeconds"], out var i) ? i : 3;
var pollMs = int.TryParse(config["PollMilliseconds"], out var poll) ? poll : 200;
var handshakeTimeoutSeconds = int.TryParse(config["HandshakeTimeoutSeconds"], out var t) ? t : 10;

using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/") };
var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

Console.WriteLine("FakeMes Simulator（假 PLC 握手模式）");
Console.WriteLine($"API: {apiBaseUrl}");
Console.WriteLine("链路: 假PLC置位 → 数采轮询 → HTTP API → 写回 Allow/NotAllow");
Console.WriteLine();

IReadOnlyList<StationDto> stations;
try
{
    stations = await http.GetFromJsonAsync<List<StationDto>>("api/stations", jsonOptions) ?? [];
}
catch (Exception ex)
{
    Console.WriteLine($"无法拉取工位列表: {ex.Message}");
    Console.WriteLine("请先启动 FakeMes.Api，再重新运行 Simulator。");
    return;
}

if (stations.Count == 0)
{
    Console.WriteLine("工位列表为空，请先在网页新增工位。");
    return;
}

Console.WriteLine("可选工位（模拟工位机绑设备）:");
for (var idx = 0; idx < stations.Count; idx++)
{
    var mark = stations[idx].Code.Equals(defaultStation, StringComparison.OrdinalIgnoreCase)
        ? " (默认)"
        : "";
    Console.WriteLine($"  {idx + 1}. {stations[idx].Code} · {stations[idx].Name}{mark}");
}

Console.WriteLine();
Console.Write($"请选择序号或工位码 [直接回车={defaultStation}]: ");
var stationInput = Console.ReadLine()?.Trim() ?? string.Empty;

string stationCode;
if (string.IsNullOrWhiteSpace(stationInput))
    stationCode = defaultStation;
else if (int.TryParse(stationInput, out var number) && number >= 1 && number <= stations.Count)
    stationCode = stations[number - 1].Code;
else
    stationCode = stationInput;

if (!stations.Any(s => s.Code.Equals(stationCode, StringComparison.OrdinalIgnoreCase)))
{
    Console.WriteLine($"工位不存在: {stationCode}");
    return;
}

stationCode = stations.First(s => s.Code.Equals(stationCode, StringComparison.OrdinalIgnoreCase)).Code;

Console.WriteLine();
Console.WriteLine("选择剧本:");
Console.WriteLine("  1. 正常循环过件（默认）");
Console.WriteLine("  2. 空条码进站     → 期望 NotAllowOnline「条码为空」");
Console.WriteLine("  3. 重复进站       → 期望 NotAllowOnline「已在站内」");
Console.WriteLine("  4. 未进站就出站   → 期望 NotAllowDownline「未进站」");
Console.WriteLine("  5. 拒绝场景全演示（2+3+4 各跑一遍后退出）");
Console.WriteLine();
Console.Write("请选择 [直接回车=1]: ");
var scenarioInput = Console.ReadLine()?.Trim() ?? string.Empty;
var scenario = string.IsNullOrWhiteSpace(scenarioInput) ? "1" : scenarioInput;

var plc = new FakePlc(stationCode, cpuIp);
var daq = new DaqWorker(http, plc, jsonOptions);
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

_ = Task.Run(() => DaqLoopAsync(daq, cts.Token), cts.Token);

Console.WriteLine();
Console.WriteLine($"工位已绑定: {stationCode}, CpuIp={cpuIp}, 剧本={scenario}");
Console.WriteLine();

try
{
    switch (scenario)
    {
        case "2":
            await RunEmptyBarcodeAsync(plc, cts.Token);
            break;
        case "3":
            await RunDuplicateOnlineAsync(plc, cts.Token);
            break;
        case "4":
            await RunDownlineWithoutInAsync(plc, cts.Token);
            break;
        case "5":
            await RunEmptyBarcodeAsync(plc, cts.Token);
            await Task.Delay(800, cts.Token);
            await RunDuplicateOnlineAsync(plc, cts.Token);
            await Task.Delay(800, cts.Token);
            await RunDownlineWithoutInAsync(plc, cts.Token);
            Console.WriteLine("拒绝场景全演示结束。");
            break;
        default:
            Console.WriteLine("开始正常循环过件，Ctrl+C 停止");
            Console.WriteLine();
            while (!cts.IsCancellationRequested)
            {
                var barcode = $"SN{DateTime.Now:HHmmss}";
                await RunNormalPieceAsync(plc, barcode, cts.Token);
                await Task.Delay(TimeSpan.FromSeconds(idleSeconds), cts.Token);
            }
            break;
    }
}
catch (OperationCanceledException)
{
    Console.WriteLine();
    Console.WriteLine("已停止。");
}

async Task DaqLoopAsync(DaqWorker worker, CancellationToken token)
{
    while (!token.IsCancellationRequested)
    {
        try
        {
            await worker.PollOnceAsync(token);
        }
        catch (OperationCanceledException)
        {
            break;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [数采] 异常: {ex.Message}");
        }

        try
        {
            await Task.Delay(pollMs, token);
        }
        catch (OperationCanceledException)
        {
            break;
        }
    }
}

async Task RunEmptyBarcodeAsync(FakePlc fakePlc, CancellationToken token)
{
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ===== 剧本2: 空条码进站 =====");
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [PLC ] 故意空条码 + OnlineRequest=1");
    fakePlc.RaiseOnlineRequest("");

    var ok = await WaitOnlineResultAsync(fakePlc, token);
    var snap = fakePlc.Snapshot();
    if (!ok)
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [PLC ] 进站握手超时");
    else if (snap.NotAllowOnline)
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [PLC ] 收到 NotAllowOnline ✓ msg={snap.MessageFromMes}");
    else
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [PLC ] 意外 AllowOnline（预期拒绝）");

    await Task.Delay(300, token);
    fakePlc.ClearOnlineHandshake();
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ===== 剧本2 结束 =====");
    Console.WriteLine();
}

async Task RunDuplicateOnlineAsync(FakePlc fakePlc, CancellationToken token)
{
    var barcode = $"SN{DateTime.Now:HHmmss}DUP";
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ===== 剧本3: 重复进站 barcode={barcode} =====");

    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [PLC ] 第一次 OnlineRequest=1（应 Allow）");
    fakePlc.RaiseOnlineRequest(barcode);
    var firstOk = await WaitOnlineResultAsync(fakePlc, token);
    var first = fakePlc.Snapshot();
    if (!firstOk || !first.AllowOnline)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [PLC ] 第一次进站未通过，无法演示重复进站 msg={first.MessageFromMes}");
        fakePlc.ClearOnlineHandshake();
        return;
    }

    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [PLC ] 第一次 AllowOnline ✓，不清在制，直接再请求进站");
    await Task.Delay(300, token);
    fakePlc.ClearOnlineHandshake();

    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [PLC ] 第二次 OnlineRequest=1（应 NotAllow「已在站内」）");
    fakePlc.RaiseOnlineRequest(barcode);
    var secondOk = await WaitOnlineResultAsync(fakePlc, token);
    var second = fakePlc.Snapshot();
    if (!secondOk)
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [PLC ] 第二次进站握手超时");
    else if (second.NotAllowOnline)
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [PLC ] 收到 NotAllowOnline ✓ msg={second.MessageFromMes}");
    else
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [PLC ] 意外 AllowOnline（预期拒绝）");

    await Task.Delay(300, token);
    fakePlc.ClearOnlineHandshake();

    // 收尾：正常出站，避免该条码一直占着工位
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [PLC ] 收尾 DownlineRequest=1（清在制）");
    fakePlc.RaiseDownlineRequest(barcode);
    await WaitDownlineResultAsync(fakePlc, token);
    await Task.Delay(300, token);
    fakePlc.ClearDownlineHandshake();

    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ===== 剧本3 结束 =====");
    Console.WriteLine();
}

async Task RunDownlineWithoutInAsync(FakePlc fakePlc, CancellationToken token)
{
    var barcode = $"SN{DateTime.Now:HHmmss}OUT";
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ===== 剧本4: 未进站就出站 barcode={barcode} =====");
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [PLC ] 不进站，直接 DownlineRequest=1");
    fakePlc.RaiseDownlineRequest(barcode);

    var ok = await WaitDownlineResultAsync(fakePlc, token);
    var snap = fakePlc.Snapshot();
    if (!ok)
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [PLC ] 出站握手超时");
    else if (snap.NotAllowDownline)
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [PLC ] 收到 NotAllowDownline ✓ msg={snap.MessageFromMes}");
    else
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [PLC ] 意外 AllowDownline（预期拒绝）");

    await Task.Delay(300, token);
    fakePlc.ClearDownlineHandshake();
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ===== 剧本4 结束 =====");
    Console.WriteLine();
}

async Task RunNormalPieceAsync(FakePlc fakePlc, string barcode, CancellationToken token)
{
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ===== 过件开始 barcode={barcode} =====");

    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [PLC ] 写条码 + OnlineRequest=1");
    fakePlc.RaiseOnlineRequest(barcode);

    var online = await WaitOnlineResultAsync(fakePlc, token);
    var afterOnline = fakePlc.Snapshot();
    if (!online)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [PLC ] 进站握手超时");
        fakePlc.ClearOnlineHandshake();
        Console.WriteLine();
        return;
    }

    if (afterOnline.NotAllowOnline)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [PLC ] 收到 NotAllowOnline, msg={afterOnline.MessageFromMes}");
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [PLC ] 清进站握手，本件结束");
        await Task.Delay(300, token);
        fakePlc.ClearOnlineHandshake();
        Console.WriteLine();
        return;
    }

    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [PLC ] 收到 AllowOnline，开始加工 {processSeconds}s");
    await Task.Delay(TimeSpan.FromSeconds(processSeconds), token);
    fakePlc.ClearOnlineHandshake();

    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [PLC ] DownlineRequest=1");
    fakePlc.RaiseDownlineRequest();

    var downline = await WaitDownlineResultAsync(fakePlc, token);
    var afterDownline = fakePlc.Snapshot();
    if (!downline)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [PLC ] 出站握手超时");
        fakePlc.ClearDownlineHandshake();
        Console.WriteLine();
        return;
    }

    if (afterDownline.AllowDownline)
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [PLC ] 收到 AllowDownline，过件完成");
    else
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [PLC ] 收到 NotAllowDownline, msg={afterDownline.MessageFromMes}");

    await Task.Delay(300, token);
    fakePlc.ClearDownlineHandshake();
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ===== 过件结束 =====");
    Console.WriteLine();
}

Task<bool> WaitOnlineResultAsync(FakePlc fakePlc, CancellationToken token)
    => WaitAsync(() =>
    {
        var s = fakePlc.Snapshot();
        return s.AllowOnline || s.NotAllowOnline;
    }, TimeSpan.FromSeconds(handshakeTimeoutSeconds), token);

Task<bool> WaitDownlineResultAsync(FakePlc fakePlc, CancellationToken token)
    => WaitAsync(() =>
    {
        var s = fakePlc.Snapshot();
        return s.AllowDownline || s.NotAllowDownline;
    }, TimeSpan.FromSeconds(handshakeTimeoutSeconds), token);

async Task<bool> WaitAsync(Func<bool> condition, TimeSpan timeout, CancellationToken token)
{
    var deadline = DateTime.UtcNow + timeout;
    while (DateTime.UtcNow < deadline)
    {
        token.ThrowIfCancellationRequested();
        if (condition()) return true;
        await Task.Delay(50, token);
    }

    return condition();
}

record StationDto(string Code, string Name);
