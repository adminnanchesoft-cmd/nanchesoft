using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations.Production;

public sealed class QualityDefectConfiguration : IEntityTypeConfiguration<QualityDefect>
{
    public void Configure(EntityTypeBuilder<QualityDefect> builder)
    {
        builder.ToTable("quality_defects");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DefectCode).HasMaxLength(30).IsRequired();
        builder.Property(x => x.DefectDescription).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Severity).HasMaxLength(20).IsRequired();
        builder.Property(x => x.ResolutionNotes).HasMaxLength(1000);

        builder.HasIndex(x => x.QualityControlRecordId);
        builder.HasIndex(x => new { x.QualityControlRecordId, x.DefectCode }).IsUnique();
    }
}
