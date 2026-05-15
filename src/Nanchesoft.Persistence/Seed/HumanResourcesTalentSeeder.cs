using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Persistence.Seed;

public static class HumanResourcesTalentSeeder
{
    public static async Task SeedAsync(NanchesoftDbContext dbContext)
    {
        var company = await dbContext.Companies.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        if (company is null)
            return;

        var branch = await dbContext.Branches.Where(x => x.CompanyId == company.Id).OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        var department = await dbContext.Departments.Where(x => x.CompanyId == company.Id).OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        var position = await dbContext.Positions.Where(x => x.CompanyId == company.Id).OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        var employee = await dbContext.Employees.Where(x => x.CompanyId == company.Id).OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();

        if (department is null || position is null || employee is null)
            return;

        var now = DateTime.UtcNow;

        var vacancy = await dbContext.RecruitmentVacancies.FirstOrDefaultAsync(x => x.CompanyId == company.Id && x.VacancyCode == "VAC-RH-001");
        if (vacancy is null)
        {
            vacancy = new RecruitmentVacancy
            {
                TenantId = company.TenantId,
                CompanyId = company.Id,
                BranchId = branch?.Id,
                DepartmentId = department.Id,
                PositionId = position.Id,
                VacancyCode = "VAC-RH-001",
                Title = $"Vacante {position.Name}",
                EmploymentType = "full_time",
                OpenDate = now,
                CloseDate = now.AddDays(25),
                Headcount = 1,
                SalaryMin = 12000m,
                SalaryMax = 18000m,
                HiringManager = string.Join(" ", new[] { employee.FirstName, employee.MiddleName, employee.LastName }.Where(x => !string.IsNullOrWhiteSpace(x))),
                Priority = "high",
                Status = "open",
                Notes = "Vacante semilla enterprise para reclutamiento.",
                IsActive = true,
                CreatedBy = "seed"
            };
            dbContext.RecruitmentVacancies.Add(vacancy);
            await dbContext.SaveChangesAsync();
        }

        var candidate = await dbContext.CandidateApplications.FirstOrDefaultAsync(x => x.CompanyId == company.Id && x.CandidateCode == "CAND-0001");
        if (candidate is null)
        {
            candidate = new CandidateApplication
            {
                TenantId = company.TenantId,
                CompanyId = company.Id,
                BranchId = branch?.Id,
                RecruitmentVacancyId = vacancy.Id,
                CandidateCode = "CAND-0001",
                FullName = "Andrea Solís Mendoza",
                Email = "andrea.solis@nanchesoft.demo",
                Phone = "4770000001",
                Source = "linkedin",
                AppliedAt = now.AddDays(-4),
                Stage = "interview",
                Score = 88m,
                OfferAmount = 16500m,
                CvFileName = "andrea_solis_cv.pdf",
                CvFilePath = "/hr/candidates/andrea_solis_cv.pdf",
                Status = "in_process",
                Notes = "Candidata destacada para continuidad del pipeline.",
                IsActive = true,
                CreatedBy = "seed"
            };
            dbContext.CandidateApplications.Add(candidate);
            await dbContext.SaveChangesAsync();
        }

        var checklist = await dbContext.OnboardingChecklistRecords.FirstOrDefaultAsync(x => x.CompanyId == company.Id && x.EmployeeId == employee.Id && x.ChecklistCode == "ONB-0001");
        if (checklist is null)
        {
            checklist = new OnboardingChecklistRecord
            {
                TenantId = company.TenantId,
                CompanyId = company.Id,
                BranchId = branch?.Id,
                EmployeeId = employee.Id,
                CandidateApplicationId = candidate.Id,
                ChecklistCode = "ONB-0001",
                ChecklistName = "Onboarding administrativo inicial",
                PlannedDate = now.AddDays(1),
                CompletedAt = null,
                ResponsibleArea = "human_resources",
                Status = "in_progress",
                CompletionPercent = 66m,
                AssetsAssigned = true,
                CredentialsIssued = true,
                InductionCompleted = false,
                Notes = "Checklist semilla para proceso de ingreso controlado.",
                IsActive = true,
                CreatedBy = "seed"
            };
            dbContext.OnboardingChecklistRecords.Add(checklist);
            await dbContext.SaveChangesAsync();
        }
    }
}
