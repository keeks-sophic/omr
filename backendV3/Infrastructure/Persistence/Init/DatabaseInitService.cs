using BackendV3.Modules.Auth.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BackendV3.Infrastructure.Persistence.Init;

public sealed class DatabaseInitService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly IHostEnvironment _env;
    private readonly IConfiguration _config;
    private readonly ILogger<DatabaseInitService> _logger;

    public DatabaseInitService(IServiceProvider services, IHostEnvironment env, IConfiguration config, ILogger<DatabaseInitService> logger)
    {
        _services = services;
        _env = env;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connString = _config.GetConnectionString("Database");
        if (!string.IsNullOrWhiteSpace(connString))
        {
            await EnsureDatabaseExistsAsync(connString, stoppingToken);
        }

        await using var scope = _services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        foreach (var sql in DatabaseInitSql.Required)
        {
            await db.Database.ExecuteSqlRawAsync(sql, stoppingToken);
        }

        foreach (var sql in DatabaseInitSql.Optional)
        {
            try
            {
                await db.Database.ExecuteSqlRawAsync(sql, stoppingToken);
            }
            catch (PostgresException ex) when (ex.SqlState is "0A000" or "42704" or "58P01")
            {
                _logger.LogWarning("Skipping optional database init step: {MessageText}", ex.MessageText);
            }
        }

        await EfMigrationsBootstrapper.EnsureHistoryMatchesExistingSchemaAsync(db, _logger, stoppingToken);
        await db.Database.MigrateAsync(stoppingToken);
        await MapsMultiMapMigration.ApplyAsync(db, _logger, stoppingToken);

        if (_env.IsDevelopment())
        {
            await AuthSeed.EnsureSeededAsync(db, scope.ServiceProvider, stoppingToken);
        }
    }

    private async Task EnsureDatabaseExistsAsync(string connectionString, CancellationToken ct)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var dbName = builder.Database;
        if (string.IsNullOrWhiteSpace(dbName)) return;

        builder.Database = "postgres";
        builder.Pooling = false;

        await using var conn = new NpgsqlConnection(builder.ConnectionString);
        await conn.OpenAsync(ct);

        await using (var cmd = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = @name;", conn))
        {
            cmd.Parameters.AddWithValue("name", dbName);
            var exists = await cmd.ExecuteScalarAsync(ct);
            if (exists != null)
            {
                return;
            }
        }

        if (!IsSafeDbIdentifier(dbName))
        {
            throw new InvalidOperationException($"Unsafe database name '{dbName}'.");
        }

        _logger.LogInformation("Creating database {Database}", dbName);
        await using (var create = new NpgsqlCommand($"CREATE DATABASE \"{dbName}\";", conn))
        {
            await create.ExecuteNonQueryAsync(ct);
        }
    }

    private static bool IsSafeDbIdentifier(string name)
    {
        foreach (var ch in name)
        {
            var ok = char.IsLetterOrDigit(ch) || ch == '_' || ch == '-';
            if (!ok) return false;
        }
        return true;
    }
}
