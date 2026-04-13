using InstaWriter.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstaWriter.Infrastructure.Data.Configurations;

public class ApprovalConfiguration : IEntityTypeConfiguration<Approval>
{
    public void Configure(EntityTypeBuilder<Approval> builder)
    {
        builder.ToTable("Approvals");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Approver).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Comments).HasMaxLength(4000);

        builder.Property(x => x.Decision)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.Timestamp).IsRequired();

        builder.HasOne(x => x.ContentDraft)
            .WithMany()
            .HasForeignKey(x => x.ContentDraftId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
