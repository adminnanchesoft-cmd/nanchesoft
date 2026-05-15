using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class ServiceNoteConfiguration : IEntityTypeConfiguration<ServiceNote>
{
    public void Configure(EntityTypeBuilder<ServiceNote> builder)
    {
        builder.ToTable("service_notes");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Folio).HasMaxLength(40).IsRequired();
        builder.Property(x => x.CustomerNameSnapshot).HasMaxLength(160).IsRequired();
        builder.Property(x => x.ServiceCodeSnapshot).HasMaxLength(40);
        builder.Property(x => x.ServiceNameSnapshot).HasMaxLength(160);
        builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
        builder.Property(x => x.StartTimeText).HasMaxLength(20);
        builder.Property(x => x.EndTimeText).HasMaxLength(20);
        builder.Property(x => x.HoursWorked).HasPrecision(18, 2);
        builder.Property(x => x.HourlyRate).HasPrecision(18, 2);
        builder.Property(x => x.Subtotal).HasPrecision(18, 2);
        builder.Property(x => x.Total).HasPrecision(18, 2);
        builder.Property(x => x.PaymentStatus).HasMaxLength(30).IsRequired();
        builder.Property(x => x.PaymentMethod).HasMaxLength(30).IsRequired();
        builder.Property(x => x.PaymentDestination).HasMaxLength(220);
        builder.Property(x => x.PaymentReference).HasMaxLength(160);
        builder.Property(x => x.Notes).HasMaxLength(600);

        builder.HasIndex(x => new { x.CompanyId, x.Folio }).IsUnique();
        builder.HasIndex(x => new { x.CompanyId, x.NoteDate });

        builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ServiceCatalogItem).WithMany().HasForeignKey(x => x.ServiceCatalogItemId).OnDelete(DeleteBehavior.Restrict);
    }
}
