using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class PieceWorkRecordConfiguration : IEntityTypeConfiguration<PieceWorkRecord>
{
    public void Configure(EntityTypeBuilder<PieceWorkRecord> builder)
    {
        builder.ToTable("piece_work_records");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.UnitPrice).HasPrecision(12, 4).IsRequired();
        builder.Property(x => x.GrossAmount).HasPrecision(12, 4).IsRequired();
        builder.Property(x => x.QualityDeduction).HasPrecision(12, 4);
        builder.Property(x => x.NetAmount).HasPrecision(12, 4);
        builder.Property(x => x.Status).HasMaxLength(20).IsRequired();
        builder.Property(x => x.ApprovedBy).HasMaxLength(120);
        builder.Property(x => x.ProcessedBy).HasMaxLength(120);
        builder.Property(x => x.Notes).HasMaxLength(2000);

        builder.HasIndex(x => new { x.EmployeeId, x.WorkDate });
        builder.HasIndex(x => x.ProductionOrderId);
        builder.HasIndex(x => x.PayrollPeriodId);
        builder.HasIndex(x => new { x.TenantId, x.CompanyId, x.WorkDate });

        builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductionVoucher).WithMany(x => x.PieceWorkRecords).HasForeignKey(x => x.ProductionVoucherId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductionOrder).WithMany().HasForeignKey(x => x.ProductionOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductionPhase).WithMany().HasForeignKey(x => x.ProductionPhaseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PayrollPeriod).WithMany().HasForeignKey(x => x.PayrollPeriodId).OnDelete(DeleteBehavior.Restrict);
    }
}
