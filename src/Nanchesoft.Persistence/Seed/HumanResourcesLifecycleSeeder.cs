using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Persistence.Seed;

public static class HumanResourcesLifecycleSeeder
{
    public static async Task SeedAsync(NanchesoftDbContext dbContext)
    {
        await EnsureSchemaAsync(dbContext);

        const string seedUser = "seed";
        var company = await dbContext.Companies.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        if (company is null)
            return;

        var branch = await dbContext.Branches.Where(x => x.CompanyId == company.Id).OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        var employee = await dbContext.Employees.Where(x => x.CompanyId == company.Id).OrderBy(x => x.EmployeeNumber).FirstOrDefaultAsync();
        if (employee is null)
            return;

        var department = await dbContext.Departments.Where(x => x.CompanyId == company.Id).OrderBy(x => x.Code).FirstOrDefaultAsync();
        var position = await dbContext.Positions.Where(x => x.CompanyId == company.Id).OrderBy(x => x.Code).FirstOrDefaultAsync();

        var documentId = Guid.Parse("E5000000-0000-0000-0000-000000000501");
        var movementId = Guid.Parse("E5000000-0000-0000-0000-000000000502");
        var certificationId = Guid.Parse("E5000000-0000-0000-0000-000000000503");

        if (!await dbContext.EmployeeDocumentRecords.AnyAsync(x => x.CompanyId == company.Id && x.EmployeeId == employee.Id && x.DocumentCode == "INE"))
        {
            dbContext.EmployeeDocumentRecords.Add(new EmployeeDocumentRecord
            {
                Id = documentId,
                TenantId = company.TenantId,
                CompanyId = company.Id,
                BranchId = branch?.Id,
                EmployeeId = employee.Id,
                DocumentCode = "INE",
                DocumentName = "Identificación oficial",
                DocumentType = "legal",
                DocumentNumber = "DOC-DEMO-001",
                IssueDate = CreateUtcDate(2025, 1, 10),
                ExpirationDate = CreateUtcDate(2035, 1, 10),
                UploadedAt = DateTime.UtcNow,
                VerifiedAt = DateTime.UtcNow,
                FileName = "ine-demo.pdf",
                FilePath = "/storage/hr/docs/ine-demo.pdf",
                Status = "verified",
                IsRequired = true,
                IsVerified = true,
                VerifiedBy = "Recursos Humanos",
                Notes = "Documento demo para expediente digital enterprise.",
                CreatedBy = seedUser
            });
        }

        if (!await dbContext.EmployeeLaborMovements.AnyAsync(x => x.CompanyId == company.Id && x.MovementCode == "MOV-0001"))
        {
            dbContext.EmployeeLaborMovements.Add(new EmployeeLaborMovement
            {
                Id = movementId,
                TenantId = company.TenantId,
                CompanyId = company.Id,
                BranchId = branch?.Id,
                EmployeeId = employee.Id,
                DepartmentId = department?.Id,
                PositionId = position?.Id,
                MovementCode = "MOV-0001",
                MovementType = "salary_change",
                EffectiveDate = CreateUtcDate(2026, 4, 1),
                AppliedAt = DateTime.UtcNow,
                PreviousValue = "Salario diario 450",
                NewValue = "Salario diario 500",
                SalaryBefore = 450m,
                SalaryAfter = 500m,
                AuthorizedBy = "Dirección RH",
                Status = "approved",
                ImpactsPayroll = true,
                Notes = "Movimiento demo de incremento salarial.",
                CreatedBy = seedUser
            });
        }

        if (!await dbContext.EmployeeCertificationRecords.AnyAsync(x => x.CompanyId == company.Id && x.EmployeeId == employee.Id && x.CertificationCode == "CERT-SEG"))
        {
            dbContext.EmployeeCertificationRecords.Add(new EmployeeCertificationRecord
            {
                Id = certificationId,
                TenantId = company.TenantId,
                CompanyId = company.Id,
                BranchId = branch?.Id,
                EmployeeId = employee.Id,
                CertificationCode = "CERT-SEG",
                CertificationName = "Seguridad industrial básica",
                Category = "training",
                IssuedBy = "Nanchesoft Academy",
                IssueDate = CreateUtcDate(2026, 2, 15),
                ExpirationDate = CreateUtcDate(2027, 2, 15),
                Score = 95m,
                Status = "active",
                IsMandatory = true,
                RenewalRequired = true,
                Notes = "Certificación demo para cumplimiento y desarrollo.",
                CreatedBy = seedUser
            });
        }

        await dbContext.SaveChangesAsync();
    }

    private static DateTime CreateUtcDate(int year, int month, int day)
        => new(year, month, day, 0, 0, 0, DateTimeKind.Utc);

    private static async Task EnsureSchemaAsync(NanchesoftDbContext dbContext)
    {
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS hr_employee_documents (
                "Id" uuid PRIMARY KEY,
                "TenantId" uuid NOT NULL,
                "CompanyId" uuid NOT NULL,
                "BranchId" uuid NULL,
                "EmployeeId" uuid NOT NULL,
                "DocumentCode" character varying(30) NOT NULL,
                "DocumentName" character varying(160) NOT NULL,
                "DocumentType" character varying(60) NOT NULL,
                "DocumentNumber" character varying(80) NOT NULL,
                "IssueDate" timestamp with time zone NULL,
                "ExpirationDate" timestamp with time zone NULL,
                "UploadedAt" timestamp with time zone NULL,
                "VerifiedAt" timestamp with time zone NULL,
                "FileName" character varying(180) NOT NULL,
                "FilePath" character varying(300) NOT NULL,
                "Status" character varying(30) NOT NULL,
                "IsRequired" boolean NOT NULL,
                "IsVerified" boolean NOT NULL,
                "VerifiedBy" character varying(120) NOT NULL,
                "Notes" character varying(800) NOT NULL,
                "IsActive" boolean NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "CreatedBy" text NULL,
                "UpdatedAt" timestamp with time zone NULL,
                "UpdatedBy" text NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ix_hr_employee_documents_company_employee_code ON hr_employee_documents ("CompanyId", "EmployeeId", "DocumentCode");
            """);

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS hr_employee_movements (
                "Id" uuid PRIMARY KEY,
                "TenantId" uuid NOT NULL,
                "CompanyId" uuid NOT NULL,
                "BranchId" uuid NULL,
                "EmployeeId" uuid NOT NULL,
                "DepartmentId" uuid NULL,
                "PositionId" uuid NULL,
                "MovementCode" character varying(40) NOT NULL,
                "MovementType" character varying(40) NOT NULL,
                "EffectiveDate" timestamp with time zone NOT NULL,
                "AppliedAt" timestamp with time zone NULL,
                "PreviousValue" character varying(180) NOT NULL,
                "NewValue" character varying(180) NOT NULL,
                "SalaryBefore" numeric(18,2) NOT NULL,
                "SalaryAfter" numeric(18,2) NOT NULL,
                "AuthorizedBy" character varying(120) NOT NULL,
                "Status" character varying(30) NOT NULL,
                "ImpactsPayroll" boolean NOT NULL,
                "Notes" character varying(800) NOT NULL,
                "IsActive" boolean NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "CreatedBy" text NULL,
                "UpdatedAt" timestamp with time zone NULL,
                "UpdatedBy" text NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ix_hr_employee_movements_company_code ON hr_employee_movements ("CompanyId", "MovementCode");
            """);

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS hr_employee_certifications (
                "Id" uuid PRIMARY KEY,
                "TenantId" uuid NOT NULL,
                "CompanyId" uuid NOT NULL,
                "BranchId" uuid NULL,
                "EmployeeId" uuid NOT NULL,
                "CertificationCode" character varying(40) NOT NULL,
                "CertificationName" character varying(160) NOT NULL,
                "Category" character varying(60) NOT NULL,
                "IssuedBy" character varying(160) NOT NULL,
                "IssueDate" timestamp with time zone NOT NULL,
                "ExpirationDate" timestamp with time zone NULL,
                "Score" numeric(18,2) NOT NULL,
                "Status" character varying(30) NOT NULL,
                "IsMandatory" boolean NOT NULL,
                "RenewalRequired" boolean NOT NULL,
                "Notes" character varying(800) NOT NULL,
                "IsActive" boolean NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "CreatedBy" text NULL,
                "UpdatedAt" timestamp with time zone NULL,
                "UpdatedBy" text NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ix_hr_employee_certifications_company_employee_code ON hr_employee_certifications ("CompanyId", "EmployeeId", "CertificationCode");
            """);
    }
}
