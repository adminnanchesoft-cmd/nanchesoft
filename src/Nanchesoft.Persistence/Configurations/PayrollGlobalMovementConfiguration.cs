using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class PayrollGlobalMovementConfiguration : IEntityTypeConfiguration<PayrollGlobalMovement>
{
    public void Configure(EntityTypeBuilder<PayrollGlobalMovement> builder)
    {
        builder.ToTable("payroll_global_movements");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.BatchCode).HasMaxLength(40);
        builder.Property(x => x.BatchName).HasMaxLength(160);
        builder.Property(x => x.MovementType).HasMaxLength(30);
        builder.Property(x => x.CalculationMode).HasMaxLength(40);
        builder.Property(x => x.Status).HasMaxLength(30);
        builder.Property(x => x.ControlNumber).HasMaxLength(60);
        builder.Property(x => x.AppliedBy).HasMaxLength(160);
        builder.Property(x => x.Notes).HasMaxLength(600);

        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.Quantity).HasPrecision(18, 4);
        builder.Property(x => x.Percentage).HasPrecision(18, 4);
        builder.Property(x => x.MaxAmount).HasPrecision(18, 2);
        builder.Property(x => x.AccumulatedAmount).HasPrecision(18, 2);
        builder.Property(x => x.MinSalary).HasPrecision(18, 2);
        builder.Property(x => x.MaxSalary).HasPrecision(18, 2);

        builder.Property(x => x.FilterDepartmentIds).HasMaxLength(4000);
        builder.Property(x => x.FilterPositionIds).HasMaxLength(4000);
        builder.Property(x => x.FilterBranchIds).HasMaxLength(4000);
        builder.Property(x => x.FilterEmployerRegistrationIds).HasMaxLength(4000);
        builder.Property(x => x.FilterWorkShiftIds).HasMaxLength(4000);
        builder.Property(x => x.FilterEmployeeIds).HasMaxLength(4000);
        builder.Property(x => x.ExcludeEmployeeIds).HasMaxLength(4000);

        builder.HasIndex(x => new { x.CompanyId, x.BatchCode }).IsUnique();

        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PayrollConcept).WithMany().HasForeignKey(x => x.PayrollConceptId).OnDelete(DeleteBehavior.Restrict);
    }
}
