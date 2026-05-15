using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class ReconciliationLineConfiguration : IEntityTypeConfiguration<ReconciliationLine>
{
    public void Configure(EntityTypeBuilder<ReconciliationLine> builder)
    {
        builder.ToTable("reconciliation_lines");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.MovementAmount).HasPrecision(18, 2);
        builder.HasOne(x => x.BankMovement).WithMany().HasForeignKey(x => x.BankMovementId).OnDelete(DeleteBehavior.Restrict);
    }
}
