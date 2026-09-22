namespace FakeMes.Simulator;

/// <summary>
/// 内存假 PLC：信号名对齐现场 Process_To_MES / Process_From_MES。
/// 没有真 OPC，用属性代替点位读写。
/// </summary>
public sealed class FakePlc
{
    private readonly object _gate = new();

    public string StationCode { get; }
    public string CpuIp { get; }

    // PLC → MES
    public bool OnlineRequest { get; private set; }
    public bool DownlineRequest { get; private set; }
    public string Barcode { get; private set; } = string.Empty;

    // MES → PLC
    public bool AllowOnline { get; private set; }
    public bool NotAllowOnline { get; private set; }
    public bool AllowDownline { get; private set; }
    public bool NotAllowDownline { get; private set; }
    public string MessageFromMes { get; private set; } = string.Empty;

    public FakePlc(string stationCode, string cpuIp)
    {
        StationCode = stationCode;
        CpuIp = cpuIp;
    }

    public void RaiseOnlineRequest(string barcode)
    {
        lock (_gate)
        {
            ClearOnlineHandshakeUnlocked();
            Barcode = barcode;
            OnlineRequest = true;
        }
    }

    public void RaiseDownlineRequest(string? barcode = null)
    {
        lock (_gate)
        {
            ClearDownlineHandshakeUnlocked();
            if (barcode is not null)
                Barcode = barcode;
            DownlineRequest = true;
        }
    }

    public void WriteOnlineResult(bool allow, string message)
    {
        lock (_gate)
        {
            AllowOnline = allow;
            NotAllowOnline = !allow;
            MessageFromMes = message;
            OnlineRequest = false;
        }
    }

    public void WriteDownlineResult(bool allow, string message)
    {
        lock (_gate)
        {
            AllowDownline = allow;
            NotAllowDownline = !allow;
            MessageFromMes = message;
            DownlineRequest = false;
        }
    }

    public void ClearOnlineHandshake()
    {
        lock (_gate) ClearOnlineHandshakeUnlocked();
    }

    public void ClearDownlineHandshake()
    {
        lock (_gate) ClearDownlineHandshakeUnlocked();
    }

    public PlcSnapshot Snapshot()
    {
        lock (_gate)
        {
            return new PlcSnapshot(
                OnlineRequest, DownlineRequest, Barcode,
                AllowOnline, NotAllowOnline, AllowDownline, NotAllowDownline,
                MessageFromMes);
        }
    }

    private void ClearOnlineHandshakeUnlocked()
    {
        OnlineRequest = false;
        AllowOnline = false;
        NotAllowOnline = false;
        MessageFromMes = string.Empty;
    }

    private void ClearDownlineHandshakeUnlocked()
    {
        DownlineRequest = false;
        AllowDownline = false;
        NotAllowDownline = false;
        MessageFromMes = string.Empty;
    }
}

public record PlcSnapshot(
    bool OnlineRequest,
    bool DownlineRequest,
    string Barcode,
    bool AllowOnline,
    bool NotAllowOnline,
    bool AllowDownline,
    bool NotAllowDownline,
    string MessageFromMes);
