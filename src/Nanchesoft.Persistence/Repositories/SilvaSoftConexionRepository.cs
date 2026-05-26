using Microsoft.EntityFrameworkCore;
using Nanchesoft.Application.SilvaSoft;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Persistence.Repositories;

/// <summary>
/// Implementación del repositorio de conexiones SilvaSoft sobre Postgres/EF Core.
///
/// MULTI-TENANT: filtra siempre por empresaId para evitar cross-tenant data leaks.
/// </summary>
public sealed class SilvaSoftConexionRepository : ISilvaSoftConexionRepository
{
    private readonly NanchesoftDbContext _db;

    public SilvaSoftConexionRepository(NanchesoftDbContext db) => _db = db;

    public async Task<SilvaSoftConexionDto?> ObtenerPorEmpresaAsync(Guid empresaId, CancellationToken ct = default)
        => await _db.SilvaSoftConexiones
            .AsNoTracking()
            .Where(x => x.CompanyId == empresaId && x.IsActive)
            .Select(x => new SilvaSoftConexionDto
            {
                Id = x.Id,
                NombreServidor = x.NombreServidor,
                BaseDatos = x.BaseDatos,
                Usuario = x.Usuario,
                Activo = x.IsActive,
                FechaUltimaSincronizacion = x.FechaUltimaSincronizacion,
                Notas = x.Notas,
                UsarAgente = x.UsarAgente,
                AgentUrl = x.AgentUrl
            })
            .FirstOrDefaultAsync(ct);

    public async Task<string?> ObtenerCadenaConexionAsync(Guid empresaId, CancellationToken ct = default)
    {
        var cfg = await _db.SilvaSoftConexiones
            .AsNoTracking()
            .Where(x => x.CompanyId == empresaId && x.IsActive)
            .Select(x => new { x.NombreServidor, x.BaseDatos, x.Usuario, x.PasswordEncriptado })
            .FirstOrDefaultAsync(ct);

        if (cfg is null) return null;

        // Construye cadena de conexión SQL Server.
        // El formato "SERVIDOR\INSTANCIA" es válido directamente en Data Source.
        // TODO (fase 2): desencriptar cfg.PasswordEncriptado con IDataProtectionProvider
        var password = cfg.PasswordEncriptado; // texto plano por ahora (fase 1)
        return $"Data Source={cfg.NombreServidor};" +
               $"Initial Catalog={cfg.BaseDatos};" +
               $"User ID={cfg.Usuario};" +
               $"Password={password};" +
               $"TrustServerCertificate=True;" +
               $"Connect Timeout=15;" +
               $"Application Name=Nanchesoft-Integration;";
    }

    public async Task<(string AgentUrl, string AgentToken)?> ObtenerConfigAgenteAsync(Guid empresaId, CancellationToken ct = default)
    {
        var cfg = await _db.SilvaSoftConexiones
            .AsNoTracking()
            .Where(x => x.CompanyId == empresaId && x.IsActive && x.UsarAgente)
            .Select(x => new { x.AgentUrl, x.AgentToken })
            .FirstOrDefaultAsync(ct);

        if (cfg is null || string.IsNullOrWhiteSpace(cfg.AgentUrl) || string.IsNullOrWhiteSpace(cfg.AgentToken))
            return null;

        return (cfg.AgentUrl, cfg.AgentToken);
    }

    public Task<bool> ExisteParaEmpresaAsync(Guid empresaId, CancellationToken ct = default)
        => _db.SilvaSoftConexiones.AnyAsync(x => x.CompanyId == empresaId, ct);

    public async Task GuardarAsync(
        SilvaSoftConexionRequest request,
        Guid tenantId,
        Guid empresaId,
        string? createdBy = null,
        CancellationToken ct = default)
    {
        var existing = await _db.SilvaSoftConexiones
            .FirstOrDefaultAsync(x => x.CompanyId == empresaId, ct);

        if (existing is not null)
        {
            existing.NombreServidor = request.NombreServidor.Trim();
            existing.BaseDatos = request.BaseDatos.Trim();
            existing.Usuario = request.Usuario.Trim();
            if (!string.IsNullOrWhiteSpace(request.Password))
                existing.PasswordEncriptado = request.Password.Trim();
            existing.Notas = request.Notas?.Trim();
            existing.IsActive = request.Activo;
            existing.UsarAgente = request.UsarAgente;
            existing.AgentUrl = request.AgentUrl?.Trim();
            if (!string.IsNullOrWhiteSpace(request.AgentToken))
                existing.AgentToken = request.AgentToken.Trim();
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedBy = createdBy ?? "web-api";
        }
        else
        {
            _db.SilvaSoftConexiones.Add(new SilvaSoftConexion
            {
                TenantId = tenantId,
                CompanyId = empresaId,
                NombreServidor = request.NombreServidor.Trim(),
                BaseDatos = request.BaseDatos.Trim(),
                Usuario = request.Usuario.Trim(),
                PasswordEncriptado = request.Password?.Trim() ?? string.Empty,
                Notas = request.Notas?.Trim(),
                IsActive = request.Activo,
                UsarAgente = request.UsarAgente,
                AgentUrl = request.AgentUrl?.Trim(),
                AgentToken = request.AgentToken?.Trim(),
                CreatedBy = createdBy ?? "web-api"
            });
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task ActualizarFechaSincAsync(Guid empresaId, CancellationToken ct = default)
    {
        var entity = await _db.SilvaSoftConexiones
            .FirstOrDefaultAsync(x => x.CompanyId == empresaId, ct);
        if (entity is null) return;
        entity.FechaUltimaSincronizacion = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}
