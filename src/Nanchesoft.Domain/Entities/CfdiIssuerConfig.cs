namespace Nanchesoft.Domain.Entities;

public class CfdiIssuerConfig
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    public string Rfc { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public string FiscalRegime { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string CertificateNumber { get; set; } = string.Empty;
    public string PacName { get; set; } = string.Empty;
    public bool IsTestingMode { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
