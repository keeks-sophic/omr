using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BackendV2.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BackendV2.Api.Data.Task;

public class TaskRepository
{
    private readonly AppDbContext _db;
    public TaskRepository(AppDbContext db) { _db = db; }
    public Task<List<BackendV2.Api.Model.Task.Task>> ListAsync() => _db.Tasks.ToListAsync();
    public Task<BackendV2.Api.Model.Task.Task?> GetAsync(Guid id) => _db.Tasks.FirstOrDefaultAsync(x => x.TaskId == id);
}
