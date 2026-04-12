using InstaWriter.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstaWriter.Infrastructure.Data.Configurations;

public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("TaskItems");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.RelatedEntityType).HasMaxLength(50);
        builder.Property(x => x.Owner).IsRequired().HasMaxLength(100);
        builder.Property(x => x.TaskType).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Description).HasMaxLength(2000);

        builder.Property(x => x.Priority)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.CreatedAt).IsRequired();
    }
}
