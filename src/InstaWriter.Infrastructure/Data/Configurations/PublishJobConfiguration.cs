using InstaWriter.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstaWriter.Infrastructure.Data.Configurations;

public class PublishJobConfiguration : IEntityTypeConfiguration<PublishJob>
{
    public void Configure(EntityTypeBuilder<PublishJob> builder)
    {
        builder.ToTable("PublishJobs");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.ExternalContainerId).HasMaxLength(200);
        builder.Property(x => x.ExternalMediaId).HasMaxLength(200);
        builder.Property(x => x.FailureReason).HasMaxLength(2000);

        builder.Property(x => x.PublishMode)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasOne(x => x.ContentDraft)
            .WithMany()
            .HasForeignKey(x => x.ContentDraftId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ChannelAccount)
            .WithMany()
            .HasForeignKey(x => x.ChannelAccountId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
