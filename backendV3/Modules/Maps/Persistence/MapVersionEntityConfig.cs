using BackendV3.Modules.Maps.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BackendV3.Modules.Maps.Persistence;

public sealed class MapVersionEntityConfig : IEntityTypeConfiguration<MapVersion>
{
    public void Configure(EntityTypeBuilder<MapVersion> builder)
    {
        builder.ToTable("map_versions", MapsDbSchema.Name);
        builder.HasKey(x => x.MapVersionId);
        builder.Property(x => x.MapId).IsRequired();
        builder.Property(x => x.Status).IsRequired();
        builder.HasIndex(x => new { x.MapId, x.Version }).IsUnique();
        builder.HasIndex(x => new { x.MapId, x.Status });
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => x.PublishedAt);
        builder.HasIndex(x => x.DerivedFromMapVersionId);
        builder.HasIndex(x => x.PublishedBy);
        builder.HasIndex(x => x.MapId).IsUnique().HasFilter("\"Status\" = 'PUBLISHED'");
    }
}
