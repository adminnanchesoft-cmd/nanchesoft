using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class PieceWorkRateConfiguration : IEntityTypeConfiguration<PieceWorkRate>
{
    public void Configure(EntityTypeBuilder<PieceWorkRate> builder)
    {
        builder.ToTable("piece_work_rates");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PricePerUnit).HasPrecision(12, 4).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(1200);

        builder.HasIndex(x => new { x.CompanyId, x.ProductionPhaseId, x.EffectiveDate });

        builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductionPhase).WithMany().HasForeignKey(x => x.ProductionPhaseId).OnDelete(DeleteBehavior.Restrict);
    }
}
