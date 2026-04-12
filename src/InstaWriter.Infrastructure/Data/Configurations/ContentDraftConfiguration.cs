using InstaWriter.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstaWriter.Infrastructure.Data.Configurations;

public class ContentDraftConfiguration : IEntityTypeConfiguration<ContentDraft>
{
    public void Configure(EntityTypeBuilder<ContentDraft> builder)
    {
        builder.ToTable("ContentDrafts");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Caption).IsRequired().HasMaxLength(2200);
        builder.Property(x => x.Script).HasMaxLength(5000);
        builder.Property(x => x.HashtagSet).HasMaxLength(1000);
        builder.Property(x => x.CoverText).HasMaxLength(200);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasOne(x => x.ContentIdea)
            .WithMany()
            .HasForeignKey(x => x.ContentIdeaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
