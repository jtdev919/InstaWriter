using InstaWriter.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstaWriter.Infrastructure.Data.Configurations;

public class CarouselTemplateConfiguration : IEntityTypeConfiguration<CarouselTemplate>
{
    public void Configure(EntityTypeBuilder<CarouselTemplate> builder)
    {
        builder.ToTable("CarouselTemplates");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.TemplateType).IsRequired().HasMaxLength(100);
        builder.Property(x => x.SlideLayoutsCsv).HasMaxLength(1000);

        builder.Property(x => x.CreatedAt).IsRequired();
    }
}
