namespace Nanchesoft.Web.State;

/// <summary>
/// Acceso al contexto del tenant que SÍ cruza scopes de DI gracias a AsyncLocal.
///
/// Existe porque <see cref="AppState"/> es Scoped y, en Blazor Server, el
/// <c>IHttpClientFactory</c> resuelve los <c>DelegatingHandler</c> en SU PROPIO
/// scope — distinto al del circuito Blazor — por lo que el handler ve un
/// AppState vacío. AsyncLocal fluye con la cadena async, no con el scope DI,
/// así que el handler sí puede leer el tenant del circuito que disparó la
/// llamada HTTP.
/// </summary>
public sealed class TenantContextAccessor
{
    private static readonly AsyncLocal<TenantContextSnapshot?> _current = new();

    public TenantContextSnapshot? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }

    public void Set(Guid? tenantId, Guid? companyId, Guid? branchId, Guid? userId, bool isPlatformOwner)
        => Current = new TenantContextSnapshot(tenantId, companyId, branchId, userId, isPlatformOwner);

    public void Clear() => Current = null;
}

public sealed record TenantContextSnapshot(
    Guid? TenantId,
    Guid? CompanyId,
    Guid? BranchId,
    Guid? UserId,
    bool IsPlatformOwner);
