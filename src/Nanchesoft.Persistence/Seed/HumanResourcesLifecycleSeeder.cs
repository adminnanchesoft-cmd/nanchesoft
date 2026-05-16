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
            CREATE TABLE IF NOT EXISTS hr.hr_employee_documents (
                id uuid PRIMARY KEY,
                tenant_id uuid NOT NULL,
                company_id uuid NOT NULL,
                branch_id uuid NULL,
                employee_id uuid NOT NULL,
                document_code character varying(30) NOT NULL,
                document_name character varying(160) NOT NULL,
                document_type character varying(60) NOT NULL,
                document_number character varying(80) NOT NULL,
                issue_date timestamp with time zone NULL,
                expiration_date timestamp with time zone NULL,
                uploaded_at timestamp with time zone NULL,
                verified_at timestamp with time zone NULL,
                file_name character varying(180) NOT NULL,
                file_path character varying(300) NOT NULL,
                status character varying(30) NOT NULL,
                is_required boolean NOT NULL,
                is_verified boolean NOT NULL,
                verified_by character varying(120) NOT NULL,
                notes character varying(800) NOT NULL,
                is_active boolean NOT NULL,
                created_at timestamp with time zone NOT NULL,
                created_by text NULL,
                updated_at timestamp with time zone NULL,
                updated_by text NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ix_hr_employee_documents_company_employee_code ON hr.hr_employee_documents (company_id, employee_id, document_code);
            """);

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS hr.hr_employee_movements (
                id uuid PRIMARY KEY,
                tenant_id uuid NOT NULL,
                company_id uuid NOT NULL,
                branch_id uuid NULL,
                employee_id uuid NOT NULL,
                department_id uuid NULL,
                position_id uuid NULL,
                movement_code character varying(40) NOT NULL,
                movement_type character varying(40) NOT NULL,
                effective_date timestamp with time zone NOT NULL,
                applied_at timestamp with time zone NULL,
                previous_value character varying(180) NOT NULL,
                new_value character varying(180) NOT NULL,
                salary_before numeric(18,2) NOT NULL,
                salary_after numeric(18,2) NOT NULL,
                authorized_by character varying(120) NOT NULL,
                status character varying(30) NOT NULL,
                impacts_payroll boolean NOT NULL,
                notes character varying(800) NOT NULL,
                is_active boolean NOT NULL,
                created_at timestamp with time zone NOT NULL,
                created_by text NULL,
                updated_at timestamp with time zone NULL,
                updated_by text NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ix_hr_employee_movements_company_code ON hr.hr_employee_movements (company_id, movement_code);
            """);

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS hr.hr_employee_certifications (
                id uuid PRIMARY KEY,
                tenant_id uuid NOT NULL,
                company_id uuid NOT NULL,
                branch_id uuid NULL,
                employee_id uuid NOT NULL,
                certification_code character varying(40) NOT NULL,
                certification_name character varying(160) NOT NULL,
                category character varying(60) NOT NULL,
                issued_by character varying(160) NOT NULL,
                issue_date timestamp with time zone NOT NULL,
                expiration_date timestamp with time zone NULL,
                score numeric(18,2) NOT NULL,
                status character varying(30) NOT NULL,
                is_mandatory boolean NOT NULL,
                renewal_required boolean NOT NULL,
                notes character varying(800) NOT NULL,
                is_active boolean NOT NULL,
                created_at timestamp with time zone NOT NULL,
                created_by text NULL,
                updated_at timestamp with time zone NULL,
                updated_by text NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ix_hr_employee_certifications_company_employee_code ON hr.hr_employee_certifications (company_id, employee_id, certification_code);
            """);
    }
}
