using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class EmployeeLoanConfiguration : IEntityTypeConfiguration<EmployeeLoan>
{
    public void Configure(EntityTypeBuilder<EmployeeLoan> builder)
    {
        builder.ToTable("employee_loans");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.LoanNumber).HasMaxLength(40);
        builder.Property(x => x.Status).HasMaxLength(30);
        builder.Property(x => x.Notes).HasMaxLength(600);
        builder.Property(x => x.PrincipalAmount).HasPrecision(18, 2);
        builder.Property(x => x.BalanceAmount).HasPrecision(18, 2);
        builder.Property(x => x.InstallmentAmount).HasPrecision(18, 2);

        builder.HasIndex(x => new { x.CompanyId, x.LoanNumber }).IsUnique();

        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PayrollConcept).WithMany().HasForeignKey(x => x.PayrollConceptId).OnDelete(DeleteBehavior.Restrict);
    }
}
