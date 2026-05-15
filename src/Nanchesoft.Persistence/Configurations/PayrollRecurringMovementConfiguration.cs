using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class PayrollRecurringMovementConfiguration : IEntityTypeConfiguration<PayrollRecurringMovement>
{
    public void Configure(EntityTypeBuilder<PayrollRecurringMovement> builder)
    {
        builder.ToTable("payroll_recurring_movements");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.MovementCode).HasMaxLength(40);
        builder.Property(x => x.MovementName).HasMaxLength(160);
        builder.Property(x => x.MovementType).HasMaxLength(30);
        builder.Property(x => x.CalculationMode).HasMaxLength(40);
        builder.Property(x => x.Status).HasMaxLength(30);
        builder.Property(x => x.Notes).HasMaxLength(600);
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.Quantity).HasPrecision(18, 4);
        builder.Property(x => x.Percentage).HasPrecision(18, 4);

        builder.HasIndex(x => new { x.CompanyId, x.EmployeeId, x.MovementCode }).IsUnique();

        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PayrollConcept).WithMany().HasForeignKey(x => x.PayrollConceptId).OnDelete(DeleteBehavior.Restrict);
    }
}
