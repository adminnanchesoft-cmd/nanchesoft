using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Persistence.Configurations;

public sealed class SubscriptionChargeConfiguration : IEntityTypeConfiguration<SubscriptionCharge>
{
    public void Configure(EntityTypeBuilder<SubscriptionCharge> builder)
    {
        builder.ToTable("subscription_charges", NanchesoftSchemaCatalog.SubscriptionSchema);

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.IsActive).HasColumnName("is_active");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        builder.Property(x => x.TenantId).HasColumnName("tenant_id");
        builder.Property(x => x.PlanId).HasColumnName("plan_id");
        builder.Property(x => x.ChargeMonth).HasColumnName("charge_month").IsRequired();
        builder.Property(x => x.BillingYear).HasColumnName("billing_year");
        builder.Property(x => x.BillingMonth).HasColumnName("billing_month");

        builder.Property(x => x.TenantCodeSnapshot).HasColumnName("tenant_code_snapshot").IsRequired();
        builder.Property(x => x.TenantNameSnapshot).HasColumnName("tenant_name_snapshot").IsRequired();
        builder.Property(x => x.PlanCodeSnapshot).HasColumnName("plan_code_snapshot").IsRequired();
        builder.Property(x => x.PlanNameSnapshot).HasColumnName("plan_name_snapshot").IsRequired();

        builder.Property(x => x.ChargeDate).HasColumnName("charge_date");
        builder.Property(x => x.DueDate).HasColumnName("due_date");

        builder.Property(x => x.PlanPriceMonthly).HasColumnName("plan_price_monthly").HasPrecision(18, 2);
        builder.Property(x => x.DiscountAmount).HasColumnName("discount_amount").HasPrecision(18, 2);
        builder.Property(x => x.SurchargeAmount).HasColumnName("surcharge_amount").HasPrecision(18, 2);
        builder.Property(x => x.TotalAmount).HasColumnName("total_amount").HasPrecision(18, 2);
        builder.Property(x => x.PaidAmount).HasColumnName("paid_amount").HasPrecision(18, 2);
        builder.Property(x => x.CompensationAmount).HasColumnName("compensation_amount").HasPrecision(18, 2);
        builder.Property(x => x.BalanceAmount).HasColumnName("balance_amount").HasPrecision(18, 2);

        builder.Property(x => x.PaidAt).HasColumnName("paid_at");
        builder.Property(x => x.PaymentMethod).HasColumnName("payment_method").IsRequired();
        builder.Property(x => x.Reference).HasColumnName("reference").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").IsRequired();
        builder.Property(x => x.Notes).HasColumnName("notes").IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.BillingYear, x.BillingMonth })
            .IsUnique()
            .HasDatabaseName("ux_subscription_charges_tenant_month");

        builder.HasIndex(x => x.Status)
            .HasDatabaseName("ix_subscription_charges_status");

        builder.HasIndex(x => x.DueDate)
            .HasDatabaseName("ix_subscription_charges_due_date");

        builder.HasOne(x => x.Tenant)
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Plan)
            .WithMany()
            .HasForeignKey(x => x.PlanId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
