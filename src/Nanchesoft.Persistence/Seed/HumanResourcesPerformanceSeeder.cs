using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Persistence.Seed;

public static class HumanResourcesPerformanceSeeder
{
    public static async Task SeedAsync(NanchesoftDbContext dbContext)
    {
        var company = await dbContext.Companies.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        if (company is null)
            return;

        var branch = await dbContext.Branches.Where(x => x.CompanyId == company.Id).OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        var department = await dbContext.Departments.Where(x => x.CompanyId == company.Id).OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        var position = await dbContext.Positions.Where(x => x.CompanyId == company.Id).OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();

        var employees = await dbContext.Employees.Where(x => x.CompanyId == company.Id).OrderBy(x => x.CreatedAt).Take(2).ToListAsync();
        if (department is null || position is null || employees.Count == 0)
            return;

        var employee = employees[0];
        var successor = employees.Count > 1 ? employees[1] : employees[0];
        var now = DateTime.UtcNow;

        var review = await dbContext.EmployeePerformanceReviews.FirstOrDefaultAsync(x => x.CompanyId == company.Id && x.EmployeeId == employee.Id && x.ReviewCode == "PERF-2026-01");
        if (review is null)
        {
            review = new EmployeePerformanceReview
            {
                TenantId = company.TenantId,
                CompanyId = company.Id,
                BranchId = branch?.Id,
                EmployeeId = employee.Id,
                DepartmentId = department.Id,
                PositionId = position.Id,
                ReviewCode = "PERF-2026-01",
                ReviewCycle = "annual_2026",
                PeriodStart = now.AddMonths(-3),
                PeriodEnd = now,
                ReviewDate = now,
                ReviewerName = "Comité RH",
                Score = 91m,
                CalibrationScore = 89m,
                GoalCompletionPercent = 94m,
                PotentialLevel = "high",
                Status = "calibrated",
                Notes = "Evaluación semilla enterprise para modelo de desempeño.",
                IsActive = true,
                CreatedBy = "seed"
            };
            dbContext.EmployeePerformanceReviews.Add(review);
            await dbContext.SaveChangesAsync();
        }

        var competency = await dbContext.EmployeeCompetencyAssessments.FirstOrDefaultAsync(x => x.CompanyId == company.Id && x.EmployeeId == employee.Id && x.AssessmentCode == "COMP-2026-01" && x.CompetencyCode == "LEADERSHIP");
        if (competency is null)
        {
            competency = new EmployeeCompetencyAssessment
            {
                TenantId = company.TenantId,
                CompanyId = company.Id,
                BranchId = branch?.Id,
                EmployeeId = employee.Id,
                DepartmentId = department.Id,
                PositionId = position.Id,
                AssessmentCode = "COMP-2026-01",
                CompetencyCode = "LEADERSHIP",
                CompetencyName = "Liderazgo",
                ExpectedLevel = 4,
                AchievedLevel = 3,
                GapLevel = 1,
                AssessedAt = now,
                AssessorName = "Capital Humano",
                DevelopmentAction = "Asignar coaching y shadowing trimestral.",
                Status = "development_plan",
                Notes = "Brecha controlada con plan de formación.",
                IsActive = true,
                CreatedBy = "seed"
            };
            dbContext.EmployeeCompetencyAssessments.Add(competency);
            await dbContext.SaveChangesAsync();
        }

        var succession = await dbContext.SuccessionPlanRecords.FirstOrDefaultAsync(x => x.CompanyId == company.Id && x.PlanCode == "SUC-2026-01");
        if (succession is null)
        {
            succession = new SuccessionPlanRecord
            {
                TenantId = company.TenantId,
                CompanyId = company.Id,
                BranchId = branch?.Id,
                PositionId = position.Id,
                IncumbentEmployeeId = employee.Id,
                SuccessorEmployeeId = successor.Id,
                PlanCode = "SUC-2026-01",
                Criticality = "high",
                ReadinessLevel = "ready_1_year",
                RiskOfLoss = "medium",
                ReviewDate = now,
                TargetReadyDate = now.AddMonths(12),
                IsNominationApproved = true,
                DevelopmentPlan = "Rotación controlada, mentoring y evaluación 9-box.",
                Status = "approved",
                Notes = "Plan semilla para continuidad operativa de posición clave.",
                IsActive = true,
                CreatedBy = "seed"
            };
            dbContext.SuccessionPlanRecords.Add(succession);
            await dbContext.SaveChangesAsync();
        }
    }
}
