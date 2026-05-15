using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Domain.Enums;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class SubscriptionControlEndpoints
{
    public static IEndpointRouteBuilder MapSubscriptionControlEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/subscription/control").WithTags("SubscriptionControl");

        group.MapGet("/dashboard", GetDashboardAsync);
        group.MapPost("/generate-month", GenerateMonthAsync);
        group.MapPut("/charges/{id:guid}", UpdateChargeAsync);
        group.MapPost("/charges/{id:guid}/register-payment", RegisterPaymentAsync);

        return app;
    }

    private static async Task<IResult> GetDashboardAsync(HttpContext httpContext, int? year, int? month, NanchesoftDbContext db)
    {
        if (!IsPlatformOwner(httpContext))
        {
            return Results.Forbid();
        }
        var today = DateTime.UtcNow.Date;
        var selectedYear = year.GetValueOrDefault(today.Year);
        var selectedMonth = month.GetValueOrDefault(today.Month);

        var rows = await db.SubscriptionCharges
            .AsNoTracking()
            .Where(x => x.BillingYear == selectedYear && x.BillingMonth == selectedMonth && x.IsActive)
            .OrderBy(x => x.TenantNameSnapshot)
            .ThenBy(x => x.DueDate)
            .Select(x => new SubscriptionChargeRowDto
            {
                SubscriptionChargeId = x.Id,
                TenantId = x.TenantId,
                PlanId = x.PlanId,
                TenantCode = x.TenantCodeSnapshot,
                TenantName = x.TenantNameSnapshot,
                PlanCode = x.PlanCodeSnapshot,
                PlanName = x.PlanNameSnapshot,
                ChargeMonth = x.ChargeMonth,
                BillingYear = x.BillingYear,
                BillingMonth = x.BillingMonth,
                ChargeDate = x.ChargeDate,
                DueDate = x.DueDate,
                PlanPriceMonthly = x.PlanPriceMonthly,
                DiscountAmount = x.DiscountAmount,
                SurchargeAmount = x.SurchargeAmount,
                TotalAmount = x.TotalAmount,
                PaidAmount = x.PaidAmount,
                CompensationAmount = x.CompensationAmount,
                BalanceAmount = x.BalanceAmount,
                PaidAt = x.PaidAt,
                PaymentMethod = x.PaymentMethod,
                Reference = x.Reference,
                Status = x.Status,
                Notes = x.Notes
            })
            .ToListAsync();

        var summary = new SubscriptionDashboardSummary
        {
            Year = selectedYear,
            Month = selectedMonth,
            TotalCount = rows.Count,
            PendingCount = rows.Count(x => x.Status == "pending"),
            PartialCount = rows.Count(x => x.Status == "partial"),
            PaidCount = rows.Count(x => x.Status == "paid"),
            CancelledCount = rows.Count(x => x.Status == "cancelled"),
            TotalAmount = rows.Sum(x => x.TotalAmount),
            PaidAmount = rows.Sum(x => x.PaidAmount),
            CompensationAmount = rows.Sum(x => x.CompensationAmount),
            BalanceAmount = rows.Sum(x => x.BalanceAmount),
            Rows = rows
        };

        return Results.Ok(summary);
    }

    private static async Task<IResult> GenerateMonthAsync(HttpContext httpContext, GenerateSubscriptionChargesRequest request, NanchesoftDbContext db)
    {
        if (!IsPlatformOwner(httpContext))
        {
            return Results.Forbid();
        }
        var today = DateTime.UtcNow.Date;
        var selectedYear = request.Year.GetValueOrDefault(today.Year);
        var selectedMonth = request.Month.GetValueOrDefault(today.Month);

        if (selectedMonth is < 1 or > 12)
        {
            return Results.BadRequest(new { message = "El mes debe estar entre 1 y 12." });
        }

        var chargeDate = EnsureUtcDate(request.ChargeDate) ?? new DateTime(selectedYear, selectedMonth, 1, 0, 0, 0, DateTimeKind.Utc);
        var dueDate = EnsureUtcDate(request.DueDate) ?? new DateTime(selectedYear, selectedMonth, Math.Min(5, DateTime.DaysInMonth(selectedYear, selectedMonth)), 0, 0, 0, DateTimeKind.Utc);
        var monthKey = $"{selectedYear:0000}-{selectedMonth:00}";

        var tenantFilter = request.TenantId.HasValue && request.TenantId.Value != Guid.Empty ? request.TenantId.Value : (Guid?)null;

        var tenants = await db.Tenants
            .AsNoTracking()
            .Include(x => x.Plan)
            .Where(x => x.IsActive && x.Status == TenantStatus.Active)
            .Where(x => !tenantFilter.HasValue || x.Id == tenantFilter.Value)
            .OrderBy(x => x.Name)
            .ToListAsync();

        var existingTenantIds = await db.SubscriptionCharges
            .AsNoTracking()
            .Where(x => x.BillingYear == selectedYear && x.BillingMonth == selectedMonth)
            .Select(x => x.TenantId)
            .ToListAsync();

        var created = 0;

        foreach (var tenant in tenants)
        {
            if (tenant.Plan is null || existingTenantIds.Contains(tenant.Id))
            {
                continue;
            }

            var total = tenant.Plan.PriceMonthly < 0m ? 0m : tenant.Plan.PriceMonthly;

            db.SubscriptionCharges.Add(new SubscriptionCharge
            {
                TenantId = tenant.Id,
                PlanId = tenant.PlanId,
                ChargeMonth = monthKey,
                BillingYear = selectedYear,
                BillingMonth = selectedMonth,
                TenantCodeSnapshot = tenant.Code,
                TenantNameSnapshot = tenant.Name,
                PlanCodeSnapshot = tenant.Plan.Code,
                PlanNameSnapshot = tenant.Plan.Name,
                ChargeDate = chargeDate,
                DueDate = dueDate,
                PlanPriceMonthly = tenant.Plan.PriceMonthly,
                DiscountAmount = 0m,
                SurchargeAmount = 0m,
                TotalAmount = total,
                PaidAmount = 0m,
                CompensationAmount = 0m,
                BalanceAmount = total,
                Status = total == 0m ? "paid" : "pending",
                Notes = (request.Notes ?? string.Empty).Trim(),
                CreatedBy = "subscription-control"
            });

            created++;
        }

        await db.SaveChangesAsync();

        return Results.Ok(new
        {
            success = true,
            created,
            year = selectedYear,
            month = selectedMonth
        });
    }

    private static async Task<IResult> UpdateChargeAsync(HttpContext httpContext, Guid id, UpdateSubscriptionChargeRequest request, NanchesoftDbContext db)
    {
        if (!IsPlatformOwner(httpContext))
        {
            return Results.Forbid();
        }
        var entity = await db.SubscriptionCharges.FirstOrDefaultAsync(x => x.Id == id && x.IsActive);
        if (entity is null)
        {
            return Results.NotFound(new { message = "No se encontró el cargo de suscripción." });
        }

        entity.ChargeDate = EnsureUtcDate(request.ChargeDate) ?? entity.ChargeDate;
        entity.DueDate = EnsureUtcDate(request.DueDate) ?? entity.DueDate;
        entity.PlanPriceMonthly = request.PlanPriceMonthly ?? entity.PlanPriceMonthly;
        entity.DiscountAmount = Math.Max(0m, request.DiscountAmount ?? entity.DiscountAmount);
        entity.SurchargeAmount = Math.Max(0m, request.SurchargeAmount ?? entity.SurchargeAmount);
        entity.Reference = CleanText(request.Reference, entity.Reference);
        entity.Notes = CleanText(request.Notes, entity.Notes);

        RecalculateAmounts(entity);

        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "subscription-control";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> RegisterPaymentAsync(HttpContext httpContext, Guid id, RegisterSubscriptionPaymentRequest request, NanchesoftDbContext db)
    {
        if (!IsPlatformOwner(httpContext))
        {
            return Results.Forbid();
        }
        var entity = await db.SubscriptionCharges.FirstOrDefaultAsync(x => x.Id == id && x.IsActive);
        if (entity is null)
        {
            return Results.NotFound(new { message = "No se encontró el cargo de suscripción." });
        }

        entity.PaidAt = EnsureUtcDate(request.PaidAt) ?? DateTime.UtcNow.Date;
        entity.PaymentMethod = NormalizePaymentMethod(request.PaymentMethod);
        entity.Reference = CleanText(request.Reference, entity.Reference);
        entity.Notes = CleanText(request.Notes, entity.Notes);
        entity.DiscountAmount = Math.Max(0m, request.DiscountAmount ?? entity.DiscountAmount);
        entity.CompensationAmount = Math.Max(0m, request.CompensationAmount);
        entity.PaidAmount = Math.Max(0m, request.ReceivedAmount) + entity.CompensationAmount;

        RecalculateAmounts(entity);

        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "subscription-control";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }


    private static bool IsPlatformOwner(HttpContext httpContext)
    {
        if (httpContext.Request.Headers.TryGetValue("X-Nanchesoft-Platform-Owner", out var values)
            && bool.TryParse(values.ToString(), out var parsed))
        {
            return parsed;
        }

        return false;
    }

    private static void RecalculateAmounts(SubscriptionCharge entity)
    {
        entity.TotalAmount = Math.Max(0m, entity.PlanPriceMonthly - entity.DiscountAmount + entity.SurchargeAmount);
        entity.BalanceAmount = Math.Max(0m, entity.TotalAmount - entity.PaidAmount);

        if (entity.BalanceAmount <= 0m)
        {
            entity.Status = "paid";
        }
        else if (entity.PaidAmount > 0m)
        {
            entity.Status = "partial";
        }
        else
        {
            entity.Status = "pending";
        }

        if (entity.Status == "paid" && !entity.PaidAt.HasValue)
        {
            entity.PaidAt = DateTime.UtcNow.Date;
        }
    }

    private static string NormalizePaymentMethod(string? value)
    {
        var normalized = (value ?? string.Empty).Trim().ToUpperInvariant();
        return normalized switch
        {
            "EFECTIVO" => "EFECTIVO",
            "DEPOSITO" => "DEPOSITO",
            "DEPÓSITO" => "DEPOSITO",
            "TRANSFERENCIA" => "TRANSFERENCIA",
            "TARJETA" => "TARJETA",
            "COMPENSACION" => "COMPENSACION",
            "COMPENSACIÓN" => "COMPENSACION",
            _ => "OTRO"
        };
    }

    private static string CleanText(string? value, string fallback = "")
        => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static DateTime? EnsureUtcDate(DateTime? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        var date = value.Value.Date;
        return date.Kind == DateTimeKind.Utc
            ? date
            : DateTime.SpecifyKind(date, DateTimeKind.Utc);
    }
}

public sealed class GenerateSubscriptionChargesRequest
{
    public Guid? TenantId { get; set; }
    public int? Year { get; set; }
    public int? Month { get; set; }
    public DateTime? ChargeDate { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Notes { get; set; }
}

public sealed class UpdateSubscriptionChargeRequest
{
    public DateTime? ChargeDate { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal? PlanPriceMonthly { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal? SurchargeAmount { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
}

public sealed class RegisterSubscriptionPaymentRequest
{
    public DateTime? PaidAt { get; set; }
    public string? PaymentMethod { get; set; }
    public decimal ReceivedAmount { get; set; }
    public decimal CompensationAmount { get; set; }
    public decimal? DiscountAmount { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
}

public sealed class SubscriptionDashboardSummary
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int TotalCount { get; set; }
    public int PendingCount { get; set; }
    public int PartialCount { get; set; }
    public int PaidCount { get; set; }
    public int CancelledCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal CompensationAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public List<SubscriptionChargeRowDto> Rows { get; set; } = new();
}

public sealed class SubscriptionChargeRowDto
{
    public Guid SubscriptionChargeId { get; set; }
    public Guid TenantId { get; set; }
    public Guid PlanId { get; set; }
    public string TenantCode { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public string PlanCode { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public string ChargeMonth { get; set; } = string.Empty;
    public int BillingYear { get; set; }
    public int BillingMonth { get; set; }
    public DateTime ChargeDate { get; set; }
    public DateTime DueDate { get; set; }
    public decimal PlanPriceMonthly { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal SurchargeAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal CompensationAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public DateTime? PaidAt { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}
