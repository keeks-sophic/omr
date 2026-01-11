using System;
using System.Threading.Tasks;
using BackendV2.Api.Infrastructure.Persistence;
using BackendV2.Api.Model.Ops;
using Microsoft.EntityFrameworkCore;

namespace BackendV2.Api.Data.Ops;

public class AuditRepository
{
    private readonly AppDbContext _db;
    public AuditRepository(AppDbContext db) { _db = db; }
    public global::System.Threading.Tasks.Task<BackendV2.Api.Model.Ops.AuditEvent?> GetAsync(Guid id) => _db.AuditEvents.FirstOrDefaultAsync(x => x.AuditEventId == id);
    public async global::System.Threading.Tasks.Task WriteAsync(Guid? actorUserId, string action, string targetId, string outcome, string detailsJson = "{}", string? targetType = null)
    {
        await _db.AuditEvents.AddAsync(new AuditEvent
        {
            AuditEventId = Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow,
            ActorUserId = actorUserId,
            Action = action,
            TargetType = targetType ?? "generic",
            TargetId = targetId,
            Outcome = outcome,
            DetailsJson = detailsJson
        });
        await _db.SaveChangesAsync();
    }
}
