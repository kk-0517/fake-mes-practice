# 链路说明

```text
【假 PLC】FakeMes.Simulator
   定时生成条码
   POST /api/daq/track-in
   POST /api/daq/track-out
        ↓
【后端】FakeMes.Api
   StationService 校验允许/拒绝
   SQLite 落库 TrackRecords
        ↑
【前端】FakeMes.Web
   GET /api/trace?barcode=...
   展示进站/出站时间线
```

对应真实 MES：

| 练手 | 现场 |
|------|------|
| Simulator | 数采 WPF + OPC/PLC |
| Api daq | apis/daq |
| Web 追溯 | 管理端查询页 |
