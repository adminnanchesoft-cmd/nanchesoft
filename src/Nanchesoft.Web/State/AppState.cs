namespace Nanchesoft.Web.State;

public sealed class AppState
{
    public Guid? CurrentTenantId { get; set; }
    public Guid? CurrentCompanyId { get; set; }
    public Guid? CurrentBranchId { get; set; }

    public string CurrentTenantName { get; set; } = "Sin tenant";
    public string CurrentCompanyName { get; set; } = "Sin empresa";
    public string CurrentBranchName { get; set; } = "Sin sucursal";
}
