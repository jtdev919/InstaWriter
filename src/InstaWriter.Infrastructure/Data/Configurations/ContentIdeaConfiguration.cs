using InstaWriter.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstaWriter.Infrastructure.Data.Configurations;

public class ContentIdeaConfiguration : IEntityTypeConfiguration<ContentIdea>
{
    public void Configure(EntityTypeBuilder<ContentIdea> builder)
    {
        builder.ToTable("ContentIdeas");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Summary).HasMaxLength(1000);
        builder.Property(x => x.SourceType).HasMaxLength(50);
        builder.Property(x => x.PillarName).HasMaxLength(100);

        builder.Property(x => x.RiskLevel)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.CreatedAt).IsRequired();
    }
}
