using Backend.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Persistence.Configurations;

public class QrConfiguration : IEntityTypeConfiguration<Qrs>
{
    public void Configure(EntityTypeBuilder<Qrs> builder)
    {
        builder.ToTable("qrs");
        builder.HasKey(q => q.Id);
        builder.Property(q => q.Id).ValueGeneratedOnAdd();

        builder.Property(q => q.MapId).IsRequired();
        builder.HasIndex(q => q.MapId);
        builder.Property(q => q.PathId).IsRequired();
        builder.HasIndex(q => q.PathId);

        builder.Property(q => q.Data).HasMaxLength(256).IsRequired();

        builder.Property(q => q.Location)
            .HasColumnType("geometry (Point, 0)");

        builder.Property(q => q.OffsetStart);

        builder.HasOne<Paths>().WithMany().HasForeignKey(q => q.PathId).OnDelete(DeleteBehavior.Cascade);
    }
}
