using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class FinancePhase2Endpoints
{
    public static IEndpointRouteBuilder MapFinancePhase2Endpoints(this IEndpointRouteBuilder app)
    {
        MapMovementTypeEndpoints(app);
        MapConceptEndpoints(app);
        MapCheckBookEndpoints(app);
        MapCheckEndpoints(app);
        MapFinancialIndicatorsEndpoints(app);
        MapFinancialProjectionEndpoints(app);
        return app;
    }

    // ===================== Tipos de movimiento =====================
    private static void MapMovementTypeEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/finance/movement-types").WithTags("FinanceMovementTypes");

        group.MapGet("/", async (NanchesoftDbContext db) =>
        {
            var rows = await db.FinanceMovementTypes.AsNoTracking()
                .OrderBy(x => x.Direction).ThenBy(x => x.Name)
                .Select(x => new FinanceMovementTypeDto
                {
                    Id = x.Id,
                    TenantId = x.TenantId,
                    CompanyId = x.CompanyId,
                    Code = x.Code,
                    Name = x.Name,
                    Direction = x.Direction,
                    Nature = x.Nature,
                    AffectsBalance = x.AffectsBalance,
                    IsSystem = x.IsSystem,
                    AccountingAccountId = x.AccountingAccountId,
                    Notes = x.Notes,
                    IsActive = x.IsActive
                }).ToListAsync();
            return Results.Ok(rows);
        });

        group.MapPost("/", async (FinanceMovementTypeDto request, NanchesoftDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return Results.BadRequest(new { message = "El nombre es obligatorio." });

            var tenant = await db.Tenants.AsNoTracking().FirstOrDefaultAsync();
            if (tenant is null) return Results.BadRequest(new { message = "No hay tenant configurado." });

            var entity = new FinanceMovementType
            {
                TenantId = tenant.Id,
                CompanyId = request.CompanyId,
                Code = (request.Code ?? string.Empty).Trim(),
                Name = request.Name.Trim(),
                Direction = NormalizeDirection(request.Direction),
                Nature = (request.Nature ?? string.Empty).Trim().ToLowerInvariant(),
                AffectsBalance = request.AffectsBalance,
                IsSystem = false,
                AccountingAccountId = request.AccountingAccountId,
                Notes = (request.Notes ?? string.Empty).Trim(),
                IsActive = request.IsActive,
                CreatedBy = "web-api"
            };
            db.FinanceMovementTypes.Add(entity);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true, id = entity.Id });
        });

        group.MapPut("/{id:guid}", async (Guid id, FinanceMovementTypeDto request, NanchesoftDbContext db) =>
        {
            var entity = await db.FinanceMovementTypes.FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return Results.NotFound(new { message = "No se encontró el tipo de movimiento." });
            if (entity.IsSystem && !string.Equals(entity.Code, request.Code, StringComparison.OrdinalIgnoreCase))
                return Results.BadRequest(new { message = "No se puede modificar el código de un tipo del sistema." });

            entity.Code = (request.Code ?? entity.Code).Trim();
            entity.Name = (request.Name ?? entity.Name).Trim();
            entity.Direction = NormalizeDirection(request.Direction);
            entity.Nature = (request.Nature ?? string.Empty).Trim().ToLowerInvariant();
            entity.AffectsBalance = request.AffectsBalance;
            entity.AccountingAccountId = request.AccountingAccountId;
            entity.Notes = (request.Notes ?? string.Empty).Trim();
            entity.IsActive = request.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = "web-api";
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });

        group.MapDelete("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.FinanceMovementTypes.FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return Results.NotFound(new { message = "No se encontró el tipo de movimiento." });
            if (entity.IsSystem)
                return Results.BadRequest(new { message = "No se puede eliminar un tipo del sistema." });
            db.FinanceMovementTypes.Remove(entity);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });
    }

    // ===================== Conceptos financieros =====================
    private static void MapConceptEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/finance/concepts").WithTags("FinanceConcepts");

        group.MapGet("/", async (NanchesoftDbContext db) =>
        {
            var rows = await db.FinanceConcepts.AsNoTracking()
                .OrderBy(x => x.Category).ThenBy(x => x.Name)
                .Select(x => new FinanceConceptDto
                {
                    Id = x.Id,
                    TenantId = x.TenantId,
                    CompanyId = x.CompanyId,
                    Code = x.Code,
                    Name = x.Name,
                    Category = x.Category,
                    Direction = x.Direction,
                    AccountingAccountId = x.AccountingAccountId,
                    IsSystem = x.IsSystem,
                    Notes = x.Notes,
                    IsActive = x.IsActive
                }).ToListAsync();
            return Results.Ok(rows);
        });

        group.MapPost("/", async (FinanceConceptDto request, NanchesoftDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return Results.BadRequest(new { message = "El nombre es obligatorio." });
            var tenant = await db.Tenants.AsNoTracking().FirstOrDefaultAsync();
            if (tenant is null) return Results.BadRequest(new { message = "No hay tenant configurado." });
            var entity = new FinanceConcept
            {
                TenantId = tenant.Id,
                CompanyId = request.CompanyId,
                Code = (request.Code ?? string.Empty).Trim(),
                Name = request.Name.Trim(),
                Category = (request.Category ?? "other").Trim().ToLowerInvariant(),
                Direction = NormalizeDirection(request.Direction),
                AccountingAccountId = request.AccountingAccountId,
                Notes = (request.Notes ?? string.Empty).Trim(),
                IsActive = request.IsActive,
                CreatedBy = "web-api"
            };
            db.FinanceConcepts.Add(entity);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true, id = entity.Id });
        });

        group.MapPut("/{id:guid}", async (Guid id, FinanceConceptDto request, NanchesoftDbContext db) =>
        {
            var entity = await db.FinanceConcepts.FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return Results.NotFound(new { message = "No se encontró el concepto." });
            if (entity.IsSystem && !string.Equals(entity.Code, request.Code, StringComparison.OrdinalIgnoreCase))
                return Results.BadRequest(new { message = "No se puede modificar el código de un concepto del sistema." });
            entity.Code = (request.Code ?? entity.Code).Trim();
            entity.Name = (request.Name ?? entity.Name).Trim();
            entity.Category = (request.Category ?? entity.Category).Trim().ToLowerInvariant();
            entity.Direction = NormalizeDirection(request.Direction);
            entity.AccountingAccountId = request.AccountingAccountId;
            entity.Notes = (request.Notes ?? string.Empty).Trim();
            entity.IsActive = request.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = "web-api";
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });

        group.MapDelete("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.FinanceConcepts.FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return Results.NotFound(new { message = "No se encontró el concepto." });
            if (entity.IsSystem)
                return Results.BadRequest(new { message = "No se puede eliminar un concepto del sistema." });
            db.FinanceConcepts.Remove(entity);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });
    }

    // ===================== Chequeras =====================
    private static void MapCheckBookEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/finance/check-books").WithTags("FinanceCheckBooks");

        group.MapGet("/", async (Guid? bankAccountId, NanchesoftDbContext db) =>
        {
            var query = db.CheckBooks.AsNoTracking().AsQueryable();
            if (bankAccountId.HasValue) query = query.Where(x => x.BankAccountId == bankAccountId.Value);
            var rows = await query.OrderBy(x => x.Code).Select(x => new CheckBookDto
            {
                Id = x.Id,
                CompanyId = x.CompanyId,
                BankAccountId = x.BankAccountId,
                Code = x.Code,
                Name = x.Name,
                Series = x.Series,
                FolioStart = x.FolioStart,
                FolioEnd = x.FolioEnd,
                NextFolio = x.NextFolio,
                Notes = x.Notes,
                IsActive = x.IsActive
            }).ToListAsync();
            return Results.Ok(rows);
        });

        group.MapPost("/", async (CheckBookDto request, NanchesoftDbContext db) =>
        {
            var account = await db.BankAccounts.FirstOrDefaultAsync(x => x.Id == request.BankAccountId);
            if (account is null) return Results.BadRequest(new { message = "Cuenta bancaria no encontrada." });
            if (request.FolioStart <= 0 || request.FolioEnd < request.FolioStart)
                return Results.BadRequest(new { message = "Rango de folios inválido." });
            var entity = new CheckBook
            {
                TenantId = account.TenantId,
                CompanyId = account.CompanyId,
                BankAccountId = account.Id,
                Code = (request.Code ?? string.Empty).Trim(),
                Name = (request.Name ?? string.Empty).Trim(),
                Series = (request.Series ?? string.Empty).Trim(),
                FolioStart = request.FolioStart,
                FolioEnd = request.FolioEnd,
                NextFolio = request.NextFolio <= 0 ? request.FolioStart : request.NextFolio,
                Notes = (request.Notes ?? string.Empty).Trim(),
                IsActive = request.IsActive,
                CreatedBy = "web-api"
            };
            db.CheckBooks.Add(entity);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true, id = entity.Id });
        });

        group.MapPut("/{id:guid}", async (Guid id, CheckBookDto request, NanchesoftDbContext db) =>
        {
            var entity = await db.CheckBooks.FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return Results.NotFound(new { message = "No se encontró la chequera." });
            entity.Code = (request.Code ?? entity.Code).Trim();
            entity.Name = (request.Name ?? entity.Name).Trim();
            entity.Series = (request.Series ?? entity.Series).Trim();
            entity.FolioStart = request.FolioStart;
            entity.FolioEnd = request.FolioEnd;
            entity.NextFolio = request.NextFolio;
            entity.Notes = (request.Notes ?? entity.Notes).Trim();
            entity.IsActive = request.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = "web-api";
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });

        group.MapDelete("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.CheckBooks.FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return Results.NotFound(new { message = "No se encontró la chequera." });
            var inUse = await db.Checks.AnyAsync(x => x.CheckBookId == id);
            if (inUse) return Results.BadRequest(new { message = "No se puede eliminar una chequera con cheques emitidos." });
            db.CheckBooks.Remove(entity);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });
    }

    // ===================== Cheques =====================
    private static void MapCheckEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/finance/checks").WithTags("FinanceChecks");

        group.MapGet("/", async (Guid? bankAccountId, string? status, DateTime? from, DateTime? to, NanchesoftDbContext db) =>
        {
            var query = db.Checks.AsNoTracking().AsQueryable();
            if (bankAccountId.HasValue) query = query.Where(x => x.BankAccountId == bankAccountId.Value);
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status);
            if (from.HasValue) query = query.Where(x => x.IssueDate >= from.Value.Date);
            if (to.HasValue) query = query.Where(x => x.IssueDate <= to.Value.Date.AddDays(1).AddTicks(-1));
            var rows = await query.OrderByDescending(x => x.IssueDate).ThenByDescending(x => x.CreatedAt)
                .Select(x => new CheckListRowDto
                {
                    Id = x.Id,
                    CompanyId = x.CompanyId,
                    BankAccountId = x.BankAccountId,
                    CheckBookId = x.CheckBookId,
                    SupplierId = x.SupplierId,
                    EmployeeId = x.EmployeeId,
                    Folio = x.Folio,
                    IssueDate = x.IssueDate,
                    PostingDate = x.PostingDate,
                    CashedDate = x.CashedDate,
                    BeneficiaryType = x.BeneficiaryType,
                    BeneficiaryName = x.BeneficiaryName,
                    Amount = x.Amount,
                    Concept = x.Concept,
                    Reference = x.Reference,
                    Status = x.Status,
                    IsPrinted = x.IsPrinted,
                    BankMovementId = x.BankMovementId,
                    Notes = x.Notes,
                    IsActive = x.IsActive
                }).ToListAsync();
            return Results.Ok(rows);
        });

        group.MapGet("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.Checks.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return Results.NotFound(new { message = "No se encontró el cheque." });
            return Results.Ok(MapToRow(entity));
        });

        group.MapPost("/", async (CheckSaveRequestDto request, NanchesoftDbContext db) =>
        {
            var account = await db.BankAccounts.FirstOrDefaultAsync(x => x.Id == request.BankAccountId);
            if (account is null) return Results.BadRequest(new { message = "Cuenta bancaria no encontrada." });
            if (request.Amount <= 0m) return Results.BadRequest(new { message = "El importe debe ser mayor a cero." });

            var folio = (request.Folio ?? string.Empty).Trim();
            if (request.CheckBookId.HasValue)
            {
                var book = await db.CheckBooks.FirstOrDefaultAsync(x => x.Id == request.CheckBookId.Value);
                if (book is null) return Results.BadRequest(new { message = "Chequera no encontrada." });
                if (string.IsNullOrWhiteSpace(folio))
                {
                    folio = $"{book.Series}{book.NextFolio:D6}".Trim();
                }
                if (book.NextFolio > book.FolioEnd)
                    return Results.BadRequest(new { message = "La chequera no tiene folios disponibles." });
                book.NextFolio++;
                book.UpdatedAt = DateTime.UtcNow;
                book.UpdatedBy = "web-api";
            }
            if (string.IsNullOrWhiteSpace(folio))
                folio = $"CHK-{DateTime.UtcNow:yyyyMMddHHmmss}";

            var beneficiary = (request.BeneficiaryName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(beneficiary) && request.SupplierId.HasValue)
                beneficiary = (await db.Suppliers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.SupplierId.Value))?.Name ?? string.Empty;
            if (string.IsNullOrWhiteSpace(beneficiary) && request.EmployeeId.HasValue)
            {
                var emp = await db.Employees.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.EmployeeId.Value);
                if (emp is not null) beneficiary = $"{emp.FirstName} {emp.LastName}".Trim();
            }

            var entity = new Check
            {
                TenantId = account.TenantId,
                CompanyId = account.CompanyId,
                BankAccountId = account.Id,
                CheckBookId = request.CheckBookId,
                SupplierId = request.SupplierId,
                EmployeeId = request.EmployeeId,
                Folio = folio,
                IssueDate = request.IssueDate?.Date ?? DateTime.UtcNow.Date,
                BeneficiaryType = NormalizeBeneficiaryType(request.BeneficiaryType, request.SupplierId, request.EmployeeId),
                BeneficiaryName = beneficiary,
                Amount = request.Amount,
                Concept = (request.Concept ?? string.Empty).Trim(),
                Reference = (request.Reference ?? string.Empty).Trim(),
                Status = "pending",
                Notes = (request.Notes ?? string.Empty).Trim(),
                IsActive = true,
                CreatedBy = "web-api"
            };
            db.Checks.Add(entity);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true, id = entity.Id, folio = entity.Folio });
        });

        group.MapPut("/{id:guid}", async (Guid id, CheckSaveRequestDto request, NanchesoftDbContext db) =>
        {
            var entity = await db.Checks.FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return Results.NotFound(new { message = "No se encontró el cheque." });
            if (entity.Status is "cashed" or "cancelled" or "issued")
                return Results.BadRequest(new { message = "Solo se pueden editar cheques pendientes." });
            if (request.Amount <= 0m) return Results.BadRequest(new { message = "El importe debe ser mayor a cero." });

            entity.IssueDate = request.IssueDate?.Date ?? entity.IssueDate;
            entity.SupplierId = request.SupplierId;
            entity.EmployeeId = request.EmployeeId;
            entity.BeneficiaryType = NormalizeBeneficiaryType(request.BeneficiaryType, request.SupplierId, request.EmployeeId);
            entity.BeneficiaryName = (request.BeneficiaryName ?? entity.BeneficiaryName).Trim();
            entity.Amount = request.Amount;
            entity.Concept = (request.Concept ?? entity.Concept).Trim();
            entity.Reference = (request.Reference ?? entity.Reference).Trim();
            entity.Notes = (request.Notes ?? entity.Notes).Trim();
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = "web-api";
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });

        // Emitir cheque (genera movimiento bancario y afecta saldo)
        group.MapPost("/{id:guid}/issue", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.Checks.FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return Results.NotFound(new { message = "No se encontró el cheque." });
            if (entity.Status != "pending" && entity.Status != "printed")
                return Results.BadRequest(new { message = "Solo cheques pendientes o impresos pueden emitirse." });

            var account = await db.BankAccounts.FirstOrDefaultAsync(x => x.Id == entity.BankAccountId);
            if (account is null) return Results.BadRequest(new { message = "Cuenta bancaria no encontrada." });
            if (account.CurrentBalance < entity.Amount)
                return Results.BadRequest(new { message = "Saldo insuficiente en la cuenta bancaria." });

            account.CurrentBalance -= entity.Amount;
            account.UpdatedAt = DateTime.UtcNow;
            account.UpdatedBy = "web-api";

            var movement = new BankMovement
            {
                TenantId = account.TenantId,
                CompanyId = account.CompanyId,
                BankAccountId = account.Id,
                MovementDate = entity.IssueDate,
                MovementType = "withdrawal",
                DocumentType = "check",
                DocumentId = entity.Id,
                Reference = $"Cheque {entity.Folio} - {entity.BeneficiaryName}".Trim(),
                AmountIn = 0m,
                AmountOut = entity.Amount,
                BalanceAfter = account.CurrentBalance,
                CreatedBy = "web-api"
            };
            db.BankMovements.Add(movement);

            entity.Status = "issued";
            entity.PostingDate = DateTime.UtcNow.Date;
            entity.BankMovementId = movement.Id;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = "web-api";

            await db.SaveChangesAsync();
            return Results.Ok(new { success = true, movementId = movement.Id });
        });

        // Marcar como cobrado
        group.MapPost("/{id:guid}/cash", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.Checks.FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return Results.NotFound(new { message = "No se encontró el cheque." });
            if (entity.Status != "issued")
                return Results.BadRequest(new { message = "Solo cheques emitidos pueden marcarse como cobrados." });
            entity.Status = "cashed";
            entity.CashedDate = DateTime.UtcNow.Date;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = "web-api";
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });

        // Cancelar (si está emitido, reversa el movimiento)
        group.MapPost("/{id:guid}/cancel", async (Guid id, CheckCancelRequestDto? request, NanchesoftDbContext db) =>
        {
            var entity = await db.Checks.FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return Results.NotFound(new { message = "No se encontró el cheque." });
            if (entity.Status == "cancelled") return Results.BadRequest(new { message = "El cheque ya está cancelado." });
            if (entity.Status == "cashed") return Results.BadRequest(new { message = "No se puede cancelar un cheque ya cobrado." });

            if (entity.Status == "issued" && entity.BankMovementId.HasValue)
            {
                var movement = await db.BankMovements.FirstOrDefaultAsync(x => x.Id == entity.BankMovementId.Value);
                if (movement is not null)
                {
                    var account = await db.BankAccounts.FirstOrDefaultAsync(x => x.Id == movement.BankAccountId);
                    if (account is not null)
                    {
                        account.CurrentBalance += movement.AmountOut;
                        account.UpdatedAt = DateTime.UtcNow;
                        account.UpdatedBy = "web-api";
                    }
                    db.BankMovements.Remove(movement);
                }
                entity.BankMovementId = null;
            }
            entity.Status = "cancelled";
            entity.CancelDate = DateTime.UtcNow.Date;
            entity.Notes = string.IsNullOrWhiteSpace(request?.Reason)
                ? entity.Notes
                : $"{entity.Notes}\nCancelado: {request.Reason}".Trim();
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = "web-api";
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });

        // Marcar como impreso
        group.MapPost("/{id:guid}/print", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.Checks.FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return Results.NotFound(new { message = "No se encontró el cheque." });
            entity.IsPrinted = true;
            entity.PrintedAt = DateTime.UtcNow;
            if (entity.Status == "pending") entity.Status = "printed";
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = "web-api";
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });

        group.MapGet("/summary", async (Guid? bankAccountId, NanchesoftDbContext db) =>
        {
            var query = db.Checks.AsNoTracking();
            if (bankAccountId.HasValue) query = query.Where(x => x.BankAccountId == bankAccountId.Value);
            var pending = await query.Where(x => x.Status == "pending" || x.Status == "printed").CountAsync();
            var pendingAmount = await query.Where(x => x.Status == "pending" || x.Status == "printed").SumAsync(x => (decimal?)x.Amount) ?? 0m;
            var issued = await query.Where(x => x.Status == "issued").CountAsync();
            var issuedAmount = await query.Where(x => x.Status == "issued").SumAsync(x => (decimal?)x.Amount) ?? 0m;
            var cashed = await query.Where(x => x.Status == "cashed").CountAsync();
            var cashedAmount = await query.Where(x => x.Status == "cashed").SumAsync(x => (decimal?)x.Amount) ?? 0m;
            var cancelled = await query.Where(x => x.Status == "cancelled").CountAsync();
            return Results.Ok(new CheckSummaryDto
            {
                PendingCount = pending, PendingAmount = pendingAmount,
                IssuedCount = issued, IssuedAmount = issuedAmount,
                CashedCount = cashed, CashedAmount = cashedAmount,
                CancelledCount = cancelled
            });
        });
    }

    // ===================== Indicadores Financieros =====================
    private static void MapFinancialIndicatorsEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/finance/indicators").WithTags("FinanceIndicators");

        group.MapGet("/", async (NanchesoftDbContext db) =>
        {
            var cash = await db.CashAccounts.AsNoTracking().SumAsync(x => (decimal?)x.CurrentBalance) ?? 0m;
            var bank = await db.BankAccounts.AsNoTracking().SumAsync(x => (decimal?)x.CurrentBalance) ?? 0m;
            var receivable = await db.AccountsReceivableAccounts.AsNoTracking().SumAsync(x => (decimal?)x.CurrentBalance) ?? 0m;
            var payable = await ComputePayableBalanceAsync(db);
            var inventoryValue = await db.StockBalances.AsNoTracking().SumAsync(x => (decimal?)(x.QuantityOnHand * x.AverageCost)) ?? 0m;

            var liquidAssets = cash + bank;
            var currentAssets = liquidAssets + receivable + inventoryValue;
            var currentLiabilities = payable;
            var workingCapital = currentAssets - currentLiabilities;
            var currentRatio = currentLiabilities > 0 ? Math.Round(currentAssets / currentLiabilities, 2) : 0m;
            var quickRatio = currentLiabilities > 0 ? Math.Round((liquidAssets + receivable) / currentLiabilities, 2) : 0m;
            var cashRatio = currentLiabilities > 0 ? Math.Round(liquidAssets / currentLiabilities, 2) : 0m;
            var debtIndex = (currentAssets + liquidAssets) > 0 ? Math.Round(currentLiabilities / (currentAssets + 1m), 2) : 0m;

            // Operational flow last 30 days
            var since = DateTime.UtcNow.Date.AddDays(-30);
            var inflow30 = await db.BankMovements.AsNoTracking()
                .Where(x => x.MovementDate >= since)
                .SumAsync(x => (decimal?)x.AmountIn) ?? 0m;
            var outflow30 = await db.BankMovements.AsNoTracking()
                .Where(x => x.MovementDate >= since)
                .SumAsync(x => (decimal?)x.AmountOut) ?? 0m;
            var netOperational = inflow30 - outflow30;

            return Results.Ok(new FinancialIndicatorsDto
            {
                CashBalance = cash,
                BankBalance = bank,
                ReceivableBalance = receivable,
                PayableBalance = payable,
                InventoryValue = inventoryValue,
                CurrentAssets = currentAssets,
                CurrentLiabilities = currentLiabilities,
                WorkingCapital = workingCapital,
                CurrentRatio = currentRatio,
                QuickRatio = quickRatio,
                CashRatio = cashRatio,
                DebtIndex = debtIndex,
                OperationalInflow30d = inflow30,
                OperationalOutflow30d = outflow30,
                NetOperationalCashFlow30d = netOperational
            });
        });
    }

    // ===================== Proyección Financiera =====================
    private static void MapFinancialProjectionEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/finance/projection").WithTags("FinanceProjection");

        group.MapGet("/", async (int? weeks, NanchesoftDbContext db) =>
        {
            var horizon = Math.Clamp(weeks ?? 8, 1, 52);

            var cash = await db.CashAccounts.AsNoTracking().SumAsync(x => (decimal?)x.CurrentBalance) ?? 0m;
            var bank = await db.BankAccounts.AsNoTracking().SumAsync(x => (decimal?)x.CurrentBalance) ?? 0m;
            decimal openingBalance = cash + bank;

            var today = DateTime.UtcNow.Date;

            var receivableBalance = await db.AccountsReceivableAccounts.AsNoTracking().SumAsync(x => (decimal?)x.CurrentBalance) ?? 0m;
            var payableBalance = await ComputePayableBalanceAsync(db);

            // Cheques pendientes que aún no afectan saldo y emitidos pendientes de cobrar
            var pendingChecks = await db.Checks.AsNoTracking()
                .Where(x => x.Status == "issued" || x.Status == "pending" || x.Status == "printed")
                .Select(x => new { x.IssueDate, x.PostingDate, x.Status, x.Amount })
                .ToListAsync();
            var pendingCheckAmount = pendingChecks.Where(c => c.Status != "issued").Sum(c => c.Amount);

            // Distribución suave en el horizonte (cobranza con perfil decreciente, pagos uniforme)
            var weeksList = new List<FinancialProjectionWeekDto>();
            decimal cumulative = openingBalance;
            decimal pendingReceivable = receivableBalance;
            decimal pendingPayable = payableBalance + pendingCheckAmount;
            decimal totalWeightIn = 0m;
            decimal totalWeightOut = 0m;
            var inflowWeights = new decimal[horizon];
            var outflowWeights = new decimal[horizon];
            for (int i = 0; i < horizon; i++)
            {
                inflowWeights[i] = Math.Max(0.05m, 1m - (i * 0.10m));
                outflowWeights[i] = 1m;
                totalWeightIn += inflowWeights[i];
                totalWeightOut += outflowWeights[i];
            }
            for (int i = 0; i < horizon; i++)
            {
                var startWeek = today.AddDays(i * 7);
                var endWeek = startWeek.AddDays(6);

                var inflow = totalWeightIn > 0m ? Math.Round(pendingReceivable * (inflowWeights[i] / totalWeightIn), 2) : 0m;
                var outflow = totalWeightOut > 0m ? Math.Round(pendingPayable * (outflowWeights[i] / totalWeightOut), 2) : 0m;
                // Cheques con fecha posterior dentro de la semana actual añaden outflow exacto
                outflow += pendingChecks
                    .Where(c => c.Status == "issued")
                    .Where(c => (c.PostingDate ?? c.IssueDate).Date >= startWeek && (c.PostingDate ?? c.IssueDate).Date <= endWeek)
                    .Sum(c => c.Amount);

                var net = inflow - outflow;
                cumulative += net;
                weeksList.Add(new FinancialProjectionWeekDto
                {
                    WeekIndex = i + 1,
                    StartDate = startWeek,
                    EndDate = endWeek,
                    ExpectedInflow = inflow,
                    ExpectedOutflow = outflow,
                    NetFlow = net,
                    CumulativeBalance = cumulative
                });
            }

            return Results.Ok(new FinancialProjectionDto
            {
                OpeningBalance = openingBalance,
                HorizonWeeks = horizon,
                ClosingBalance = cumulative,
                Weeks = weeksList,
                PendingReceivable = receivableBalance,
                PendingPayable = payableBalance,
                PendingChecksAmount = pendingCheckAmount
            });
        });
    }

    // ===================== Helpers =====================
    private static async Task<decimal> ComputePayableBalanceAsync(NanchesoftDbContext db)
    {
        var charges = await db.PurchaseInvoices.AsNoTracking()
            .Where(x => x.IsActive && x.Status != "cancelled")
            .SumAsync(x => (decimal?)x.Total) ?? 0m;
        var returns = await db.PurchaseReturns.AsNoTracking()
            .Where(x => x.IsActive && x.Status != "cancelled")
            .SumAsync(x => (decimal?)x.Total) ?? 0m;
        var payments = await db.PaymentLines.AsNoTracking()
            .Where(line => line.IsActive && line.PurchaseInvoiceId != null)
            .Join(db.Payments.AsNoTracking().Where(p => p.IsActive && p.Status != "cancelled"),
                line => line.PaymentId, payment => payment.Id, (line, payment) => line.Amount)
            .SumAsync(x => (decimal?)x) ?? 0m;
        return charges - returns - payments;
    }

    private static string NormalizeDirection(string? value)
    {
        return (value ?? "neutral").Trim().ToLowerInvariant() switch
        {
            "in" or "input" or "ingreso" => "in",
            "out" or "output" or "egreso" => "out",
            _ => "neutral"
        };
    }

    private static string NormalizeBeneficiaryType(string? type, Guid? supplierId, Guid? employeeId)
    {
        var normalized = (type ?? string.Empty).Trim().ToLowerInvariant();
        if (normalized is "supplier" or "employee" or "other") return normalized;
        if (supplierId.HasValue) return "supplier";
        if (employeeId.HasValue) return "employee";
        return "other";
    }

    private static CheckListRowDto MapToRow(Check entity) => new()
    {
        Id = entity.Id,
        CompanyId = entity.CompanyId,
        BankAccountId = entity.BankAccountId,
        CheckBookId = entity.CheckBookId,
        SupplierId = entity.SupplierId,
        EmployeeId = entity.EmployeeId,
        Folio = entity.Folio,
        IssueDate = entity.IssueDate,
        PostingDate = entity.PostingDate,
        CashedDate = entity.CashedDate,
        BeneficiaryType = entity.BeneficiaryType,
        BeneficiaryName = entity.BeneficiaryName,
        Amount = entity.Amount,
        Concept = entity.Concept,
        Reference = entity.Reference,
        Status = entity.Status,
        IsPrinted = entity.IsPrinted,
        BankMovementId = entity.BankMovementId,
        Notes = entity.Notes,
        IsActive = entity.IsActive
    };
}

// ========================= DTOs =========================

public sealed class FinanceMovementTypeDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Direction { get; set; } = "neutral";
    public string Nature { get; set; } = string.Empty;
    public bool AffectsBalance { get; set; } = true;
    public bool IsSystem { get; set; }
    public Guid? AccountingAccountId { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class FinanceConceptDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = "other";
    public string Direction { get; set; } = "neutral";
    public Guid? AccountingAccountId { get; set; }
    public bool IsSystem { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class CheckBookDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BankAccountId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Series { get; set; } = string.Empty;
    public int FolioStart { get; set; }
    public int FolioEnd { get; set; }
    public int NextFolio { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class CheckSaveRequestDto
{
    public Guid? Id { get; set; }
    public Guid BankAccountId { get; set; }
    public Guid? CheckBookId { get; set; }
    public Guid? SupplierId { get; set; }
    public Guid? EmployeeId { get; set; }
    public string? Folio { get; set; }
    public DateTime? IssueDate { get; set; }
    public string? BeneficiaryType { get; set; }
    public string? BeneficiaryName { get; set; }
    public decimal Amount { get; set; }
    public string? Concept { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
}

public sealed class CheckCancelRequestDto
{
    public string? Reason { get; set; }
}

public sealed class CheckListRowDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BankAccountId { get; set; }
    public Guid? CheckBookId { get; set; }
    public Guid? SupplierId { get; set; }
    public Guid? EmployeeId { get; set; }
    public string Folio { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public DateTime? PostingDate { get; set; }
    public DateTime? CashedDate { get; set; }
    public string BeneficiaryType { get; set; } = string.Empty;
    public string BeneficiaryName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Concept { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsPrinted { get; set; }
    public Guid? BankMovementId { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class CheckSummaryDto
{
    public int PendingCount { get; set; }
    public decimal PendingAmount { get; set; }
    public int IssuedCount { get; set; }
    public decimal IssuedAmount { get; set; }
    public int CashedCount { get; set; }
    public decimal CashedAmount { get; set; }
    public int CancelledCount { get; set; }
}

public sealed class FinancialIndicatorsDto
{
    public decimal CashBalance { get; set; }
    public decimal BankBalance { get; set; }
    public decimal ReceivableBalance { get; set; }
    public decimal PayableBalance { get; set; }
    public decimal InventoryValue { get; set; }
    public decimal CurrentAssets { get; set; }
    public decimal CurrentLiabilities { get; set; }
    public decimal WorkingCapital { get; set; }
    public decimal CurrentRatio { get; set; }
    public decimal QuickRatio { get; set; }
    public decimal CashRatio { get; set; }
    public decimal DebtIndex { get; set; }
    public decimal OperationalInflow30d { get; set; }
    public decimal OperationalOutflow30d { get; set; }
    public decimal NetOperationalCashFlow30d { get; set; }
}

public sealed class FinancialProjectionDto
{
    public decimal OpeningBalance { get; set; }
    public int HorizonWeeks { get; set; }
    public decimal ClosingBalance { get; set; }
    public decimal PendingReceivable { get; set; }
    public decimal PendingPayable { get; set; }
    public decimal PendingChecksAmount { get; set; }
    public List<FinancialProjectionWeekDto> Weeks { get; set; } = new();
}

public sealed class FinancialProjectionWeekDto
{
    public int WeekIndex { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal ExpectedInflow { get; set; }
    public decimal ExpectedOutflow { get; set; }
    public decimal NetFlow { get; set; }
    public decimal CumulativeBalance { get; set; }
}
