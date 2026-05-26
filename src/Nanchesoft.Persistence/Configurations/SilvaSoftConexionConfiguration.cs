using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class SilvaSoftConexionConfiguration : IEntityTypeConfiguration<SilvaSoftConexion>
{
    public void Configure(EntityTypeBuilder<SilvaSoftConexion> builder)
    {
        builder.ToTable("silvasoft_conexiones");
        builder.HasKey(x => x.Id);

        // Campos con nombres Spanish explícitos según spec
        builder.Property(x => x.NombreServidor).HasColumnName("nombre_servidor").HasMaxLength(256).IsRequired();
        builder.Property(x => x.BaseDatos).HasColumnName("base_datos").HasMaxLength(128).IsRequired();
        builder.Property(x => x.Usuario).HasColumnName("usuario").HasMaxLength(128).IsRequired();
        builder.Property(x => x.PasswordEncriptado).HasColumnName("password_encriptado").HasMaxLength(1024).IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("activo");
        builder.Property(x => x.FechaUltimaSincronizacion).HasColumnName("fecha_ultima_sincronizacion");
        builder.Property(x => x.Notas).HasMaxLength(500);

        // Una configuración por empresa
        builder.HasIndex(x => x.CompanyId).IsUnique();

        builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
    }
}
