using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class ProductionScheduleConfiguration : IEntityTypeConfiguration<ProductionSchedule>
{
    public void Configure(EntityTypeBuilder<ProductionSchedule> builder)
    {
        builder.ToTable("production_schedules");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.WeekCode).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(20).IsRequired();
        builder.Property(x => x.LoadPercentage).HasPrecision(5, 2);
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.Property(x => x.LockedBy).HasMaxLength(120);
        builder.Property(x => x.ClosedBy).HasMaxLength(120);

        builder.HasIndex(x => new { x.TenantId, x.CompanyId, x.BranchId, x.WeekCode }).IsUnique();

        builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Lines).WithOne(x => x.ProductionSchedule).HasForeignKey(x => x.ProductionScheduleId).OnDelete(DeleteBehavior.Cascade);
    }
}
