using BackendV3.Modules.Maps.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BackendV3.Modules.Maps.Persistence;

public sealed class MapNodeEntityConfig : IEntityTypeConfiguration<MapNode>
{
    public void Configure(EntityTypeBuilder<MapNode> builder)
    {
        builder.ToTable("map_nodes", MapsDbSchema.Name);
        builder.HasKey(x => new { x.MapVersionId, x.NodeId });
        builder.Property(x => x.Label).IsRequired();
        builder.Property(x => x.Location).HasColumnType("geometry(Point, 0)");
        builder.HasIndex(x => x.MapVersionId);
        builder.HasIndex(x => x.Location).HasMethod("gist");
    }
}

