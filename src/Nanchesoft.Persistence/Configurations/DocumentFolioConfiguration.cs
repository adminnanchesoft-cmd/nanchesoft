using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class DocumentFolioConfiguration : IEntityTypeConfiguration<DocumentFolio>
{
    public void Configure(EntityTypeBuilder<DocumentFolio> builder)
    {
        builder.ToTable("document_folios");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.DocumentType).HasMaxLength(50).IsRequired();

        builder.HasIndex(x => new { x.CompanyId, x.DocumentType, x.SeriesId }).IsUnique();

        builder.HasOne(x => x.Tenant)
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Company)
            .WithMany()
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Series)
            .WithMany(x => x.DocumentFolios)
            .HasForeignKey(x => x.SeriesId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
