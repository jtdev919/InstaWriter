using InstaWriter.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstaWriter.Infrastructure.Data.Configurations;

public class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        builder.ToTable("Assets");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.FileName).IsRequired().HasMaxLength(260);
        builder.Property(x => x.ContentType).IsRequired().HasMaxLength(100);
        builder.Property(x => x.BlobUri).HasMaxLength(2000);
        builder.Property(x => x.ThumbnailUri).HasMaxLength(2000);
        builder.Property(x => x.Owner).HasMaxLength(100);
        builder.Property(x => x.Tags).HasMaxLength(1000);
        builder.Property(x => x.PillarName).HasMaxLength(100);

        builder.Property(x => x.AssetType)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasOne(x => x.ContentIdea)
            .WithMany()
            .HasForeignKey(x => x.ContentIdeaId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.ContentDraft)
            .WithMany()
            .HasForeignKey(x => x.ContentDraftId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
