using BackendV2.Api.Infrastructure.Persistence;
using BackendV2.Api.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;

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
builder.Services.AddScoped<BackendV2.Api.Data.Auth.UserRepository>();
builder.Services.AddScoped<BackendV2.Api.Data.Auth.RoleRepository>();
builder.Services.AddScoped<BackendV2.Api.Data.Auth.AccessPolicyRepository>();
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
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:3000", "http://localhost:3001")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Initialize database and seed roles/admin
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (await db.Database.CanConnectAsync())
    {
        await db.Database.MigrateAsync();
        app.Logger.LogInformation("Database connection successful.");
        var needed = new[] { "Admin", "Planner", "Operator", "Viewer" };
        foreach (var name in needed)
        {
            var exists = await db.Roles.AsNoTracking().AnyAsync(r => r.Name == name);
            if (!exists)
            {
                await db.Roles.AddAsync(new BackendV2.Api.Model.Auth.Role { RoleId = Guid.NewGuid(), Name = name });
            }
        }
        await db.SaveChangesAsync();
        var seedAdmin = (Environment.GetEnvironmentVariable("BACKENDV2_SEED_ADMIN") ?? "false").Equals("true", StringComparison.OrdinalIgnoreCase);
        if (seedAdmin)
        {
            var adminUser = Environment.GetEnvironmentVariable("BACKENDV2_ADMIN_USER") ?? "admin";
            var adminPass = Environment.GetEnvironmentVariable("BACKENDV2_ADMIN_PASSWORD");
            if (!string.IsNullOrEmpty(adminPass))
            {
                var existing = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == adminUser);
                if (existing == null)
                {
                    var hasher = scope.ServiceProvider.GetRequiredService<PasswordHasher>();
                    var u = new BackendV2.Api.Model.Auth.User { UserId = Guid.NewGuid(), Username = adminUser, DisplayName = "Administrator", PasswordHash = hasher.Hash(adminPass), IsDisabled = false };
                    await db.Users.AddAsync(u);
                    var adminRole = await db.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Name == "Admin");
                    if (adminRole != null) await db.UserRoles.AddAsync(new BackendV2.Api.Model.Auth.UserRole { UserId = u.UserId, RoleId = adminRole.RoleId });
                    await db.SaveChangesAsync();
                    app.Logger.LogInformation("Seeded admin user '{adminUser}'", adminUser);
                }
            }
            else
            {
                app.Logger.LogWarning("BACKENDV2_ADMIN_PASSWORD not set; skipping admin seeding");
            }
        }
    }
    else
    {
        app.Logger.LogError("Database connection failed. Check BACKENDV2_DB or ConnectionStrings:Default.");
    }
}

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.Use(async (ctx, next) =>
{
    await next();
    if (ctx.Response.StatusCode == StatusCodes.Status403Forbidden || ctx.Response.StatusCode == StatusCodes.Status401Unauthorized)
    {
        var db = ctx.RequestServices.GetRequiredService<AppDbContext>();
        Guid? actor = null;
        var sub = ctx.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (Guid.TryParse(sub, out var g)) actor = g;
        var path = ctx.Request.Path.Value ?? "";
        await db.AuditEvents.AddAsync(new BackendV2.Api.Model.Ops.AuditEvent
        {
            AuditEventId = Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow,
            ActorUserId = actor,
            Action = "authorization.denied",
            TargetType = "endpoint",
            TargetId = path,
            Outcome = "DENIED",
            DetailsJson = "{}"
        });
        await db.SaveChangesAsync();
    }
});

app.MapControllers();
app.MapHub<BackendV2.Api.Hub.RealtimeHub>("/hubs/realtime");

app.Run();
