namespace Nanchesoft.Application.SilvaSoft;

/// <summary>
/// Repositorio para gestionar configuraciones de conexión SilvaSoft en Postgres.
/// La implementación concreta vive en Nanchesoft.Persistence.
///
/// MULTI-TENANT: todos los métodos reciben empresaId para aislar datos por empresa.
/// </summary>
public interface ISilvaSoftConexionRepository
{
    /// <summary>Devuelve la configuración activa de la empresa, sin contraseña.</summary>
    Task<SilvaSoftConexionDto?> ObtenerPorEmpresaAsync(Guid empresaId, CancellationToken ct = default);

    /// <summary>
    /// Devuelve la cadena de conexión SQL Server completa (incluye contraseña).
    /// Solo debe usarse internamente por ISilvaSoftService.
    /// </summary>
    Task<string?> ObtenerCadenaConexionAsync(Guid empresaId, CancellationToken ct = default);

    Task<bool> ExisteParaEmpresaAsync(Guid empresaId, CancellationToken ct = default);

    Task GuardarAsync(SilvaSoftConexionRequest request, Guid tenantId, Guid empresaId, string? createdBy = null, CancellationToken ct = default);

    /// <summary>Marca la fecha de última sincronización exitosa.</summary>
    Task ActualizarFechaSincAsync(Guid empresaId, CancellationToken ct = default);
}
