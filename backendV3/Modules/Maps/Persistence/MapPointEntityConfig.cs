using BackendV3.Modules.Maps.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BackendV3.Modules.Maps.Persistence;

public sealed class MapPointEntityConfig : IEntityTypeConfiguration<MapPoint>
{
    public void Configure(EntityTypeBuilder<MapPoint> builder)
    {
        builder.ToTable("map_points", MapsDbSchema.Name);
        builder.HasKey(x => x.PointId);
        builder.Property(x => x.Type).IsRequired();
        builder.Property(x => x.Label).IsRequired();
        builder.Property(x => x.Location).HasColumnType("geometry(Point, 0)");
        builder.HasIndex(x => x.MapVersionId);
        builder.HasIndex(x => x.Location).HasMethod("gist");
    }
}

