# OnlineRequest / AllowOnline 对照表

> 本机若无真仓库，用关键字在公司代码里搜：`OnlineRequest`、`AllowOnline`、`DownlineRequest`、`AllowDownline`，把「现场文件路径」列补上即可。

## 一、角色对照

| 现场 | FakeMes | 说明 |
|------|---------|------|
| PLC + OPC UA 点位 | `FakePlc` | 信号名对齐，无真 OPC / 无 NodeId |
| `LMES.DC.MesCoreServer` 数采 | `DaqWorker` | 轮询发现请求 → 调 API → 写回 Allow |
| `apis/daq` + `modules/mam` | `StationAppService` + `/api/daq/*` | 业务校验 + 落库 |
| `Nio.Xpu.Lmes.Web` | `FakeMes.Web` | 追溯 / 看板 |

```text
现场:  PLC --OPC--> 数采 --HTTP--> 后端 --DB--> Web
练手:  FakePlc --内存--> DaqWorker --HTTP--> Host --SQL--> Web
```

---

## 二、信号名（完全同名）

| 方向 | 信号 | 现场（典型） | FakeMes |
|------|------|--------------|---------|
| PLC→MES | 进站请求 | OPC 读 `…OnlineRequest` | `FakePlc.OnlineRequest` |
| PLC→MES | 条码 | OPC 读条码区 | `FakePlc.Barcode` |
| MES→PLC | 允许进站 | OPC 写 `AllowOnline` | `WriteOnlineResult(true, …)` → `AllowOnline` |
| MES→PLC | 拒绝进站 | OPC 写 `NotAllowOnline` + Message | `NotAllowOnline` + `MessageFromMes` |
| PLC→MES | 出站请求 | `DownlineRequest` | `FakePlc.DownlineRequest` |
| MES→PLC | 允许/拒绝出站 | `AllowDownline` / `NotAllowDownline` | `WriteDownlineResult` |

现场多出来：`{DbPrefix}_MESProcess_Data_DB…` 完整 NodeId、CpuIp:4840、UaExpert 对点。

---

## 三、进站握手逐步对照

| 步 | 现场大概做什么 | FakeMes 代码位置 |
|----|----------------|------------------|
| 1 | PLC 置 `OnlineRequest=1`，写条码 | `FakePlc.RaiseOnlineRequest(barcode)`（`Program.cs`） |
| 2 | 数采订阅/扫到上升沿 | `DaqWorker.PollOnceAsync` 发现 `OnlineRequest` |
| 3 | 数采调后端进站 API | `POST api/daq/track-in`（`HandleOnlineAsync`） |
| 4 | 后端校验（空码/在制/工位…） | `StationAppService.TrackInAsync` |
| 5 | 数采按结果写回 PLC | `plc.WriteOnlineResult(allow, message)` |
| 6 | PLC 看到 Allow 后开工；或 NotAllow 报警 | 日志 `[PLC ] 收到 AllowOnline / NotAllowOnline` |

---

## 四、出站握手逐步对照

| 步 | 现场 | FakeMes |
|----|------|---------|
| 1 | PLC 置 `DownlineRequest=1` | `RaiseDownlineRequest` |
| 2 | 数采发现 | `PollOnceAsync` → `HandleDownlineAsync` |
| 3 | 调出站 API | `POST api/daq/track-out` |
| 4 | 后端校验（是否已进站） | `TrackOutAsync` |
| 5 | 写回 Allow/NotAllow Downline | `WriteDownlineResult` |
| 6 | 清握手 / 下一件 | `ClearDownlineHandshake` |

---

## 五、FakeMes 有、现场也有（你已练熟）

- 请求 → 数采感知 → 调后端 → Allow/NotAllow 写回  
- 业务拒：空条码、已在站内、未进站出站  
- 落库 + Web 追溯可见  

---

## 六、现场多出来的（FakeMes 故意简化）

| 类别 | 现场有 | FakeMes |
|------|--------|---------|
| 通讯 | OPC UA `opc.tcp://CpuIp:4840` | 无，内存属性 |
| 寻址 | DbPrefix + 长 NodeId | 属性名代替 |
| 主数据 | 设备/计划/工艺路线/在制复杂状态 | 仅工位码 + 进出站记录 |
| 身份 | Identity / 网关 / 多服务 | 单 Host，无登录 |
| 数采形态 | WPF 配置、多设备并行 | 控制台单工位轮询 |
| 可靠性 | 断线重连、心跳、灯、超时 | 无 |

联调断线时先问：**断在六层哪一层**（前端 / 后端 / 数采配置 / OPC / 信号 / 追溯），再决定动哪边。

---

## 七、你在真仓库怎么搜

1. 搜 `OnlineRequest` → 数采「读 PLC / 订阅」写点  
2. 搜 `AllowOnline` 或 `NotAllowOnline` → 数采「写回 PLC」写点  
3. 搜 `track-in` / `TrackIn` / `daq` → 后端进站接口  
4. 把文件路径填进本表「现场」列，和 `DaqWorker` / `FakePlc` 并排看  

一句话：

> **顺序和信号名一致；差的是 OPC、主数据复杂度和多服务。**
