using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using BackendV2.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BackendV2.Api.Workers;

public class TimescaleInitializerWorker : BackgroundService
{
    private readonly AppDbContext _db;
    private readonly ILogger<TimescaleInitializerWorker> _logger;
    public TimescaleInitializerWorker(AppDbContext db, ILogger<TimescaleInitializerWorker> logger)
    {
        _db = db;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            if (!await _db.Database.CanConnectAsync(stoppingToken)) return;
            await _db.Database.ExecuteSqlRawAsync("CREATE EXTENSION IF NOT EXISTS timescaledb", stoppingToken);
            await _db.Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS idx_robot_events_robot_ts ON replay.robot_events(robot_id, timestamp)", stoppingToken);
            await _db.Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS idx_robot_events_type_ts ON replay.robot_events(type, timestamp)", stoppingToken);
            await _db.Database.ExecuteSqlRawAsync("SELECT create_hypertable('replay.robot_events','timestamp', chunk_time_interval => INTERVAL '1 day', if_not_exists => true)", stoppingToken);
            await _db.Database.ExecuteSqlRawAsync("ALTER TABLE replay.robot_events SET (timescaledb.compress)", stoppingToken);
            await _db.Database.ExecuteSqlRawAsync("SELECT add_compression_policy('replay.robot_events', INTERVAL '7 days')", stoppingToken);
            await _db.Database.ExecuteSqlRawAsync("SELECT add_retention_policy('replay.robot_events', INTERVAL '90 days')", stoppingToken);
            await _db.Database.ExecuteSqlRawAsync(@"
                CREATE MATERIALIZED VIEW IF NOT EXISTS replay.cagg_battery_1min 
                WITH (timescaledb.continuous) AS
                SELECT robot_id, time_bucket('1 minute', timestamp) AS ts,
                       avg((payload->>'BatteryPct')::double precision) AS battery_pct,
                       avg((payload->>'Voltage')::double precision) AS voltage
                FROM replay.robot_events
                WHERE type = 'telemetry.battery'
                GROUP BY robot_id, ts
            ", stoppingToken);
            await _db.Database.ExecuteSqlRawAsync(@"
                SELECT add_continuous_aggregate_policy('replay.cagg_battery_1min',
                    start_offset => INTERVAL '90 days',
                    end_offset => INTERVAL '1 hour',
                    schedule_interval => INTERVAL '5 minutes')
            ", stoppingToken);
            await _db.Database.ExecuteSqlRawAsync(@"
                CREATE MATERIALIZED VIEW IF NOT EXISTS replay.cagg_motion_500ms 
                WITH (timescaledb.continuous) AS
                SELECT robot_id, time_bucket('500 milliseconds', timestamp) AS ts,
                       avg((payload->>'CurrentLinearVel')::double precision) AS current_linear_vel,
                       avg((payload->>'TargetLinearVel')::double precision) AS target_linear_vel
                FROM replay.robot_events
                WHERE type = 'telemetry.motion'
                GROUP BY robot_id, ts
            ", stoppingToken);
            await _db.Database.ExecuteSqlRawAsync(@"
                SELECT add_continuous_aggregate_policy('replay.cagg_motion_500ms',
                    start_offset => INTERVAL '7 days',
                    end_offset => INTERVAL '30 minutes',
                    schedule_interval => INTERVAL '2 minutes')
            ", stoppingToken);
            _logger.LogInformation("TimescaleDB initialization completed.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning("TimescaleDB initialization skipped or failed: {Message}", ex.Message);
        }
    }
}
