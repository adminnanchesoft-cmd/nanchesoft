using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class CustomerServiceRateConfiguration : IEntityTypeConfiguration<CustomerServiceRate>
{
    public void Configure(EntityTypeBuilder<CustomerServiceRate> builder)
    {
        builder.ToTable("customer_service_rates");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Rate).HasPrecision(18, 2);
        builder.Property(x => x.Notes).HasMaxLength(600);

        builder.HasIndex(x => new { x.CompanyId, x.CustomerId, x.ServiceCatalogItemId, x.EffectiveFrom }).IsUnique();
        builder.HasIndex(x => new { x.CustomerId, x.ServiceCatalogItemId, x.IsActive });

        builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ServiceCatalogItem).WithMany().HasForeignKey(x => x.ServiceCatalogItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Currency).WithMany().HasForeignKey(x => x.CurrencyId).OnDelete(DeleteBehavior.Restrict);
    }
}
