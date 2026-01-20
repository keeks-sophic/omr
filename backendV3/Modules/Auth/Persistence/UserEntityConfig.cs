using BackendV3.Modules.Auth.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BackendV3.Modules.Auth.Persistence;

public sealed class UserEntityConfig : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", AuthDbSchema.Name);
        builder.HasKey(x => x.UserId);
        builder.Property(x => x.Username).IsRequired();
        builder.Property(x => x.DisplayName).IsRequired();
        builder.Property(x => x.PasswordHash).IsRequired();
        builder.HasIndex(x => x.Username).IsUnique();
    }
}
