using BackendV2.Api.Infrastructure.Persistence;
using BackendV2.Api.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddDbContext<BackendV2.Api.Infrastructure.Persistence.AppDbContext>(options =>
{
    var configuration = builder.Configuration;
    var conn = Environment.GetEnvironmentVariable("BACKENDV2_DB") ?? configuration.GetConnectionString("Default") ?? string.Empty;
    options.UseNpgsql(conn, o => o.UseNetTopologySuite());
});
builder.Services.AddSignalR();
builder.Services.AddSingleton<BackendV2.Api.Infrastructure.Messaging.NatsConnection>();
builder.Services.AddScoped<BackendV2.Api.Service.Realtime.RealtimeSnapshotService>();
builder.Services.AddScoped<BackendV2.Api.Service.Core.RobotRegistryService>();
builder.Services.AddScoped<BackendV2.Api.Service.Ingestion.StateIngestionService>();
builder.Services.AddSingleton<BackendV2.Api.Service.Tasks.NatsPublisherStub>();
builder.Services.AddScoped<BackendV2.Api.Service.Tasks.TaskManagerService>();
builder.Services.AddScoped<BackendV2.Api.Service.Routes.RoutePlannerService>();
builder.Services.AddScoped<BackendV2.Api.Service.Traffic.TrafficControlService>();
builder.Services.AddScoped<BackendV2.Api.Service.Config.ConfigurationService>();
builder.Services.AddScoped<BackendV2.Api.Service.Schedule.SchedulePublisherService>();
builder.Services.AddScoped<BackendV2.Api.Service.Teach.TeachingService>();
builder.Services.AddScoped<BackendV2.Api.Service.Missions.MissionService>();
builder.Services.AddScoped<BackendV2.Api.Service.Sim.SimulationService>();
builder.Services.AddScoped<BackendV2.Api.Service.Ops.OpsService>();
builder.Services.AddScoped<BackendV2.Api.Service.Replay.ReplayService>();
builder.Services.AddSingleton<BackendV2.Api.Service.Replay.ReplayStreamingCoordinator>();
builder.Services.AddScoped<BackendV2.Api.Service.Timescale.TimescaleQueryService>();
builder.Services.AddHostedService<BackendV2.Api.Workers.OfflineDetectionWorker>();
builder.Services.AddHostedService<BackendV2.Api.Workers.SchedulePublisherWorker>();
builder.Services.AddHostedService<BackendV2.Api.Workers.OpsMetricsWorker>();
builder.Services.AddHostedService<BackendV2.Api.Workers.NatsConsumersWorker>();
builder.Services.AddHostedService<BackendV2.Api.Workers.NatsJetStreamSetupWorker>();
builder.Services.AddHostedService<BackendV2.Api.Workers.BackendSimControlWorker>();
builder.Services.AddHostedService<BackendV2.Api.Workers.SimDriverWorker>();
builder.Services.AddHostedService<BackendV2.Api.Workers.TimescaleInitializerWorker>();
var jwtOptions = new JwtOptions();
builder.Configuration.GetSection("Jwt").Bind(jwtOptions);
builder.Services.AddSingleton(jwtOptions);
builder.Services.AddSingleton<PasswordHasher>();
builder.Services.AddSingleton(new JwtTokenService(jwtOptions));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var key = Environment.GetEnvironmentVariable("BACKENDV2_JWTKEY") ?? jwtOptions.Key;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            NameClaimType = "unique_name",
            RoleClaimType = "roles"
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async ctx =>
            {
                var db = ctx.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                var sub = ctx.Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                var jti = ctx.Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
                if (!string.IsNullOrEmpty(sub) && Guid.TryParse(sub, out var userId))
                {
                    var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId);
                    if (user == null || user.IsDisabled)
                    {
                        ctx.Fail("user_disabled");
                        return;
                    }
                }
                if (!string.IsNullOrEmpty(jti))
                {
                    var revoked = await db.RevokedTokens.AsNoTracking().AnyAsync(x => x.Jti == jti);
                    if (revoked)
                    {
                        ctx.Fail("token_revoked");
                        return;
                    }
                }
            }
        };
    });
builder.Services.AddAuthorization(AuthorizationPolicies.AddPolicies);
builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.Policy.IAuthorizationMiddlewareResultHandler, AuthorizationAuditHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.CanConnect())
    {
        app.Logger.LogInformation("Database connection successful.");
    }
    else
    {
        app.Logger.LogError("Database connection failed. Check BACKENDV2_DB or ConnectionStrings:Default.");
    }
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<BackendV2.Api.Hub.RealtimeHub>("/hubs/realtime");

app.Run();
