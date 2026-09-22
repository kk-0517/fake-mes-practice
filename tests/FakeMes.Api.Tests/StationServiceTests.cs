using FakeMes.Api.Domain;
using FakeMes.Api.Dtos;
using FakeMes.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace FakeMes.Api.Tests;

public class StationServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly StationService _sut;

    public StationServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        _db.Stations.Add(new Station { Code = "OP10", Name = "组装工位" });
        _db.SaveChanges();
        _sut = new StationService(_db);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task TrackIn_EmptyBarcode_ShouldReject()
    {
        var result = await _sut.TrackInAsync(new TrackRequest("OP10", "  "));

        Assert.False(result.Allow);
        Assert.Equal("条码为空", result.Message);
    }

    [Fact]
    public async Task TrackIn_UnknownStation_ShouldReject()
    {
        var result = await _sut.TrackInAsync(new TrackRequest("OP99", "SN001"));

        Assert.False(result.Allow);
        Assert.Contains("工位不存在", result.Message);
    }

    [Fact]
    public async Task TrackIn_DuplicateWhileInStation_ShouldReject()
    {
        var first = await _sut.TrackInAsync(new TrackRequest("OP10", "SN001"));
        var second = await _sut.TrackInAsync(new TrackRequest("OP10", "SN001"));

        Assert.True(first.Allow);
        Assert.False(second.Allow);
        Assert.Equal("已在站内", second.Message);
    }

    [Fact]
    public async Task TrackOut_WithoutTrackIn_ShouldReject()
    {
        var result = await _sut.TrackOutAsync(new TrackRequest("OP10", "SN999"));

        Assert.False(result.Allow);
        Assert.Equal("未进站，不能出站", result.Message);
    }

    [Fact]
    public async Task TrackInThenTrackOut_ShouldAllowBoth()
    {
        var trackIn = await _sut.TrackInAsync(new TrackRequest("OP10", "SN100"));
        var trackOut = await _sut.TrackOutAsync(new TrackRequest("OP10", "SN100"));

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
        var result = await _sut.CreateStationAsync(new CreateStationRequest("OP10", "重复"));

        Assert.False(result.Ok);
        Assert.Contains("已存在", result.Message);
    }
}
