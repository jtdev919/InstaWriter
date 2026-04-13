using InstaWriter.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstaWriter.Infrastructure.Data.Configurations;

public class BrandProfileConfiguration : IEntityTypeConfiguration<BrandProfile>
{
    public void Configure(EntityTypeBuilder<BrandProfile> builder)
    {
        builder.ToTable("BrandProfiles");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.VoiceGuide).HasMaxLength(4000);
        builder.Property(x => x.ToneGuide).HasMaxLength(4000);
        builder.Property(x => x.CTAStyle).HasMaxLength(2000);
        builder.Property(x => x.DisclaimerRules).HasMaxLength(4000);
        builder.Property(x => x.DefaultHashtagSets).HasMaxLength(2000);

        builder.Property(x => x.CreatedAt).IsRequired();
    }
}
