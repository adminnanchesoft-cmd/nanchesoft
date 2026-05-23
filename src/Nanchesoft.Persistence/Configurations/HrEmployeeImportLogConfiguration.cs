using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class HrEmployeeImportLogConfiguration : IEntityTypeConfiguration<HrEmployeeImportLog>
{
    public void Configure(EntityTypeBuilder<HrEmployeeImportLog> builder)
    {
        builder.ToTable("hr_employee_import_logs");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FileName).HasMaxLength(260).IsRequired();
        builder.Property(x => x.ConflictMode).HasMaxLength(20).IsRequired();
        builder.Property(x => x.ExecutedBy).HasMaxLength(180).IsRequired();

        builder.Property(x => x.Errors).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.Duplicates).HasColumnType("jsonb").IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.ExecutedAt });
        builder.HasIndex(x => new { x.CompanyId, x.ExecutedAt });
    }
}
