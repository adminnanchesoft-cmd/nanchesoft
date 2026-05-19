using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class InternalTransferConfiguration : IEntityTypeConfiguration<InternalTransfer>
{
    public void Configure(EntityTypeBuilder<InternalTransfer> builder)
    {
        builder.ToTable("internal_transfers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SourceAccountType).HasMaxLength(20).IsRequired();
        builder.Property(x => x.DestinationAccountType).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Reference).HasMaxLength(160);
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.Property(x => x.Status).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.HasIndex(x => new { x.CompanyId, x.TransferDate });
        builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
    }
}
