using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class SilvaSoftSyncLogConfiguration : IEntityTypeConfiguration<SilvaSoftSyncLog>
{
    public void Configure(EntityTypeBuilder<SilvaSoftSyncLog> builder)
    {
        builder.ToTable("silvasoft_sync_logs");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Operation).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(30).IsRequired();
        builder.Property(x => x.ErrorMessage).HasMaxLength(2000);
        builder.Property(x => x.TriggeredBy).HasMaxLength(120);

        builder.HasIndex(x => new { x.CompanyId, x.StartedAt });
    }
}
