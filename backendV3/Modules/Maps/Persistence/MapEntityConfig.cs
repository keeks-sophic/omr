using BackendV3.Modules.Maps.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BackendV3.Modules.Maps.Persistence;

public sealed class MapEntityConfig : IEntityTypeConfiguration<Map>
{
    public void Configure(EntityTypeBuilder<Map> builder)
    {
        builder.ToTable("maps", MapsDbSchema.Name);
        builder.HasKey(x => x.MapId);
        builder.Property(x => x.Name).IsRequired();
        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasIndex(x => x.ActivePublishedMapVersionId);
        builder.HasIndex(x => x.ArchivedAt);
    }
}
