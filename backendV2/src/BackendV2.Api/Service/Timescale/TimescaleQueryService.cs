using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BackendV2.Api.Infrastructure.Persistence;
using Npgsql;

namespace BackendV2.Api.Service.Timescale;

public class TimescaleQueryService
{
    private readonly AppDbContext _db;
    public TimescaleQueryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<TimeSeriesPointDouble>> GetBatteryAsync(string robotId, DateTimeOffset from, DateTimeOffset to, int bucketMs)
    {
        var sql = $@"
            SELECT time_bucket(INTERVAL '{bucketMs} milliseconds', timestamp) AS ts,
                   avg((payload->>'BatteryPct')::double precision) AS v1,
                   avg((payload->>'Voltage')::double precision) AS v2
            FROM replay.robot_events
            WHERE robot_id = @robotId AND type = 'telemetry.battery' AND timestamp BETWEEN @from AND @to
            GROUP BY ts
            ORDER BY ts";
        return await QueryTwoAsync(sql, robotId, from, to);
    }

    public async Task<List<TimeSeriesPointDouble>> GetMotionAsync(string robotId, DateTimeOffset from, DateTimeOffset to, int bucketMs)
    {
        var sql = $@"
            SELECT time_bucket(INTERVAL '{bucketMs} milliseconds', timestamp) AS ts,
                   avg((payload->>'CurrentLinearVel')::double precision) AS v1,
                   avg((payload->>'TargetLinearVel')::double precision) AS v2
            FROM replay.robot_events
            WHERE robot_id = @robotId AND type = 'telemetry.motion' AND timestamp BETWEEN @from AND @to
            GROUP BY ts
            ORDER BY ts";
        return await QueryTwoAsync(sql, robotId, from, to);
    }

    public async Task<List<TimeSeriesPointTriple>> GetPoseAsync(string robotId, DateTimeOffset from, DateTimeOffset to, int bucketMs)
    {
        var sql = $@"
            SELECT time_bucket(INTERVAL '{bucketMs} milliseconds', timestamp) AS ts,
                   avg((payload->>'X')::double precision) AS v1,
                   avg((payload->>'Y')::double precision) AS v2,
                   avg((payload->>'Heading')::double precision) AS v3
            FROM replay.robot_events
            WHERE robot_id = @robotId AND type = 'telemetry.pose' AND timestamp BETWEEN @from AND @to
            GROUP BY ts
            ORDER BY ts";
        return await QueryTripleAsync(sql, robotId, from, to);
    }

    public async Task<List<RouteProgressPoint>> GetRouteProgressAsync(string routeId, DateTimeOffset? from, DateTimeOffset? to, int limit)
    {
        var sql = @"
            SELECT timestamp AS ts,
                   (payload->>'SegmentIndex')::integer AS segment_index,
                   (payload->>'DistanceAlong')::double precision AS distance_along,
                   (payload->>'Eta')::timestamptz AS eta,
                   (payload->>'RouteId') AS route_id,
                   robot_id
            FROM replay.robot_events
            WHERE type = 'route.progress' AND payload->>'RouteId' = @routeId
              AND (@from IS NULL OR timestamp >= @from)
              AND (@to IS NULL OR timestamp <= @to)
            ORDER BY ts DESC
            LIMIT @limit";
        var list = new List<RouteProgressPoint>();
        await using var conn = (NpgsqlConnection)_db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open) await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("routeId", routeId);
        cmd.Parameters.AddWithValue("from", (object?)from ?? DBNull.Value);
        cmd.Parameters.AddWithValue("to", (object?)to ?? DBNull.Value);
        cmd.Parameters.AddWithValue("limit", limit);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new RouteProgressPoint
            {
                Ts = reader.GetFieldValue<DateTimeOffset>(0),
                SegmentIndex = reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1),
                DistanceAlong = reader.IsDBNull(2) ? (double?)null : reader.GetDouble(2),
                Eta = reader.IsDBNull(3) ? (DateTimeOffset?)null : reader.GetFieldValue<DateTimeOffset>(3),
                RouteId = reader.GetString(4),
                RobotId = reader.GetString(5)
            });
        }
        return list;
    }

    private async Task<List<TimeSeriesPointDouble>> QueryTwoAsync(string sql, string robotId, DateTimeOffset from, DateTimeOffset to)
    {
        var list = new List<TimeSeriesPointDouble>();
        await using var conn = (NpgsqlConnection)_db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open) await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("robotId", robotId);
        cmd.Parameters.AddWithValue("from", from);
        cmd.Parameters.AddWithValue("to", to);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new TimeSeriesPointDouble
            {
                Ts = reader.GetFieldValue<DateTimeOffset>(0),
                V1 = reader.IsDBNull(1) ? (double?)null : reader.GetDouble(1),
                V2 = reader.IsDBNull(2) ? (double?)null : reader.GetDouble(2)
            });
        }
        return list;
    }

    private async Task<List<TimeSeriesPointTriple>> QueryTripleAsync(string sql, string robotId, DateTimeOffset from, DateTimeOffset to)
    {
        var list = new List<TimeSeriesPointTriple>();
        await using var conn = (NpgsqlConnection)_db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open) await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("robotId", robotId);
        cmd.Parameters.AddWithValue("from", from);
        cmd.Parameters.AddWithValue("to", to);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new TimeSeriesPointTriple
            {
                Ts = reader.GetFieldValue<DateTimeOffset>(0),
                V1 = reader.IsDBNull(1) ? (double?)null : reader.GetDouble(1),
                V2 = reader.IsDBNull(2) ? (double?)null : reader.GetDouble(2),
                V3 = reader.IsDBNull(3) ? (double?)null : reader.GetDouble(3)
            });
        }
        return list;
    }
}

public class TimeSeriesPointDouble
{
    public DateTimeOffset Ts { get; set; }
    public double? V1 { get; set; }
    public double? V2 { get; set; }
}

public class TimeSeriesPointTriple
{
    public DateTimeOffset Ts { get; set; }
    public double? V1 { get; set; }
    public double? V2 { get; set; }
    public double? V3 { get; set; }
}

public class RouteProgressPoint
{
    public DateTimeOffset Ts { get; set; }
    public int? SegmentIndex { get; set; }
    public double? DistanceAlong { get; set; }
    public DateTimeOffset? Eta { get; set; }
    public string RouteId { get; set; } = string.Empty;
    public string RobotId { get; set; } = string.Empty;
}
