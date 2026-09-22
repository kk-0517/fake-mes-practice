# FakeMes Practice（.NET + Vue）

无真实 PLC/设备的 **进站 → 出站 → 网页追溯** 练手项目。

```text
FakeMes.Simulator（假 PLC + 假数采握手）
        ↓ HTTP
FakeMes.Api（.NET + SQL Server LocalDB）
        ↑
FakeMes.Web（Vue 追溯页）
```

## 环境

- .NET 10 SDK（本机已验证）
- Node.js 16+（前端用 Vite 4）
- SQL Server LocalDB（本机已有 `MSSQLLocalDB`；连接串在 `src/FakeMes.Api/appsettings.json`）

## 启动顺序（三个终端）

### 1. 后端 API

```bash
cd src/FakeMes.Api
dotnet run --launch-profile http
```

默认地址：`http://localhost:5251`（自动跳转到 Swagger；新增 Controller 会自动出现）

### 2. 模拟数采

```bash
cd src/FakeMes.Simulator
dotnet run
```

会先选工位（模拟工位机绑设备），然后按现场握手跑：

```text
假PLC OnlineRequest → 数采调 track-in → AllowOnline/NotAllowOnline
假PLC DownlineRequest → 数采调 track-out → AllowDownline/NotAllowDownline
```

控制台会分别打印 `[PLC ]` 与 `[数采]` 日志。启动时可先选工位，再选剧本：

| 剧本 | 期望 |
|------|------|
| 1 正常循环 | AllowOnline → AllowDownline |
| 2 空条码 | NotAllowOnline「条码为空」 |
| 3 重复进站 | 第二次 NotAllowOnline「已在站内」 |
| 4 未进站出站 | NotAllowDownline「未进站」 |
| 5 全演示 | 2+3+4 各跑一遍 |

`CpuIp` 在 `appsettings.json` 里，仅作现场设备 IP 的占位说明。

详见 [docs/flow.md](docs/flow.md)。  
对照公司联调手册：[docs/manual-mapping.md](docs/manual-mapping.md)（六层 / 六拍勾选表）。

### 3. 前端

```bash
cd src/FakeMes.Web
npm install
npm run dev
```

浏览器打开：`http://localhost:5173`  
把 Simulator 打印的条码粘贴进查询框。

## 接口

| 方法 | 路径 | 说明 |
|------|------|------|
| POST | `/api/daq/track-in` | 进站 |
| POST | `/api/daq/track-out` | 出站 |
| GET | `/api/stations` | 工位列表 |
| POST | `/api/stations` | 新增工位 `{ "code","name" }` |
| GET | `/api/trace?barcode=SNxxxx` | 条码追溯 |
| GET | `/api/dashboard?recent=10` | 今日进站数 + 最近 N 条 |

请求体示例：

```json
{ "stationCode": "OP10", "barcode": "SN143015" }
```

## 业务规则（P0）

1. 条码空 → 拒绝  
2. 同工位已进站未出站 → 拒绝再进站  
3. 未进站 → 拒绝出站  
4. 成功则写入 SQL Server（库名 `FakeMes`，首次启动自动建库表）

## 数据维护（正式 MES 风格）

热库 `TrackRecords` 只保留近期数据；超过保留期的记录归档到 `TrackRecordArchives`（追溯仍可查）。

配置见 `src/FakeMes.Api/appsettings.json` → `Maintenance`：

| 项 | 含义 | 默认 |
|----|------|------|
| RetentionDays | 热库保留天数 | 90 |
| IntervalHours | 定时归档间隔（小时） | 24 |
| Enabled | 是否启用后台定时任务 | true |
| BatchSize | 单次归档批大小 | 1000 |

Swagger「维护」分组：

- `GET /api/maintenance/status` 查看热库/归档数量与上次执行
- `POST /api/maintenance/archive` 立刻执行一次归档

练习时可把 `RetentionDays` 临时改成 `0` 或 `1` 再点归档，观察数据从 `TrackRecords` 挪到 `TrackRecordArchives`。

## 自动化测试

```bash
dotnet test
```

覆盖：空条码、工位不存在、重复进站、未进站出站、正常进出站、重复工位码。测试用 EF InMemory，不依赖 SQL Server。

## 建议你接着改

- 按 [docs/manual-mapping.md](docs/manual-mapping.md) 勾一遍进站/出站六拍
- 在真仓库搜 `OnlineRequest`，对照 `DaqWorker`
- 提交并推送到 GitHub
- 真 OPC UA（可选，需仿真器如 Prosys）
