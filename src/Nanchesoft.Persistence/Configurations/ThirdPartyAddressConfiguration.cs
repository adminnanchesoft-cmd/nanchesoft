using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class ThirdPartyAddressConfiguration : IEntityTypeConfiguration<ThirdPartyAddress>
{
    public void Configure(EntityTypeBuilder<ThirdPartyAddress> builder)
    {
        builder.ToTable("third_party_addresses");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ThirdPartyType).HasMaxLength(20).IsRequired();
        builder.Property(x => x.AddressType).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Street).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ExteriorNumber).HasMaxLength(20);
        builder.Property(x => x.InteriorNumber).HasMaxLength(20);
        builder.Property(x => x.Neighborhood).HasMaxLength(120);
        builder.Property(x => x.ZipCode).HasMaxLength(20);
        builder.Property(x => x.Reference).HasMaxLength(200);

        builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Country).WithMany().HasForeignKey(x => x.CountryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.State).WithMany().HasForeignKey(x => x.StateId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.City).WithMany().HasForeignKey(x => x.CityId).OnDelete(DeleteBehavior.Restrict);
    }
}
