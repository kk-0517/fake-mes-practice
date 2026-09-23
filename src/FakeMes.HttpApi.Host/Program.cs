using FakeMes.Application;
using FakeMes.Domain.Stations;
using FakeMes.EntityFrameworkCore;
using FakeMes.HttpApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFakeMesHttpApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FakeMes",
        Version = "v1.0",
        Description = "FakeMes 系统 WebApi 接口（ABP 分层）"
    });
});
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("缺少连接字符串 ConnectionStrings:Default");

builder.Services.AddFakeMesEntityFrameworkCore(connectionString);
builder.Services.AddFakeMesApplication(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FakeMesDbContext>();
    DatabaseSchemaBootstrap.EnsureAsync(db).GetAwaiter().GetResult();
    if (!db.Stations.Any())
    {
        db.Stations.Add(new Station { Code = "OP10", Name = "组装工位" });
        db.SaveChanges();
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "FakeMes API");
        options.DocumentTitle = "FakeMes API";
        options.DisplayRequestDuration();
    });
}

app.UseCors();
app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

if (app.Environment.IsDevelopment())
{
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        var url = "http://localhost:5251/swagger";
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch
        {
            // 浏览器打不开不影响服务运行
        }
    });
}

app.Run();
