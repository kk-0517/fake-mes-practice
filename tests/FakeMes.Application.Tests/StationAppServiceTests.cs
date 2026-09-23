using FakeMes.Application.Contracts.Daq;
using FakeMes.Application.Contracts.Stations;
using FakeMes.Application.Stations;
using FakeMes.Domain.Stations;
using FakeMes.EntityFrameworkCore;
using FakeMes.EntityFrameworkCore.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FakeMes.Application.Tests;

public class StationAppServiceTests : IDisposable
{
    private readonly FakeMesDbContext _db;
    private readonly StationAppService _sut;

    public StationAppServiceTests()
    {
        var options = new DbContextOptionsBuilder<FakeMesDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new FakeMesDbContext(options);
        _db.Stations.Add(new Station { Code = "OP10", Name = "组装工位" });
        _db.SaveChanges();

        _sut = new StationAppService(
            new EfStationRepository(_db),
            new EfTrackRecordRepository(_db),
            new EfUnitOfWork(_db));
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task TrackIn_EmptyBarcode_ShouldReject()
    {
        var result = await _sut.TrackInAsync(new TrackRequestDto("OP10", "  "));

        Assert.False(result.Allow);
        Assert.Equal("条码为空", result.Message);
    }

    [Fact]
    public async Task TrackIn_UnknownStation_ShouldReject()
    {
        var result = await _sut.TrackInAsync(new TrackRequestDto("OP99", "SN001"));

        Assert.False(result.Allow);
        Assert.Contains("工位不存在", result.Message);
    }

    [Fact]
    public async Task TrackIn_DuplicateWhileInStation_ShouldReject()
    {
        var first = await _sut.TrackInAsync(new TrackRequestDto("OP10", "SN001"));
        var second = await _sut.TrackInAsync(new TrackRequestDto("OP10", "SN001"));

        Assert.True(first.Allow);
        Assert.False(second.Allow);
        Assert.Equal("已在站内", second.Message);
    }

    [Fact]
    public async Task TrackOut_WithoutTrackIn_ShouldReject()
    {
        var result = await _sut.TrackOutAsync(new TrackRequestDto("OP10", "SN999"));

        Assert.False(result.Allow);
        Assert.Equal("未进站，不能出站", result.Message);
    }

    [Fact]
    public async Task TrackInThenTrackOut_ShouldAllowBoth()
    {
        var trackIn = await _sut.TrackInAsync(new TrackRequestDto("OP10", "SN100"));
        var trackOut = await _sut.TrackOutAsync(new TrackRequestDto("OP10", "SN100"));

        Assert.True(trackIn.Allow);
        Assert.True(trackOut.Allow);

        var trace = await _sut.GetTraceAsync("SN100");
        Assert.Equal(2, trace.Count);
        Assert.Equal("In", trace[0].Type);
        Assert.Equal("Out", trace[1].Type);
    }

    [Fact]
    public async Task CreateStation_DuplicateCode_ShouldReject()
    {
        var result = await _sut.CreateStationAsync(new CreateStationRequestDto("OP10", "重复"));

        Assert.False(result.Ok);
        Assert.Contains("已存在", result.Message);
    }
}
