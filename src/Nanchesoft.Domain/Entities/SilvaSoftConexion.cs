using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

/*
 * SilvaSoftConexion — configuración de acceso a SQL Server de SilvaSoft por empresa.
 *
 * MULTI-TENANT: cada empresa tiene su propia fila con sus credenciales SilvaSoft.
 * Esto permite que varios clientes (tenants) usen el mismo Nanchesoft cloud
 * conectados cada uno a su propia instancia de SilvaSoft on-premise.
 *
 * ARQUITECTURA FUTURA (cloud ↔ on-premise):
 * SilvaSoft corre en SQL Server local del cliente y Nanchesoft está en la nube.
 * Dos opciones de integración planificadas:
 *   1. SqlClient directo (requiere que el SQL Server sea accesible desde internet → firewall/VPN)
 *   2. Agente Windows Service local: el agente corre en la red del cliente,
 *      expone una REST API interna, y Nanchesoft consume esa API en lugar de
 *      conectarse a SQL Server directamente. Esto evita abrir puertos sensibles.
 *      Para habilitar la opción 2, simplemente se cambia la implementación de
 *      ISilvaSoftService sin tocar ninguna otra capa.
 *
 * SEGURIDAD:
 * TODO (fase 2): encriptar PasswordEncriptado usando IDataProtectionProvider
 * de ASP.NET Core antes de guardar y desencriptar al leer.
 * Por ahora se almacena en texto plano sólo para el MVP inicial.
 */
public sealed class SilvaSoftConexion : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    /// <summary>Servidor SQL Server. Soporta formato: "HOST\INSTANCIA", IP o FQDN.</summary>
    public string NombreServidor { get; set; } = string.Empty;

    /// <summary>Nombre de la base de datos SilvaSoft en el servidor.</summary>
    public string BaseDatos { get; set; } = string.Empty;

    /// <summary>Usuario de autenticación SQL Server.</summary>
    public string Usuario { get; set; } = string.Empty;

    /// <summary>
    /// Contraseña del usuario SQL Server.
    /// TODO (fase 2): proteger con IDataProtectionProvider antes de persistir.
    /// </summary>
    public string PasswordEncriptado { get; set; } = string.Empty;

    public DateTime? FechaUltimaSincronizacion { get; set; }

    public string? Notas { get; set; }
}
