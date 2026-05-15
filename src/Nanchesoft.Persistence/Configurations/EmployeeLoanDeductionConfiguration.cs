using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class EmployeeLoanDeductionConfiguration : IEntityTypeConfiguration<EmployeeLoanDeduction>
{
    public void Configure(EntityTypeBuilder<EmployeeLoanDeduction> builder)
    {
        builder.ToTable("employee_loan_deductions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status).HasMaxLength(30);
        builder.Property(x => x.Notes).HasMaxLength(600);
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.PrincipalApplied).HasPrecision(18, 2);
        builder.Property(x => x.InterestApplied).HasPrecision(18, 2);
        builder.Property(x => x.RemainingBalance).HasPrecision(18, 2);

        builder.HasIndex(x => new { x.EmployeeLoanId, x.InstallmentNumber }).IsUnique();

        builder.HasOne(x => x.EmployeeLoan).WithMany().HasForeignKey(x => x.EmployeeLoanId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PayrollPeriod).WithMany().HasForeignKey(x => x.PayrollPeriodId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PayrollRun).WithMany().HasForeignKey(x => x.PayrollRunId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PayrollRunLine).WithMany().HasForeignKey(x => x.PayrollRunLineId).OnDelete(DeleteBehavior.Restrict);
    }
}
