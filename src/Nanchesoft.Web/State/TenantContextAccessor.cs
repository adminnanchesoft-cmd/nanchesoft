using System.Runtime.CompilerServices;

namespace Nanchesoft.Web.State;

/// <summary>
/// Puente de contexto de tenant hacia ApiTenantScopeHandler, que vive en el scope
/// de IHttpClientFactory (distinto al circuito Blazor).
///
/// Estrategia dual:
///   1. AsyncLocal  — funciona cuando el HTTP call ocurre en la misma cadena async
///      que llamó Set() (mismo evento Blazor).
///   2. ConditionalWeakTable keyed by SynchronizationContext — funciona entre
///      eventos Blazor distintos del mismo circuito (login → navegación SPA →
///      OnInitializedAsync). La clave es débil: se GC-colecta cuando el circuito
///      desconecta, sin necesidad de limpieza explícita.
/// </summary>
public sealed class TenantContextAccessor
{
    private static readonly AsyncLocal<TenantContextSnapshot?> _asyncCurrent = new();
    private static readonly ConditionalWeakTable<SynchronizationContext, Holder> _circuitMap = new();

    private sealed class Holder { public TenantContextSnapshot? Snapshot; }

    public TenantContextSnapshot? Current
    {
        get
        {
            if (_asyncCurrent.Value is { } v) return v;
            var sc = SynchronizationContext.Current;
            if (sc is not null && _circuitMap.TryGetValue(sc, out var h)) return h.Snapshot;
            return null;
        }
        set => _asyncCurrent.Value = value;
    }

    public void Set(Guid? tenantId, Guid? companyId, Guid? branchId, Guid? userId, bool isPlatformOwner)
    {
        var snapshot = new TenantContextSnapshot(tenantId, companyId, branchId, userId, isPlatformOwner);
        _asyncCurrent.Value = snapshot;
        var sc = SynchronizationContext.Current;
        if (sc is not null)
            _circuitMap.GetOrCreateValue(sc).Snapshot = snapshot;
    }

    public void Clear()
    {
        _asyncCurrent.Value = null;
        var sc = SynchronizationContext.Current;
        if (sc is not null && _circuitMap.TryGetValue(sc, out var h))
            h.Snapshot = null;
    }
}

public sealed record TenantContextSnapshot(
    Guid? TenantId,
    Guid? CompanyId,
    Guid? BranchId,
    Guid? UserId,
    bool IsPlatformOwner);
