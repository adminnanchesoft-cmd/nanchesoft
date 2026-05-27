using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

/// <summary>
/// Regla de AUTO-REPLICACIÓN de destajos entre fases productivas.
///
/// Cuando un operario captura un destajo en FromPhase, el sistema DEBE generar
/// automáticamente otro destajo en ToPhase con la MISMA cantidad, asignado a
/// DefaultProductionCellId (o CaptureProductionCellId si se especifica).
///
/// Reglas:
/// - 1:1 estricto: UNIQUE en (CompanyId, FromPhaseId)
/// - Recursivo: si ToPhase tiene a su vez una regla, también se dispara (A→B→C...)
/// - Sin ciclos: trigger PostgreSQL previene A→...→A
/// - Misma cantidad exacta (sin multiplicadores)
///
/// La IMPLEMENTACIÓN de la lógica de auto-replicación en C# se hará en un prompt
/// posterior cuando se construya el módulo de captura de destajos (PieceWorkRecord).
/// Esta entidad solo MODELA la configuración de la regla.
///
/// Equivalente a dbo.Fraccion_Cadena de Silvasoft Orange.
/// </summary>
public sealed class ProductionPhaseAutoLink : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    /// <summary>Fase principal cuya captura de destajo DISPARA la auto-replicación.</summary>
    public Guid FromPhaseId { get; set; }
    public ProductionPhase? FromPhase { get; set; }

    /// <summary>Fase secundaria que se auto-genera con la misma cantidad.</summary>
    public Guid ToPhaseId { get; set; }
    public ProductionPhase? ToPhase { get; set; }

    /// <summary>Célula de producción default a asignar al destajo auto-generado.</summary>
    public Guid? DefaultProductionCellId { get; set; }

    /// <summary>Célula que captura el destajo (opcional, sobrescribe la default si se especifica).</summary>
    public Guid? CaptureProductionCellId { get; set; }

    /// <summary>Mapeo opcional con Silvasoft Orange Fraccion_CadenaID.</summary>
    public Guid? SilvasoftChainId { get; set; }
}
