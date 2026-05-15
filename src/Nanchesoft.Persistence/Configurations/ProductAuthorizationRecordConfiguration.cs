using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class ProductAuthorizationRecordConfiguration : IEntityTypeConfiguration<ProductAuthorizationRecord>
{
    public void Configure(EntityTypeBuilder<ProductAuthorizationRecord> builder)
    {
        builder.ToTable("product_authorization_records");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AuthorizationCode).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(30).IsRequired();
        builder.Property(x => x.AuthorizedBy).HasMaxLength(120);
        builder.Property(x => x.RejectionReason).HasMaxLength(600);
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.HasIndex(x => new { x.CompanyId, x.AuthorizationCode }).IsUnique();
        builder.HasIndex(x => x.FinishedProductId);
    }
}
