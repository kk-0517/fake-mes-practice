# FakeMes Practice（.NET + Vue）

无真实 PLC/设备的 **进站 → 出站 → 网页追溯** 练手项目。

```text
FakeMes.Simulator（假数采）
        ↓ HTTP
FakeMes.Api（.NET + SQLite）
        ↑
FakeMes.Web（Vue 追溯页）
```

## 环境

- .NET 10 SDK（本机已验证）
- Node.js 16+（前端用 Vite 4）

## 启动顺序（三个终端）

### 1. 后端 API

```bash
cd src/FakeMes.Api
dotnet run --launch-profile http
```

默认地址：`http://localhost:5251`

### 2. 模拟数采

```bash
cd src/FakeMes.Simulator
dotnet run
```

会每隔几秒自动：`track-in` → 等待加工 → `track-out`，控制台打印条码与 Allow/拒绝。

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
| GET | `/api/trace?barcode=SNxxxx` | 条码追溯 |

请求体示例：

```json
{ "stationCode": "OP10", "barcode": "SN143015" }
```

## 业务规则（P0）

1. 条码空 → 拒绝  
2. 同工位已进站未出站 → 拒绝再进站  
3. 未进站 → 拒绝出站  
4. 成功则写入 SQLite（`src/FakeMes.Api/fakemes.db`）

## 建议你接着改

- Web 上新增工位
- 今日进站数看板
- 重复进站/未进站出站的拒绝用例自动化测试
