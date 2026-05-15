using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class NavigationItemConfiguration : IEntityTypeConfiguration<NavigationItem>
{
    public void Configure(EntityTypeBuilder<NavigationItem> builder)
    {
        builder.ToTable("navigation_items");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Module).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Code).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Icon).HasMaxLength(100);
        builder.Property(x => x.Route).HasMaxLength(200).IsRequired();
        builder.Property(x => x.RequiredPermission).HasMaxLength(200);

        builder.HasIndex(x => x.Code).IsUnique();
    }
}