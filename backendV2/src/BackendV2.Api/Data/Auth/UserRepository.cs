using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BackendV2.Api.Infrastructure.Persistence;
using BackendV2.Api.Model.Auth;
using Microsoft.EntityFrameworkCore;

namespace BackendV2.Api.Data.Auth;

public class UserRepository
{
    private readonly AppDbContext _db;
    public UserRepository(AppDbContext db) { _db = db; }
    public Task<List<User>> ListAsync() => _db.Users.ToListAsync();
    public Task<User?> GetAsync(Guid id) => _db.Users.FirstOrDefaultAsync(x => x.UserId == id);
}
