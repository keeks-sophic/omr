using BackendV3.Modules.Maps.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BackendV3.Modules.Maps.Persistence;

public sealed class QrAnchorEntityConfig : IEntityTypeConfiguration<QrAnchor>
{
    public void Configure(EntityTypeBuilder<QrAnchor> builder)
    {
        builder.ToTable("qr_anchors", MapsDbSchema.Name);
        builder.HasKey(x => new { x.MapVersionId, x.QrId });
        builder.Property(x => x.QrCode).IsRequired();
        builder.Property(x => x.Location).HasColumnType("geometry(Point, 0)");
        builder.HasIndex(x => x.MapVersionId);
        builder.HasIndex(x => x.PathId);
        builder.HasIndex(x => x.Location).HasMethod("gist");
    }
}

