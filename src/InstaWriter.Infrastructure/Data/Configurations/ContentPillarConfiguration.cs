using InstaWriter.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstaWriter.Infrastructure.Data.Configurations;

public class ContentPillarConfiguration : IEntityTypeConfiguration<ContentPillar>
{
    public void Configure(EntityTypeBuilder<ContentPillar> builder)
    {
        builder.ToTable("ContentPillars");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Description).HasMaxLength(2000);

        builder.Property(x => x.CreatedAt).IsRequired();
    }
}
