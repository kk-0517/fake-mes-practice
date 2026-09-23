# 链路说明

```text
【假 PLC】FakePlc（内存信号，代替 OPC 点位）
   OnlineRequest / DownlineRequest + Barcode
        ↓ 数采轮询（DaqWorker）
【假数采】发现请求 → POST API → 写回 Allow / NotAllow
        ↓ HTTP
【后端】FakeMes.HttpApi.Host（校验 + 落库）
        ↑
【前端】FakeMes.Web（看板 / 追溯）
```

对应真实 MES：

| 练手 | 现场 |
|------|------|
| FakePlc 信号位 | PLC DB 点位（OPC UA） |
| DaqWorker 轮询 | 数采 WPF 订阅/扫信号 |
| track-in / track-out | apis/daq |
| AllowOnline / NotAllowOnline | Process_From_MES 写回 |
| StationCode + CpuIp 配置 | 设备台账绑定 |

## 一次过件六拍（简化）

1. PLC：`OnlineRequest=1` + 条码  
2. 数采：发现请求  
3. 数采：调后端进站  
4. 数采：写 `AllowOnline` 或 `NotAllowOnline`  
5. PLC：加工后 `DownlineRequest=1`  
6. 数采：出站并写 `AllowDownline` / `NotAllowDownline`

## 拒绝剧本（启动时选 2/3/4/5）

| 剧本 | PLC 行为 | 期望写回 |
|------|----------|----------|
| 空条码 | `OnlineRequest` + 空条码 | `NotAllowOnline` |
| 重复进站 | 同条码第二次 `OnlineRequest` | `NotAllowOnline` |
| 未进站出站 | 直接 `DownlineRequest` | `NotAllowDownline` |
