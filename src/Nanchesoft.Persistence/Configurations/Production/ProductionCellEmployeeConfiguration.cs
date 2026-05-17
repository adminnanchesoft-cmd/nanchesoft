using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class ProductionCellEmployeeConfiguration : IEntityTypeConfiguration<ProductionCellEmployee>
{
    public void Configure(EntityTypeBuilder<ProductionCellEmployee> builder)
    {
        builder.ToTable("production_cell_employees");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Role).HasMaxLength(30).IsRequired();

        builder.HasIndex(x => new { x.ProductionCellId, x.EmployeeId });

        builder.HasOne(x => x.ProductionCell).WithMany(x => x.CellEmployees).HasForeignKey(x => x.ProductionCellId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
    }
}
