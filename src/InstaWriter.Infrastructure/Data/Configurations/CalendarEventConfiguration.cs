using InstaWriter.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstaWriter.Infrastructure.Data.Configurations;

public class CalendarEventConfiguration : IEntityTypeConfiguration<CalendarEvent>
{
    public void Configure(EntityTypeBuilder<CalendarEvent> builder)
    {
        builder.ToTable("CalendarEvents");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.ExternalCalendarId).HasMaxLength(500);
        builder.Property(x => x.StartDateTime).IsRequired();
        builder.Property(x => x.EndDateTime).IsRequired();
        builder.Property(x => x.ReminderProfile).HasMaxLength(500);
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasOne(x => x.TaskItem)
            .WithMany()
            .HasForeignKey(x => x.TaskItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
