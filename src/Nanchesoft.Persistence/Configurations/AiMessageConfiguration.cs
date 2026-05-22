using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class AiMessageConfiguration : IEntityTypeConfiguration<AiMessage>
{
    public void Configure(EntityTypeBuilder<AiMessage> builder)
    {
        builder.ToTable("ai_messages");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Role).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Content).IsRequired();
        builder.Property(x => x.Intent).HasMaxLength(80);
        builder.Property(x => x.Endpoint).HasMaxLength(200);
        builder.Property(x => x.DataJson).HasColumnType("jsonb");

        builder.HasIndex(x => new { x.ConversationId, x.SequenceNumber });
        builder.HasIndex(x => new { x.TenantId, x.ConversationId, x.CreatedAt });

        builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}
