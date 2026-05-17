using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations.Production;

public sealed class QualityControlRecordConfiguration : IEntityTypeConfiguration<QualityControlRecord>
{
    public void Configure(EntityTypeBuilder<QualityControlRecord> builder)
    {
        builder.ToTable("quality_control_records");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Folio).HasMaxLength(20).IsRequired();
        builder.Property(x => x.InspectorName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Result).HasMaxLength(20);
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.Property(x => x.ClosedBy).HasMaxLength(120);

        builder.HasIndex(x => new { x.TenantId, x.CompanyId, x.Folio }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.CompanyId, x.Status });
        builder.HasIndex(x => new { x.TenantId, x.ProductionOrderId });

        builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductionOrder).WithMany().HasForeignKey(x => x.ProductionOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Defects).WithOne(x => x.QualityControlRecord).HasForeignKey(x => x.QualityControlRecordId).OnDelete(DeleteBehavior.Cascade);
    }
}
