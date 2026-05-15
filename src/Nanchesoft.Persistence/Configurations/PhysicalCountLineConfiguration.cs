using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;
namespace Nanchesoft.Persistence.Configurations;
public sealed class PhysicalCountLineConfiguration : IEntityTypeConfiguration<PhysicalCountLine>
{
    public void Configure(EntityTypeBuilder<PhysicalCountLine> builder)
    {
        builder.ToTable("physical_count_lines");
        builder.HasKey(x => x.Id);
    }
}
