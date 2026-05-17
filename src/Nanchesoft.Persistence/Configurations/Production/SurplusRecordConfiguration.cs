using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class SurplusRecordConfiguration : IEntityTypeConfiguration<SurplusRecord>
{
    public void Configure(EntityTypeBuilder<SurplusRecord> builder)
    {
        builder.ToTable("surplus_records");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Disposition).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.Property(x => x.ResolvedBy).HasMaxLength(120);

        builder.HasIndex(x => x.ProductionOrderId);
        builder.HasIndex(x => x.FinishedProductId);

        builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductionOrder).WithMany().HasForeignKey(x => x.ProductionOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.FinishedProduct).WithMany().HasForeignKey(x => x.FinishedProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.SizeRunSize).WithMany().HasForeignKey(x => x.SizeRunSizeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.AssignedOrder).WithMany().HasForeignKey(x => x.AssignedOrderId).OnDelete(DeleteBehavior.Restrict);
    }
}
