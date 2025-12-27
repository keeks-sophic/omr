using backend.Services;
using backend.Hubs;
using backend.Mappers;
using backend.Options;
using backend.Workers;
using backend.Data;
using backend.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using backend.Options;
using backend.DTOs;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins("http://localhost:3000", "http://localhost:3001")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});
builder.Services.AddSignalR();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var conn = builder.Configuration.GetConnectionString("Default");
    options.UseNpgsql(conn, o => o.UseNetTopologySuite());
});
builder.Services.AddScoped<IRobotRepository, RobotRepository>();
builder.Services.AddScoped<IRobotService, RobotService>();
builder.Services.AddScoped<IMapRepository, MapRepository>();
builder.Services.AddScoped<IMapService, MapService>();
builder.Services.AddScoped<IRouteRepository, RouteRepository>();
builder.Services.AddScoped<IRoutePlannerService, RoutePlannerService>();
builder.Services.AddSingleton<ITrafficControlService, TrafficControlService>();
builder.Services.AddSingleton<NatsService>();
builder.Services.Configure<NatsOptions>(builder.Configuration.GetSection("Nats"));
builder.Services.Configure<ControlOptions>(builder.Configuration.GetSection("Control"));
builder.Services.AddHostedService<RobotStreamWorker>();
var controlCfg = builder.Configuration.GetSection("Control").Get<ControlOptions>() ?? new ControlOptions();
if (controlCfg.EnableSimulation)
{
    builder.Services.AddHostedService<RobotControlWorker>();
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowFrontend");
// app.UseHttpsRedirection();
app.MapHub<RobotHub>("/hub/robots");
app.MapHub<MapHub>("/hub/maps");

app.MapGet("/robots", (IRobotService service) =>
{
    var robots = service.GetAll().Select(RobotMapper.ToDto).ToArray();
    return Results.Ok(robots);
})
.WithName("GetRobots");

app.MapGet("/robots/{ip}", (string ip, IRobotService service) =>
{
    var robot = service.GetByIp(ip);
    return robot is null
        ? Results.NotFound()
        : Results.Ok(RobotMapper.ToDto(robot));
})
.WithName("GetRobotByIp");

app.MapGet("/maps", (IMapService service) =>
{
    var maps = service.GetAll().Select(MapMapper.ToDto).ToArray();
    return Results.Ok(maps);
})
.WithName("GetMaps");

app.MapGet("/maps/{id}/graph", (int id, IMapService service) =>
{
    var graph = service.GetGraph(id);
    return graph is null
        ? Results.NotFound()
        : Results.Ok(graph);
})
.WithName("GetMapGraph");

app.MapPost("/maps/graph", async (MapGraphDto graph, IMapService service, IHubContext<MapHub> hub) =>
{
    if (graph.Nodes.Count > 0)
    {
        var origin = graph.Nodes[0];
        foreach (var n in graph.Nodes)
        {
            n.X -= origin.X;
            n.Y -= origin.Y;
        }
    }
    var saved = service.SaveGraph(graph);
    await hub.Clients.All.SendAsync("MapUpdated", new { mapId = saved.Id, ts = DateTime.UtcNow });
    return Results.Ok(new { id = saved.Id });
})
.WithName("SaveMapGraph");

app.MapPost("/routes/plan", (RouteRequestDto req, IRoutePlannerService planner) =>
{
    var plan = planner.Plan(req);
    return plan is null ? Results.BadRequest() : Results.Ok(plan);
})
.WithName("PlanRoute");

app.MapPost("/routes/send", async (RouteAssignDto assign, NatsService nats, IOptions<NatsOptions> opts, ITrafficControlService traffic) =>
{
    traffic.AssignRoute(assign);
    var payload = new { ip = assign.RobotIp, command = "route.assign", route = assign, ts = DateTime.UtcNow };
    await nats.PublishJsonAsync(opts.Value.CommandSubject, payload);
    return Results.Ok(new { sent = true });
})
.WithName("SendRoute");

app.MapPost("/traffic/guidance", (GuidanceRequestDto req, ITrafficControlService traffic) =>
{
    var res = traffic.Guide(req);
    return Results.Ok(res);
})
.WithName("TrafficGuidance");

app.MapPost("/robots/{ip}/assign-map/{mapId:int}", async (string ip, int mapId, IRobotService robots, NatsService nats, IOptions<NatsOptions> opts) =>
{
    var r = robots.GetByIp(ip);
    if (r is null) return Results.NotFound();
    r.MapId = mapId;
    r.X = 0;
    r.Y = 0;
    r.Geom = new NetTopologySuite.Geometries.Point(0, 0) { SRID = 0 };
    robots.UpdateTelemetry(ip, true, r.Name, true ? 0 : r.X, true ? 0 : r.Y, r.State, r.Battery, mapId);
    var dto = RobotMapper.ToDto(robots.GetByIp(ip)!);
    await nats.PublishJsonAsync(opts.Value.CommandSubject, new { ip, command = "robot.sync", robot = dto, ts = DateTime.UtcNow });
    return Results.Ok();
})
.WithName("AssignRobotToMap");

app.MapDelete("/robots/{ip}/map", async (string ip, IRobotService robots, NatsService nats, IOptions<NatsOptions> opts) =>
{
    var r = robots.GetByIp(ip);
    if (r is null) return Results.NotFound();
    robots.UnassignFromMap(ip);
    var dto = RobotMapper.ToDto(robots.GetByIp(ip)!);
    await nats.PublishJsonAsync(opts.Value.CommandSubject, new { ip, command = "robot.sync", robot = dto, ts = DateTime.UtcNow });
    return Results.Ok();
})
.WithName("UnassignRobotFromMap");

app.MapPost("/robots/{ip}/relocate", async (string ip, RelocateDto pos, IRobotService robots, IHubContext<RobotHub> hub, IOptions<NatsOptions> opts, NatsService nats) =>
{
    var existing = robots.GetByIp(ip);
    var updated = robots.UpdateTelemetry(
        ip,
        true,
        existing?.Name ?? ip,
        pos.X,
        pos.Y,
        existing?.State,
        existing?.Battery,
        pos.MapId ?? existing?.MapId
    );
    var dto = RobotMapper.ToDto(updated);
    await hub.Clients.All.SendAsync("telemetry", dto);
    await nats.PublishJsonAsync(opts.Value.CommandSubject, new { ip, command = "robot.sync", robot = dto, ts = DateTime.UtcNow });
    return Results.Ok();
})
.WithName("RelocateRobot");

app.MapPost("/robots/{ip}/navigate", async (string ip, NavigateDto dest, IOptions<NatsOptions> opts, NatsService nats) =>
{
    var payload = new { ip, command = "navigate.request", dest = new { x = dest.X, y = dest.Y, mapId = dest.MapId }, ts = DateTime.UtcNow };
    await nats.PublishJsonAsync(opts.Value.CommandSubject, payload);
    return Results.Ok(new { sent = true });
})
.WithName("NavigateRobotToPoint");


app.MapPost("/dev/command/{ip}/{cmd}", async (string ip, string cmd, NatsService nats, IOptions<NatsOptions> opts) =>
{
    var payload = new { ip, command = cmd, ts = DateTime.UtcNow };
    await nats.PublishJsonAsync(opts.Value.CommandSubject, payload);
    return Results.Ok(new { sent = true, payload });
})
.WithName("SendDevCommand");

app.Run();
