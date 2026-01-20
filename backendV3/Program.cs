using BackendV3.Infrastructure.Config;
using BackendV3.Infrastructure.Persistence;
using BackendV3.Infrastructure.Persistence.Init;
using BackendV3.Infrastructure.Security;
using BackendV3.Modules.Auth.Data;
using BackendV3.Modules.Maps.Service;
using BackendV3.Realtime;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

DotEnv.Load();
Environment.SetEnvironmentVariable("ConnectionStrings__Database", null);

var builder = WebApplication.CreateBuilder(args);

JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

builder.Services.AddOpenApi();

builder.Services.AddControllers();
builder.Services.AddSignalR();

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Database"),
        npgsql => npgsql.UseNetTopologySuite()));

builder.Services.AddSingleton<PasswordHasher>();
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<RoleRepository>();
builder.Services.AddScoped<TokenRepository>();
builder.Services.AddScoped<MapHubPublisher>();
builder.Services.AddScoped<MapManagementService>();
builder.Services.AddScoped<MapSnapshotService>();

var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
builder.Services.AddSingleton(jwtOptions);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        var key = Environment.GetEnvironmentVariable("BACKENDV3_JWTKEY") ?? jwtOptions.Key;
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            NameClaimType = "unique_name",
            RoleClaimType = "roles",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
        opt.Events = JwtBearerEventsFactory.Create();
    });

builder.Services.AddAuthorization(AuthorizationPolicies.AddPolicies);

builder.Services.AddHostedService<DatabaseInitService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<RealtimeHub>(SignalRRoutes.RealtimeHubPath);

app.Run();
