using FakeMes.Api.Domain;
using FakeMes.Api.Options;
using FakeMes.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FakeMes",
        Version = "v1.0",
        Description = "FakeMes 系统 WebApi 接口"
    });
});
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

builder.Services.Configure<MaintenanceOptions>(
    builder.Configuration.GetSection(MaintenanceOptions.SectionName));

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("缺少连接字符串 ConnectionStrings:Default");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddScoped<StationService>();
builder.Services.AddScoped<MaintenanceService>();
builder.Services.AddHostedService<MaintenanceBackgroundService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
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

app.Run();
