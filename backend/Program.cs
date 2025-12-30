using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Backend.Options;
using Backend.Services;
using Backend.Hubs;
using Backend.Database;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddDbContext<Backend.Infrastructure.Persistence.AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Database"),
        npgsql => npgsql.UseNetTopologySuite()));
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddCors(opt =>
{
    opt.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
builder.Services.Configure<NatsOptions>(builder.Configuration.GetSection("Nats"));
builder.Services.AddSingleton<NatsService>();
builder.Services.AddHostedService<RobotTelemetrySubscriber>();
builder.Services.AddScoped<MapRepository>();
builder.Services.AddScoped<RobotRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();
app.UseCors();
app.MapHub<RobotsHub>("/hub/robots");

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
