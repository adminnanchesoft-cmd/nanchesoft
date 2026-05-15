using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class PayrollRunLineDetailConfiguration : IEntityTypeConfiguration<PayrollRunLineDetail>
{
    public void Configure(EntityTypeBuilder<PayrollRunLineDetail> builder)
    {
        builder.ToTable("payroll_run_line_details");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ConceptCode).HasMaxLength(32).IsRequired();
        builder.Property(x => x.ConceptName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.ConceptType).HasMaxLength(32).IsRequired();
        builder.Property(x => x.SatCode).HasMaxLength(32);
        builder.Property(x => x.TaxableType).HasMaxLength(32);
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(2048);

        builder.HasIndex(x => new { x.PayrollRunId, x.EmployeeId, x.SortOrder });
        builder.HasIndex(x => new { x.PayrollRunLineId, x.PayrollConceptId, x.SortOrder }).IsUnique();
    }
}
