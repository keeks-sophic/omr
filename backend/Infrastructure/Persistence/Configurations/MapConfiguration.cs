using Backend.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Persistence.Configurations;

public class MapConfiguration : IEntityTypeConfiguration<Maps>
{
    public void Configure(EntityTypeBuilder<Maps> builder)
    {
        builder.ToTable("maps");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedOnAdd();
        builder.Property(m => m.Name).HasMaxLength(128).IsRequired();

        builder.HasMany(m => m.Nodes).WithOne().HasForeignKey(n => n.MapId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(m => m.Paths).WithOne().HasForeignKey(p => p.MapId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(m => m.Points).WithOne().HasForeignKey(p => p.MapId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(m => m.Qrs).WithOne().HasForeignKey(q => q.MapId).OnDelete(DeleteBehavior.Cascade);
    }
}
