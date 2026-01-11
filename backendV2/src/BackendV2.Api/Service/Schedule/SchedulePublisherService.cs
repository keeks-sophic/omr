using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BackendV2.Api.Contracts.Traffic;
using BackendV2.Api.Dto.Traffic;
using BackendV2.Api.Infrastructure.Persistence;
using BackendV2.Api.Service.Traffic;
using BackendV2.Api.Service.Tasks;
using Microsoft.EntityFrameworkCore;

namespace BackendV2.Api.Service.Schedule;

public class SchedulePublisherService
{
    private readonly AppDbContext _db;
    private readonly TrafficControlService _traffic;
    private readonly NatsPublisherStub _nats;
    public SchedulePublisherService(AppDbContext db, TrafficControlService traffic, NatsPublisherStub nats)
    {
        _db = db;
        _traffic = traffic;
        _nats = nats;
    }

    public async Task PublishSchedulesAsync()
    {
        var summaries = await _traffic.ComputeScheduleSummariesAsync();
        var now = DateTimeOffset.UtcNow;
        foreach (var s in summaries)
        {
            var session = await _db.RobotSessions.AsNoTracking().FirstOrDefaultAsync(x => x.RobotId == s.RobotId);
            if (session == null || !session.Connected) continue;
            var schedule = BuildSchedule(s, now, session);
            await _nats.PublishTrafficScheduleAsync(s.RobotId, schedule);
        }
    }

    private static TrafficSchedule BuildSchedule(RobotScheduleSummaryDto s, DateTimeOffset now, BackendV2.Api.Model.Core.RobotSession session)
    {
        var horizonMs = 2000;
        var target = s.TargetLinearVel;
        double maxVel = target;
        try
        {
            if (!string.IsNullOrWhiteSpace(session.MotionLimitsJson))
            {
                var limits = System.Text.Json.JsonSerializer.Deserialize<BackendV2.Api.Dto.Config.MotionLimitsDto>(session.MotionLimitsJson);
                if (limits != null && limits.MaxLinearVel > 0) maxVel = Math.Min(target, limits.MaxLinearVel);
            }
        }
        catch { }
        var startVel = s.HeadwaySeconds > 0.5 ? 0.0 : maxVel;
        var points = new List<SchedulePoint>
        {
            new SchedulePoint { TMs = 0, TargetVel = startVel },
            new SchedulePoint { TMs = 500, TargetVel = maxVel },
            new SchedulePoint { TMs = 1500, TargetVel = maxVel }
        };
        return new TrafficSchedule
        {
            ScheduleId = Guid.NewGuid().ToString("N"),
            GeneratedAt = now,
            HorizonMs = horizonMs,
            Points = points.ToArray()
        };
    }
}
