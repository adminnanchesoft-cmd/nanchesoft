using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class PayrollSatVerificationEndpoints
{
    public static IEndpointRouteBuilder MapPayrollSatVerificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/payroll/sat-verification").WithTags("PayrollSatVerification");
        group.MapGet("/", GetVerificationAsync);
        group.MapGet("/summary", GetSummaryAsync);
        return app;
    }

    private static async Task<(Guid? TenantId, Guid? CompanyId)> ResolveContextAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        var tenantId = ApiTenantScope.ResolveTenantId(httpContext);
        var companyId = ApiTenantScope.ResolveCompanyId(httpContext);

        if (companyId.HasValue)
        {
            if (!tenantId.HasValue)
                tenantId = await db.Companies.Where(x => x.Id == companyId.Value).Select(x => (Guid?)x.TenantId).FirstOrDefaultAsync();
            return (tenantId, companyId);
        }

        if (tenantId.HasValue)
        {
            var comp = await db.Companies.Where(x => x.TenantId == tenantId.Value).OrderBy(x => x.CreatedAt).Select(x => new { x.Id, x.TenantId }).FirstOrDefaultAsync();
            if (comp is not null) return (comp.TenantId, comp.Id);
        }

        var fallback = await db.Companies.OrderBy(x => x.CreatedAt).Select(x => new { x.Id, x.TenantId }).FirstOrDefaultAsync();
        return fallback is null ? (null, null) : (fallback.TenantId, fallback.Id);
    }

    private static bool IsRfcValid(string? rfc)
    {
        if (string.IsNullOrWhiteSpace(rfc)) return false;
        var trimmed = rfc.Trim().ToUpperInvariant();
        return trimmed.Length >= 12 && trimmed.Length <= 13 && System.Text.RegularExpressions.Regex.IsMatch(trimmed, "^[A-Z&Ñ]{3,4}[0-9]{6}[A-Z0-9]{2,3}$");
    }

    private static bool IsCurpValid(string? curp)
    {
        if (string.IsNullOrWhiteSpace(curp)) return false;
        var trimmed = curp.Trim().ToUpperInvariant();
        return trimmed.Length == 18 && System.Text.RegularExpressions.Regex.IsMatch(trimmed, "^[A-Z]{4}[0-9]{6}[HM][A-Z]{5}[0-9A-Z][0-9]$");
    }

    private static bool IsNssValid(string? nss)
    {
        if (string.IsNullOrWhiteSpace(nss)) return false;
        var digits = new string(nss.Where(char.IsDigit).ToArray());
        return digits.Length == 11;
    }

    private static async Task<IResult> GetVerificationAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        var report = await BuildReportAsync(httpContext, db);
        return Results.Ok(report);
    }

    private static async Task<IResult> GetSummaryAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        var report = await BuildReportAsync(httpContext, db);
        return Results.Ok(new PayrollSatVerificationSummaryDto
        {
            TotalIssues = report.Sections.Sum(x => x.Findings.Count),
            CriticalIssues = report.Sections.Where(x => x.Severity == "critical").Sum(x => x.Findings.Count),
            WarningIssues = report.Sections.Where(x => x.Severity == "warning").Sum(x => x.Findings.Count),
            IsReadyToStamp = report.Sections.All(x => x.Severity != "critical" || x.Findings.Count == 0),
            CheckedAt = report.CheckedAt
        });
    }

    private static async Task<PayrollSatVerificationReportDto> BuildReportAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        var context = await ResolveContextAsync(httpContext, db);
        var report = new PayrollSatVerificationReportDto
        {
            CompanyId = context.CompanyId ?? Guid.Empty,
            CheckedAt = DateTime.UtcNow,
            Sections = []
        };

        if (!context.CompanyId.HasValue)
            return report;

        var companyId = context.CompanyId.Value;

        // ── Conceptos ──
        var concepts = await db.PayrollConcepts.AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .Select(x => new
            {
                x.Id, x.Code, x.Name, x.ConceptType, x.IsActive,
                x.SatCode, x.SatAgrupador,
                x.SatTipoPercepcionCode, x.SatTipoDeduccionCode, x.SatTipoOtroPagoCode,
                x.TaxableType, x.TaxablePercent, x.ExemptPercent,
                x.RequiresSatStamping, x.PrintOnReceipt
            })
            .ToListAsync();

        var conceptSection = new PayrollSatVerificationSectionDto
        {
            Section = "concepts",
            Title = "Conceptos sin clave SAT",
            Severity = "critical",
            Findings = []
        };

        foreach (var c in concepts.Where(x => x.IsActive && x.RequiresSatStamping))
        {
            var missing = new List<string>();
            if (string.IsNullOrWhiteSpace(c.SatCode)) missing.Add("Código SAT");
            if (string.IsNullOrWhiteSpace(c.SatAgrupador)) missing.Add("Agrupador SAT");

            if (c.ConceptType == "perception" || c.ConceptType == "earning")
            {
                if (string.IsNullOrWhiteSpace(c.SatTipoPercepcionCode)) missing.Add("TipoPercepcion CFDI");
            }
            else if (c.ConceptType == "deduction")
            {
                if (string.IsNullOrWhiteSpace(c.SatTipoDeduccionCode)) missing.Add("TipoDeduccion CFDI");
            }

            if (c.TaxableType == "mixed" && c.TaxablePercent + c.ExemptPercent != 100m)
                missing.Add("% gravado + % exento ≠ 100");

            if (missing.Count > 0)
            {
                conceptSection.Findings.Add(new PayrollSatVerificationFindingDto
                {
                    EntityType = "concept",
                    EntityId = c.Id,
                    Code = c.Code,
                    Label = c.Name,
                    Missing = missing,
                    Message = $"{c.Code} · {c.Name}: faltan {string.Join(", ", missing)}"
                });
            }
        }
        report.Sections.Add(conceptSection);

        // ── Empleados ──
        var employees = await db.Employees.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.IsActive && x.Status == "active")
            .Select(x => new
            {
                x.Id, x.EmployeeNumber, x.FirstName, x.LastName,
                x.TaxId, x.Curp, x.Nss, x.TaxRegime, x.ImssRegId,
                x.BankCode, x.BankAccount, x.Clabe,
                x.PaymentForm, x.PeriodSalary, x.DailySalary
            })
            .ToListAsync();

        var employeeFiscalSection = new PayrollSatVerificationSectionDto
        {
            Section = "employees-fiscal",
            Title = "Empleados con datos fiscales incompletos",
            Severity = "critical",
            Findings = []
        };
        var employeeImssSection = new PayrollSatVerificationSectionDto
        {
            Section = "employees-imss",
            Title = "Empleados sin registro patronal IMSS",
            Severity = "warning",
            Findings = []
        };
        var employeeBankSection = new PayrollSatVerificationSectionDto
        {
            Section = "employees-bank",
            Title = "Empleados sin cuenta bancaria para dispersión",
            Severity = "warning",
            Findings = []
        };

        foreach (var e in employees)
        {
            var fullName = (e.FirstName + " " + e.LastName).Trim();
            var label = $"{e.EmployeeNumber} · {fullName}";

            var fiscalMissing = new List<string>();
            if (!IsRfcValid(e.TaxId)) fiscalMissing.Add("RFC inválido");
            if (!IsCurpValid(e.Curp)) fiscalMissing.Add("CURP inválida");
            if (!IsNssValid(e.Nss)) fiscalMissing.Add("NSS inválido");
            if (string.IsNullOrWhiteSpace(e.TaxRegime)) fiscalMissing.Add("Régimen fiscal");

            if (fiscalMissing.Count > 0)
            {
                employeeFiscalSection.Findings.Add(new PayrollSatVerificationFindingDto
                {
                    EntityType = "employee",
                    EntityId = e.Id,
                    Code = e.EmployeeNumber,
                    Label = label,
                    Missing = fiscalMissing,
                    Message = $"{label}: {string.Join(", ", fiscalMissing)}"
                });
            }

            if (string.IsNullOrWhiteSpace(e.ImssRegId))
            {
                employeeImssSection.Findings.Add(new PayrollSatVerificationFindingDto
                {
                    EntityType = "employee",
                    EntityId = e.Id,
                    Code = e.EmployeeNumber,
                    Label = label,
                    Missing = new List<string> { "Registro patronal IMSS" },
                    Message = $"{label}: sin registro patronal IMSS asignado"
                });
            }

            if (string.Equals(e.PaymentForm, "tarjeta", StringComparison.OrdinalIgnoreCase)
                || string.Equals(e.PaymentForm, "transferencia", StringComparison.OrdinalIgnoreCase))
            {
                var bankMissing = new List<string>();
                if (string.IsNullOrWhiteSpace(e.BankCode)) bankMissing.Add("Banco");
                if (string.IsNullOrWhiteSpace(e.Clabe) && string.IsNullOrWhiteSpace(e.BankAccount)) bankMissing.Add("CLABE/Cuenta");

                if (bankMissing.Count > 0)
                {
                    employeeBankSection.Findings.Add(new PayrollSatVerificationFindingDto
                    {
                        EntityType = "employee",
                        EntityId = e.Id,
                        Code = e.EmployeeNumber,
                        Label = label,
                        Missing = bankMissing,
                        Message = $"{label}: {string.Join(", ", bankMissing)}"
                    });
                }
            }
        }
        report.Sections.Add(employeeFiscalSection);
        report.Sections.Add(employeeImssSection);
        report.Sections.Add(employeeBankSection);

        // ── Salarios sospechosos ──
        var salarySection = new PayrollSatVerificationSectionDto
        {
            Section = "employees-salary",
            Title = "Empleados sin salario configurado",
            Severity = "warning",
            Findings = []
        };
        foreach (var e in employees.Where(x => x.PeriodSalary <= 0m && x.DailySalary <= 0m))
        {
            var label = $"{e.EmployeeNumber} · {(e.FirstName + " " + e.LastName).Trim()}";
            salarySection.Findings.Add(new PayrollSatVerificationFindingDto
            {
                EntityType = "employee",
                EntityId = e.Id,
                Code = e.EmployeeNumber,
                Label = label,
                Missing = new List<string> { "Salario diario / del periodo" },
                Message = $"{label}: sin salario configurado"
            });
        }
        report.Sections.Add(salarySection);

        // ── Empresa: emisor CFDI ──
        var companyData = await db.Companies.AsNoTracking()
            .Where(x => x.Id == companyId)
            .Select(x => new { x.TaxId, x.LegalName, x.Name })
            .FirstOrDefaultAsync();

        var companySection = new PayrollSatVerificationSectionDto
        {
            Section = "company",
            Title = "Configuración fiscal de la empresa",
            Severity = "critical",
            Findings = []
        };
        if (companyData is not null)
        {
            var missing = new List<string>();
            if (!IsRfcValid(companyData.TaxId)) missing.Add("RFC empresa");
            if (string.IsNullOrWhiteSpace(companyData.LegalName)) missing.Add("Razón social");
            if (missing.Count > 0)
            {
                companySection.Findings.Add(new PayrollSatVerificationFindingDto
                {
                    EntityType = "company",
                    EntityId = companyId,
                    Code = string.Empty,
                    Label = companyData.Name ?? string.Empty,
                    Missing = missing,
                    Message = $"Empresa: faltan {string.Join(", ", missing)}"
                });
            }
        }
        report.Sections.Add(companySection);

        return report;
    }
}

public sealed class PayrollSatVerificationReportDto
{
    public Guid CompanyId { get; set; }
    public DateTime CheckedAt { get; set; }
    public List<PayrollSatVerificationSectionDto> Sections { get; set; } = [];
}

public sealed class PayrollSatVerificationSectionDto
{
    public string Section { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Severity { get; set; } = "warning";
    public List<PayrollSatVerificationFindingDto> Findings { get; set; } = [];
}

public sealed class PayrollSatVerificationFindingDto
{
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public List<string> Missing { get; set; } = [];
    public string Message { get; set; } = string.Empty;
}

public sealed class PayrollSatVerificationSummaryDto
{
    public int TotalIssues { get; set; }
    public int CriticalIssues { get; set; }
    public int WarningIssues { get; set; }
    public bool IsReadyToStamp { get; set; }
    public DateTime CheckedAt { get; set; }
}
