using BackendV3.Modules.Auth.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BackendV3.Modules.Auth.Persistence;

public sealed class UserRoleEntityConfig : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("user_roles", AuthDbSchema.Name);
        builder.HasKey(x => new { x.UserId, x.RoleId });
        builder.HasIndex(x => x.RoleId);
    }
}
