# 公司联调手册 ↔ FakeMes 对照

对照文档：`NIO.XPU_NEW/进站出站全链路联调手册.md`  
目标：用 FakeMes 把现场「六层 / 六拍」在脑子里跑熟；真联调时知道断在哪一层。

---

## 一、全链路心智模型对照

| 现场（手册） | FakeMes |
|--------------|---------|
| `Nio.Xpu.Lmes.Web` | `FakeMes.Web`（看板 + 追溯 + 新增工位） |
| `apis/daq` + `modules/mam` | `FakeMes.Api`（`/api/daq/*` + `StationService`） |
| `LMES.DC.MesCoreServer` 数采 | `DaqWorker`（轮询假 PLC） |
| PLC + OPC UA `opc.tcp:4840` | `FakePlc`（内存信号，无真 OPC） |

```text
现场:  Web ↔ 后端 ↔ 数采 ↔ OPC ↔ PLC
练手:  Web ↔ Api  ↔ DaqWorker ↔ 内存 ↔ FakePlc
```

---

## 二、六层定位怎么在 FakeMes 里练

| 层 | 现场查什么 | FakeMes 里对应练什么 |
|----|------------|----------------------|
| ① 前端主数据 | 设备/工序/计划/路线 | Web「新增工位」、工位芯片列表 |
| ② 后端服务 | daq 通不通、mam 拒不拒、库写入 | API 日志 / Swagger / `dotnet test` |
| ③ 数采配置 | JSON、API 地址、设备码、`DbPrefix` | Simulator 启动选工位；`appsettings` 的 `ApiBaseUrl`/`StationCode`/`CpuIp` |
| ④ OPC 通讯 | CpuIp、4840、灯、UaExpert | **未真做**；`CpuIp` 只是占位；信号在内存里 |
| ⑤ 握手信号 | Online/Downline Request ↔ Allow | 控制台 `[PLC ]` / `[数采]` 日志 |
| ⑥ 前端追溯 | 条码时间线 | Web 看板最近 10 条 + 按条码查询 |

现场断在 ④ 时，FakeMes 帮不上忙——那是真网线 / OPC。  
①②③⑤⑥ 你已经能在 FakeMes 里完整走通。

---

## 三、信号名（完全对齐）

| 方向 | 信号 | FakeMes |
|------|------|---------|
| PLC→MES | `OnlineRequest` | `FakePlc.OnlineRequest` |
| MES→PLC | `AllowOnline` / `NotAllowOnline` | `WriteOnlineResult` |
| PLC→MES | `DownlineRequest` | `FakePlc.DownlineRequest` |
| MES→PLC | `AllowDownline` / `NotAllowDownline` | `WriteDownlineResult` |
| 可选 | 条码 / Message | `Barcode` / `MessageFromMes` |

现场还有 `{DbPrefix}_MESProcess_Data_DB...` 节点路径；练手里用属性名代替 NodeId。

---

## 四、进站六拍 —— FakeMes 勾选表

启动：API + Web；Simulator 选工位 + 剧本 **1（正常）**。

| 拍 | 现场看哪里 | FakeMes 看哪里 | ☐ |
|----|------------|----------------|---|
| ① 请求 | PLC `OnlineRequest` | 日志 `[PLC ] 写条码 + OnlineRequest=1` | ☐ |
| ② 感知 | 数采界面日志 | 日志 `[数采] 发现 OnlineRequest=1` | ☐ |
| ③ 调用 | daq/mam 日志 | 日志 `[数采] → POST api/daq/track-in`；API 控制台 SQL | ☐ |
| ④ 应答 | FromMes Allow/NotAllow | `[数采] ← 写回 AllowOnline` + `[PLC ] 收到 AllowOnline` | ☐ |
| ⑤ 落库 | DB 在制/进站 | SQL Server `TrackRecords` 或 Web 看板今日进站 +1 | ☐ |
| ⑥ 可见 | Web 追溯 | 复制条码到 Web 查询，看到 **进站** | ☐ |

拒绝对照（剧本 2/3）：④ 应为 `NotAllowOnline`，⑥ 可无进站记录；数采 msg 与后端 `message` 一致。

---

## 五、出站六拍 —— FakeMes 勾选表

接在进站 Allow 之后（剧本 1 会自动出站）。

| 拍 | 现场看哪里 | FakeMes 看哪里 | ☐ |
|----|------------|----------------|---|
| ① 请求 | `DownlineRequest` | `[PLC ] DownlineRequest=1` | ☐ |
| ② 感知 | 数采日志 | `[数采] 发现 DownlineRequest=1` | ☐ |
| ③ 调用 | daq/mam | `[数采] → POST api/daq/track-out` | ☐ |
| ④ 应答 | Allow/NotAllow Downline | `AllowDownline` / 剧本4 的 `NotAllowDownline` | ☐ |
| ⑤ 落库 | DB 出站 | `TrackRecords` 多一条 Out | ☐ |
| ⑥ 可见 | Web 追溯 | 同一条码看到 **进站→出站** 时间串 | ☐ |

---

## 六、时间线 T0～T11 压缩版

| 现场 | FakeMes |
|------|---------|
| T0 三端对表 | 确认 API:5251、Web:5173、Simulator 选同一工位 |
| T1 主数据 | Web 有该工位（种子 OP10 或你新增的） |
| T2 后端/数采活着 | API 已 `Now listening`；Simulator 已启动 Daq 轮询 |
| T3 清残留 | 每件开始前 FakePlc 会清握手 |
| T4 OnlineRequest | `[PLC ] OnlineRequest=1` |
| T5 数采→后端→Allow | `[数采]` POST + Allow 日志 |
| T6 DB+Web | 看板 / 追溯 |
| T7～T8 加工+Downline | 等待 ProcessSeconds 后 Downline |
| T9～T10 出站+Web | AllowDownline + 追溯两行 |
| T11 清握手 | `ClearDownlineHandshake` |

---

## 七、故障表：现场现象 → FakeMes 怎么复现/理解

| 手册现象 | FakeMes 怎么理解 |
|----------|------------------|
| Web 无设备 | 删光工位 / 不新增 → 进站「工位不存在」 |
| 数采打错 API | 改 Simulator `ApiBaseUrl` 成错误地址 → `[数采] 异常` |
| 有 Online 无数采日志 | （真 OPC/DbPrefix 问题）练手里若停掉 Daq 轮询才会出现 |
| 后端拒 | 剧本 2/3/4 或 `dotnet test` |
| 后端成功 PLC 无 Allow | 练手里若 Daq 调 API 成功但忘记 `WriteOnlineResult` 就会；当前代码已写回 |
| Web 查无 | 查错条码；或 API/Web 不是同一套库 |

---

## 八、建议练习顺序（30 分钟）

1. 开 API + Web + Simulator（剧本 1），用上面进站/出站六拍表各勾一遍。  
2. 跑剧本 **5**，确认三种 NotAllow 的 msg 和手册「业务拒」一节对得上。  
3. 打开公司手册第六节，把 FakeMes 勾选表和现场 ☐ 并排看，标出「现场多出来的」：OPC、计划、工艺路线、Identity、网关。  
4. （加分）在真仓库里用关键字搜 `OnlineRequest` / `AllowOnline`，找到数采真实写点代码，和 `DaqWorker` 对照。

---

## 九、一句话记住

> FakeMes 练的是手册里的 **业务链路 + 握手顺序**；  
> 现场多出来的是 **真 OPC、主数据复杂度、权限与多服务**。  
> 联调断线时：先问「断在六层的哪一层」，再决定动 Web / 后端 / 数采配置 / OPC / 信号 / 追溯。
