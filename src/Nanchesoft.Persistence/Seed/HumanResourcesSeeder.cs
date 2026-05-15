using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Persistence.Seed;

public static class HumanResourcesSeeder
{
    private static DateTime Utc(int year, int month, int day)
        => new(year, month, day, 0, 0, 0, DateTimeKind.Utc);

    public static async Task SeedAsync(NanchesoftDbContext dbContext)
    {
        const string seedUser = "seed";
        var company = await dbContext.Companies.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        var branch = await dbContext.Branches.Where(x => company != null && x.CompanyId == company.Id).OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();

        if (company is null)
            return;

        var tenantId = company.TenantId;
        var companyId = company.Id;
        var branchId = branch?.Id;

        var departmentAdminId = Guid.Parse("E1000000-0000-0000-0000-000000000001");
        var departmentOpsId = Guid.Parse("E1000000-0000-0000-0000-000000000002");
        var positionManagerId = Guid.Parse("E1000000-0000-0000-0000-000000000011");
        var positionAnalystId = Guid.Parse("E1000000-0000-0000-0000-000000000012");
        var employeeAdminId = Guid.Parse("E1000000-0000-0000-0000-000000000021");
        var employeeOpsId = Guid.Parse("E1000000-0000-0000-0000-000000000022");
        var contractAdminId = Guid.Parse("E1000000-0000-0000-0000-000000000031");
        var contractOpsId = Guid.Parse("E1000000-0000-0000-0000-000000000032");
        var periodId = Guid.Parse("E1000000-0000-0000-0000-000000000041");
        var conceptSalaryId = Guid.Parse("E1000000-0000-0000-0000-000000000051");
        var conceptBonusId = Guid.Parse("E1000000-0000-0000-0000-000000000052");
        var conceptTaxId = Guid.Parse("E1000000-0000-0000-0000-000000000053");
        var runId = Guid.Parse("E1000000-0000-0000-0000-000000000061");
        var runLine1Id = Guid.Parse("E1000000-0000-0000-0000-000000000071");
        var runLine2Id = Guid.Parse("E1000000-0000-0000-0000-000000000072");
        var incident1Id = Guid.Parse("E1000000-0000-0000-0000-000000000081");
        var detail1Id = Guid.Parse("E1000000-0000-0000-0000-000000000091");
        var detail2Id = Guid.Parse("E1000000-0000-0000-0000-000000000092");
        var detail3Id = Guid.Parse("E1000000-0000-0000-0000-000000000093");
        var detail4Id = Guid.Parse("E1000000-0000-0000-0000-000000000094");
        var detail5Id = Guid.Parse("E1000000-0000-0000-0000-000000000095");

        if (!await dbContext.Departments.AnyAsync(x => x.CompanyId == companyId && x.Code == "ADMIN"))
        {
            dbContext.Departments.Add(new Department
            {
                Id = departmentAdminId,
                TenantId = tenantId,
                CompanyId = companyId,
                Code = "ADMIN",
                Name = "Administración",
                Description = "Departamento administrativo y de soporte.",
                CreatedBy = seedUser
            });
        }

        if (!await dbContext.Departments.AnyAsync(x => x.CompanyId == companyId && x.Code == "OPER"))
        {
            dbContext.Departments.Add(new Department
            {
                Id = departmentOpsId,
                TenantId = tenantId,
                CompanyId = companyId,
                Code = "OPER",
                Name = "Operaciones",
                Description = "Departamento operativo y de control diario.",
                CreatedBy = seedUser
            });
        }

        if (!await dbContext.Positions.AnyAsync(x => x.CompanyId == companyId && x.Code == "GER"))
        {
            dbContext.Positions.Add(new Position
            {
                Id = positionManagerId,
                TenantId = tenantId,
                CompanyId = companyId,
                DepartmentId = departmentAdminId,
                Code = "GER",
                Name = "Gerente administrativo",
                Description = "Responsable del área administrativa.",
                PayrollGroup = "QUINCENAL",
                BaseSalary = 25000m,
                CreatedBy = seedUser
            });
        }

        if (!await dbContext.Positions.AnyAsync(x => x.CompanyId == companyId && x.Code == "ANL"))
        {
            dbContext.Positions.Add(new Position
            {
                Id = positionAnalystId,
                TenantId = tenantId,
                CompanyId = companyId,
                DepartmentId = departmentOpsId,
                Code = "ANL",
                Name = "Analista operativo",
                Description = "Soporte operativo para procesos internos.",
                PayrollGroup = "SEMANAL",
                BaseSalary = 9800m,
                CreatedBy = seedUser
            });
        }

        if (!await dbContext.Employees.AnyAsync(x => x.CompanyId == companyId && x.EmployeeNumber == "EMP-0001"))
        {
            dbContext.Employees.Add(new Employee
            {
                Id = employeeAdminId,
                TenantId = tenantId,
                CompanyId = companyId,
                BranchId = branchId,
                DepartmentId = departmentAdminId,
                PositionId = positionManagerId,
                Code = "EMP001",
                EmployeeNumber = "EMP-0001",
                FirstName = "María",
                LastName = "López",
                MiddleName = "Fernanda",
                Email = "maria.lopez@nanchesoft.com",
                Phone = "4771001001",
                TaxId = "LOFM900101AAA",
                NationalId = "NSS0010001",
                HireDate = Utc(2025, 1, 10),
                BirthDate = Utc(1990, 1, 1),
                DailySalary = 833.33m,
                IntegratedDailySalary = 925.00m,
                Status = "active",
                CreatedBy = seedUser
            });
        }

        if (!await dbContext.Employees.AnyAsync(x => x.CompanyId == companyId && x.EmployeeNumber == "EMP-0002"))
        {
            dbContext.Employees.Add(new Employee
            {
                Id = employeeOpsId,
                TenantId = tenantId,
                CompanyId = companyId,
                BranchId = branchId,
                DepartmentId = departmentOpsId,
                PositionId = positionAnalystId,
                Code = "EMP002",
                EmployeeNumber = "EMP-0002",
                FirstName = "Carlos",
                LastName = "Ruiz",
                MiddleName = "Alberto",
                Email = "carlos.ruiz@nanchesoft.com",
                Phone = "4771001002",
                TaxId = "RUAC920202BBB",
                NationalId = "NSS0010002",
                HireDate = Utc(2025, 3, 1),
                BirthDate = Utc(1992, 2, 2),
                DailySalary = 326.67m,
                IntegratedDailySalary = 355.20m,
                Status = "active",
                CreatedBy = seedUser
            });
        }

        if (!await dbContext.EmployeeContracts.AnyAsync(x => x.CompanyId == companyId && x.ContractNumber == "CONT-0001"))
        {
            dbContext.EmployeeContracts.Add(new EmployeeContract
            {
                Id = contractAdminId,
                TenantId = tenantId,
                CompanyId = companyId,
                BranchId = branchId,
                EmployeeId = employeeAdminId,
                ContractNumber = "CONT-0001",
                ContractType = "indefinite",
                StartDate = Utc(2025, 1, 10),
                PaymentFrequency = "quincenal",
                BaseSalary = 25000m,
                IntegratedSalary = 27750m,
                Status = "active",
                Notes = "Contrato demo de administración.",
                CreatedBy = seedUser
            });
        }

        if (!await dbContext.EmployeeContracts.AnyAsync(x => x.CompanyId == companyId && x.ContractNumber == "CONT-0002"))
        {
            dbContext.EmployeeContracts.Add(new EmployeeContract
            {
                Id = contractOpsId,
                TenantId = tenantId,
                CompanyId = companyId,
                BranchId = branchId,
                EmployeeId = employeeOpsId,
                ContractNumber = "CONT-0002",
                ContractType = "indefinite",
                StartDate = Utc(2025, 3, 1),
                PaymentFrequency = "semanal",
                BaseSalary = 9800m,
                IntegratedSalary = 10600m,
                Status = "active",
                Notes = "Contrato demo operativo.",
                CreatedBy = seedUser
            });
        }

        if (!await dbContext.PayrollPeriods.AnyAsync(x => x.CompanyId == companyId && x.Code == "2026-Q1-APR"))
        {
            dbContext.PayrollPeriods.Add(new PayrollPeriod
            {
                Id = periodId,
                TenantId = tenantId,
                CompanyId = companyId,
                Code = "2026-Q1-APR",
                Name = "Quincena abril 1",
                PeriodType = "quincenal",
                StartDate = Utc(2026, 4, 1),
                EndDate = Utc(2026, 4, 15),
                PaymentDate = Utc(2026, 4, 15),
                Status = "open",
                IsClosed = false,
                CreatedBy = seedUser
            });
        }

        if (!await dbContext.PayrollConcepts.AnyAsync(x => x.CompanyId == companyId && x.Code == "SAL"))
        {
            dbContext.PayrollConcepts.Add(new PayrollConcept
            {
                Id = conceptSalaryId,
                TenantId = tenantId,
                CompanyId = companyId,
                Code = "SAL",
                Name = "Sueldo base",
                ConceptType = "perception",
                CalculationType = "fixed",
                SatCode = "001",
                TaxableType = "taxable",
                IsRecurring = true,
                CreatedBy = seedUser
            });
        }

        if (!await dbContext.PayrollConcepts.AnyAsync(x => x.CompanyId == companyId && x.Code == "BON"))
        {
            dbContext.PayrollConcepts.Add(new PayrollConcept
            {
                Id = conceptBonusId,
                TenantId = tenantId,
                CompanyId = companyId,
                Code = "BON",
                Name = "Bono productividad",
                ConceptType = "perception",
                CalculationType = "manual",
                SatCode = "019",
                TaxableType = "mixed",
                IsRecurring = false,
                CreatedBy = seedUser
            });
        }

        if (!await dbContext.PayrollConcepts.AnyAsync(x => x.CompanyId == companyId && x.Code == "ISR"))
        {
            dbContext.PayrollConcepts.Add(new PayrollConcept
            {
                Id = conceptTaxId,
                TenantId = tenantId,
                CompanyId = companyId,
                Code = "ISR",
                Name = "ISR nómina",
                ConceptType = "deduction",
                CalculationType = "formula",
                SatCode = "002",
                TaxableType = "not_applicable",
                IsRecurring = true,
                CreatedBy = seedUser
            });
        }

        if (!await dbContext.PayrollRuns.AnyAsync(x => x.CompanyId == companyId && x.Folio == "NOM-0001"))
        {
            dbContext.PayrollRuns.Add(new PayrollRun
            {
                Id = runId,
                TenantId = tenantId,
                CompanyId = companyId,
                BranchId = branchId,
                PayrollPeriodId = periodId,
                Folio = "NOM-0001",
                RunDate = Utc(2026, 4, 15),
                Status = "draft",
                EmployeeCount = 2,
                GrossAmount = 19800m,
                DeductionsAmount = 2400m,
                NetAmount = 17400m,
                Notes = "Proceso demo de nómina quincenal.",
                CreatedBy = seedUser
            });
        }

        if (!await dbContext.PayrollRunLines.AnyAsync(x => x.PayrollRunId == runId && x.EmployeeId == employeeAdminId))
        {
            dbContext.PayrollRunLines.Add(new PayrollRunLine
            {
                Id = runLine1Id,
                TenantId = tenantId,
                CompanyId = companyId,
                PayrollRunId = runId,
                EmployeeId = employeeAdminId,
                DepartmentId = departmentAdminId,
                PositionId = positionManagerId,
                DaysPaid = 15m,
                GrossAmount = 12500m,
                DeductionsAmount = 1600m,
                NetAmount = 10900m,
                IncidentsAmount = 0m,
                Notes = "Recibo demo de administración.",
                CreatedBy = seedUser
            });
        }

        if (!await dbContext.PayrollRunLines.AnyAsync(x => x.PayrollRunId == runId && x.EmployeeId == employeeOpsId))
        {
            dbContext.PayrollRunLines.Add(new PayrollRunLine
            {
                Id = runLine2Id,
                TenantId = tenantId,
                CompanyId = companyId,
                PayrollRunId = runId,
                EmployeeId = employeeOpsId,
                DepartmentId = departmentOpsId,
                PositionId = positionAnalystId,
                DaysPaid = 15m,
                GrossAmount = 7300m,
                DeductionsAmount = 800m,
                NetAmount = 6500m,
                IncidentsAmount = 250m,
                Notes = "Recibo demo operativo.",
                CreatedBy = seedUser
            });
        }

        if (!await dbContext.EmployeeIncidents.AnyAsync(x => x.EmployeeId == employeeOpsId && x.IncidentType == "bonus"))
        {
            dbContext.EmployeeIncidents.Add(new EmployeeIncident
            {
                Id = incident1Id,
                TenantId = tenantId,
                CompanyId = companyId,
                EmployeeId = employeeOpsId,
                PayrollPeriodId = periodId,
                IncidentDate = Utc(2026, 4, 14),
                IncidentType = "bonus",
                Quantity = 1m,
                Amount = 250m,
                Notes = "Bono de productividad capturado para demo.",
                Status = "approved",
                CreatedBy = seedUser
            });
        }

        await dbContext.SaveChangesAsync();

        if (!await dbContext.PayrollRunLineDetails.AnyAsync(x => x.PayrollRunLineId == runLine1Id && x.PayrollConceptId == conceptSalaryId))
        {
            dbContext.PayrollRunLineDetails.Add(new PayrollRunLineDetail
            {
                Id = detail1Id,
                TenantId = tenantId,
                CompanyId = companyId,
                PayrollRunId = runId,
                PayrollRunLineId = runLine1Id,
                EmployeeId = employeeAdminId,
                PayrollConceptId = conceptSalaryId,
                ConceptCode = "SAL",
                ConceptName = "Sueldo base",
                ConceptType = "perception",
                SatCode = "001",
                TaxableType = "taxable",
                Quantity = 15m,
                Amount = 12500m,
                TaxableAmount = 12500m,
                ExemptAmount = 0m,
                SortOrder = 10,
                IsGenerated = true,
                Status = "applied",
                Notes = "Percepción base del periodo.",
                CreatedBy = seedUser
            });
        }

        if (!await dbContext.PayrollRunLineDetails.AnyAsync(x => x.PayrollRunLineId == runLine1Id && x.PayrollConceptId == conceptTaxId))
        {
            dbContext.PayrollRunLineDetails.Add(new PayrollRunLineDetail
            {
                Id = detail2Id,
                TenantId = tenantId,
                CompanyId = companyId,
                PayrollRunId = runId,
                PayrollRunLineId = runLine1Id,
                EmployeeId = employeeAdminId,
                PayrollConceptId = conceptTaxId,
                ConceptCode = "ISR",
                ConceptName = "ISR nómina",
                ConceptType = "deduction",
                SatCode = "002",
                TaxableType = "not_applicable",
                Quantity = 1m,
                Amount = 1600m,
                TaxableAmount = 0m,
                ExemptAmount = 0m,
                SortOrder = 90,
                IsGenerated = true,
                Status = "applied",
                Notes = "Deducción ISR demo.",
                CreatedBy = seedUser
            });
        }

        if (!await dbContext.PayrollRunLineDetails.AnyAsync(x => x.PayrollRunLineId == runLine2Id && x.PayrollConceptId == conceptSalaryId))
        {
            dbContext.PayrollRunLineDetails.Add(new PayrollRunLineDetail
            {
                Id = detail3Id,
                TenantId = tenantId,
                CompanyId = companyId,
                PayrollRunId = runId,
                PayrollRunLineId = runLine2Id,
                EmployeeId = employeeOpsId,
                PayrollConceptId = conceptSalaryId,
                ConceptCode = "SAL",
                ConceptName = "Sueldo base",
                ConceptType = "perception",
                SatCode = "001",
                TaxableType = "taxable",
                Quantity = 15m,
                Amount = 7050m,
                TaxableAmount = 7050m,
                ExemptAmount = 0m,
                SortOrder = 10,
                IsGenerated = true,
                Status = "applied",
                Notes = "Percepción base del periodo.",
                CreatedBy = seedUser
            });
        }

        if (!await dbContext.PayrollRunLineDetails.AnyAsync(x => x.PayrollRunLineId == runLine2Id && x.PayrollConceptId == conceptBonusId))
        {
            dbContext.PayrollRunLineDetails.Add(new PayrollRunLineDetail
            {
                Id = detail4Id,
                TenantId = tenantId,
                CompanyId = companyId,
                PayrollRunId = runId,
                PayrollRunLineId = runLine2Id,
                EmployeeId = employeeOpsId,
                PayrollConceptId = conceptBonusId,
                ConceptCode = "BON",
                ConceptName = "Bono productividad",
                ConceptType = "perception",
                SatCode = "019",
                TaxableType = "mixed",
                Quantity = 1m,
                Amount = 250m,
                TaxableAmount = 150m,
                ExemptAmount = 100m,
                SortOrder = 20,
                IsGenerated = true,
                Status = "applied",
                Notes = "Incidencia aplicada a la nómina demo.",
                CreatedBy = seedUser
            });
        }

        if (!await dbContext.PayrollRunLineDetails.AnyAsync(x => x.PayrollRunLineId == runLine2Id && x.PayrollConceptId == conceptTaxId))
        {
            dbContext.PayrollRunLineDetails.Add(new PayrollRunLineDetail
            {
                Id = detail5Id,
                TenantId = tenantId,
                CompanyId = companyId,
                PayrollRunId = runId,
                PayrollRunLineId = runLine2Id,
                EmployeeId = employeeOpsId,
                PayrollConceptId = conceptTaxId,
                ConceptCode = "ISR",
                ConceptName = "ISR nómina",
                ConceptType = "deduction",
                SatCode = "002",
                TaxableType = "not_applicable",
                Quantity = 1m,
                Amount = 800m,
                TaxableAmount = 0m,
                ExemptAmount = 0m,
                SortOrder = 90,
                IsGenerated = true,
                Status = "applied",
                Notes = "Deducción ISR demo.",
                CreatedBy = seedUser
            });
        }

        await dbContext.SaveChangesAsync();
    }
}
