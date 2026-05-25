using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class UserThemePreferenceConfiguration : IEntityTypeConfiguration<UserThemePreference>
{
    public void Configure(EntityTypeBuilder<UserThemePreference> builder)
    {
        builder.ToTable("user_theme_preferences");

        builder.HasKey(x => x.UserId);

        builder.Property(x => x.ThemeName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.AccentColor).HasMaxLength(20).IsRequired();
        builder.Property(x => x.SecondaryColor).HasMaxLength(20).IsRequired();
        builder.Property(x => x.BackgroundColor).HasMaxLength(20).IsRequired();
    }
}
