using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("companies");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.LegalName).HasMaxLength(250).IsRequired();
        builder.Property(x => x.TaxId).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Timezone).HasMaxLength(100).IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();

        builder.HasOne(x => x.Tenant)
            .WithMany(x => x.Companies)
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}