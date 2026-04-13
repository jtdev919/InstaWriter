using InstaWriter.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstaWriter.Infrastructure.Data.Configurations;

public class ChannelAccountConfiguration : IEntityTypeConfiguration<ChannelAccount>
{
    public void Configure(EntityTypeBuilder<ChannelAccount> builder)
    {
        builder.ToTable("ChannelAccounts");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.AccountName).IsRequired().HasMaxLength(100);
        builder.Property(x => x.ExternalAccountId).HasMaxLength(100);
        builder.Property(x => x.AccessToken).HasMaxLength(500);

        builder.Property(x => x.PlatformType)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.AuthStatus)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.CreatedAt).IsRequired();
    }
}
