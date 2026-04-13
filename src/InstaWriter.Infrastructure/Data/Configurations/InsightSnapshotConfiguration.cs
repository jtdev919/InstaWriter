using InstaWriter.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstaWriter.Infrastructure.Data.Configurations;

public class InsightSnapshotConfiguration : IEntityTypeConfiguration<InsightSnapshot>
{
    public void Configure(EntityTypeBuilder<InsightSnapshot> builder)
    {
        builder.ToTable("InsightSnapshots");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.SnapshotDate).IsRequired();

        builder.HasOne(x => x.PublishJob)
            .WithMany()
            .HasForeignKey(x => x.PublishJobId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
