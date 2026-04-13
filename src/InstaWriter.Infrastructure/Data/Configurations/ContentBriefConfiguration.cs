using InstaWriter.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstaWriter.Infrastructure.Data.Configurations;

public class ContentBriefConfiguration : IEntityTypeConfiguration<ContentBrief>
{
    public void Configure(EntityTypeBuilder<ContentBrief> builder)
    {
        builder.ToTable("ContentBriefs");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.TargetFormat)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.Objective).IsRequired().HasMaxLength(2000);
        builder.Property(x => x.Audience).HasMaxLength(1000);
        builder.Property(x => x.HookDirection).HasMaxLength(2000);
        builder.Property(x => x.KeyMessage).IsRequired().HasMaxLength(2000);
        builder.Property(x => x.CTA).HasMaxLength(1000);

        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasOne(x => x.ContentIdea)
            .WithMany()
            .HasForeignKey(x => x.ContentIdeaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
