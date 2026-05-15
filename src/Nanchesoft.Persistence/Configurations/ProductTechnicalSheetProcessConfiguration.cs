using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class ProductTechnicalSheetProcessConfiguration : IEntityTypeConfiguration<ProductTechnicalSheetProcess>
{
    public void Configure(EntityTypeBuilder<ProductTechnicalSheetProcess> builder)
    {
        builder.ToTable("product_technical_sheet_processes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProcessCode).HasMaxLength(40).IsRequired();
        builder.Property(x => x.ProcessName).HasMaxLength(160).IsRequired();
        builder.Property(x => x.WorkstationCode).HasMaxLength(40);
        builder.Property(x => x.DeliverToWarehouseCode).HasMaxLength(40);
        builder.Property(x => x.Notes).HasMaxLength(1200);
        builder.HasIndex(x => new { x.ProductTechnicalSheetId, x.SortOrder });
    }
}
