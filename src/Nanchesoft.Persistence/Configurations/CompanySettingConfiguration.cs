using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class CompanySettingConfiguration : IEntityTypeConfiguration<CompanySetting>
{
    public void Configure(EntityTypeBuilder<CompanySetting> builder)
    {
        builder.ToTable("company_settings");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Timezone).HasMaxLength(100).IsRequired();

        builder.HasIndex(x => x.CompanyId).IsUnique();

        builder.HasOne(x => x.Tenant)
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Company)
            .WithMany()
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Currency)
            .WithMany(x => x.CompanySettings)
            .HasForeignKey(x => x.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.DefaultPurchaseSeries)
            .WithMany()
            .HasForeignKey(x => x.DefaultPurchaseSeriesId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.DefaultSalesSeries)
            .WithMany()
            .HasForeignKey(x => x.DefaultSalesSeriesId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
