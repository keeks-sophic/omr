using BackendV3.Modules.Maps.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BackendV3.Modules.Maps.Persistence;

public sealed class MapPathEntityConfig : IEntityTypeConfiguration<MapPath>
{
    public void Configure(EntityTypeBuilder<MapPath> builder)
    {
        builder.ToTable("map_paths", MapsDbSchema.Name);
        builder.HasKey(x => x.PathId);
        builder.Property(x => x.Direction).IsRequired();
        builder.Property(x => x.Location).HasColumnType("geometry(LineString, 0)");
        builder.HasIndex(x => x.MapVersionId);
        builder.HasIndex(x => x.Location).HasMethod("gist");
    }
}

