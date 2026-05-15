using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;
namespace Nanchesoft.Persistence.Configurations;
public sealed class PhysicalCountConfiguration : IEntityTypeConfiguration<PhysicalCount>
{
    public void Configure(EntityTypeBuilder<PhysicalCount> builder)
    {
        builder.ToTable("physical_counts");
        builder.HasKey(x => x.Id);
        builder.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.PhysicalCountId).OnDelete(DeleteBehavior.Cascade);
    }
}
