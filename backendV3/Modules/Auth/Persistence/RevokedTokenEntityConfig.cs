using BackendV3.Modules.Auth.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BackendV3.Modules.Auth.Persistence;

public sealed class RevokedTokenEntityConfig : IEntityTypeConfiguration<RevokedToken>
{
    public void Configure(EntityTypeBuilder<RevokedToken> builder)
    {
        builder.ToTable("revoked_tokens", AuthDbSchema.Name);
        builder.HasKey(x => x.RevocationId);
        builder.Property(x => x.Jti).IsRequired();
        builder.HasIndex(x => x.Jti).IsUnique();
    }
}
