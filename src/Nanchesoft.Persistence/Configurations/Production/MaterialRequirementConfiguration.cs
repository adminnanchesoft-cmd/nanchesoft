using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class MaterialRequirementConfiguration : IEntityTypeConfiguration<MaterialRequirement>
{
    public void Configure(EntityTypeBuilder<MaterialRequirement> builder)
    {
        builder.ToTable("material_requirements");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CalculatedBy).HasMaxLength(120);
        builder.Property(x => x.Status).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(2000);

        builder.HasIndex(x => x.ProductionOrderId);
        builder.HasIndex(x => new { x.TenantId, x.CompanyId, x.Status });

        builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductionOrder).WithMany(x => x.MaterialRequirements).HasForeignKey(x => x.ProductionOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Lines).WithOne(x => x.MaterialRequirement).HasForeignKey(x => x.MaterialRequirementId).OnDelete(DeleteBehavior.Cascade);
    }
}
