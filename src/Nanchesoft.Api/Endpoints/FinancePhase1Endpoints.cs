using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class FinancePhase1Endpoints
{
    public static IEndpointRouteBuilder MapFinancePhase1Endpoints(this IEndpointRouteBuilder app)
    {
        MapBankMovementEndpoints(app);
        MapInternalTransferEndpoints(app);
        MapBankStatementEndpoints(app);
        MapReconciliationSuggestionEndpoints(app);
        return app;
    }

    // -------------------------------------------------------------------------
    // Bank movements (manual capture: deposit, withdrawal, fee, interest, charge)
    // -------------------------------------------------------------------------
    private static void MapBankMovementEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/finance/bank-movements").WithTags("FinanceBankMovements");

        group.MapGet("/", async (Guid? bankAccountId, DateTime? from, DateTime? to, NanchesoftDbContext db) =>
        {
            var query = db.BankMovements.AsNoTracking().AsQueryable();
            if (bankAccountId.HasValue) query = query.Where(x => x.BankAccountId == bankAccountId.Value);
            if (from.HasValue) query = query.Where(x => x.MovementDate >= from.Value.Date);
            if (to.HasValue) query = query.Where(x => x.MovementDate <= to.Value.Date.AddDays(1).AddTicks(-1));
            var rows = await query.OrderByDescending(x => x.MovementDate).ThenByDescending(x => x.CreatedAt)
                .Select(x => new BankMovementListRowDto
                {
                    BankMovementId = x.Id,
                    CompanyId = x.CompanyId,
                    BankAccountId = x.BankAccountId,
                    MovementDate = x.MovementDate,
                    MovementType = x.MovementType,
                    DocumentType = x.DocumentType,
                    DocumentId = x.DocumentId,
                    Reference = x.Reference,
                    AmountIn = x.AmountIn,
                    AmountOut = x.AmountOut,
                    BalanceAfter = x.BalanceAfter,
                    IsReconciled = x.IsReconciled,
                    IsActive = x.IsActive
                }).ToListAsync();
            return Results.Ok(rows);
        });

        group.MapGet("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.BankMovements.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return Results.NotFound(new { message = "No se encontró el movimiento." });
            return Results.Ok(new BankMovementRequestDto
            {
                BankMovementId = entity.Id,
                CompanyId = entity.CompanyId,
                BankAccountId = entity.BankAccountId,
                MovementDate = entity.MovementDate,
                MovementType = entity.MovementType,
                DocumentType = entity.DocumentType,
                Reference = entity.Reference,
                AmountIn = entity.AmountIn,
                AmountOut = entity.AmountOut,
                IsReconciled = entity.IsReconciled,
                IsActive = entity.IsActive
            });
        });

        group.MapPost("/", async (BankMovementRequestDto request, NanchesoftDbContext db) =>
        {
            var account = await db.BankAccounts.FirstOrDefaultAsync(x => x.Id == request.BankAccountId);
            if (account is null) return Results.BadRequest(new { message = "Cuenta bancaria no encontrada." });
            if (request.AmountIn < 0m || request.AmountOut < 0m)
                return Results.BadRequest(new { message = "Los montos no pueden ser negativos." });
            if (request.AmountIn == 0m && request.AmountOut == 0m)
                return Results.BadRequest(new { message = "El movimiento debe tener una entrada o una salida." });
            if (request.AmountIn > 0m && request.AmountOut > 0m)
                return Results.BadRequest(new { message = "Un movimiento no puede tener entrada y salida al mismo tiempo." });
            if (request.AmountOut > 0m && account.CurrentBalance < request.AmountOut)
                return Results.BadRequest(new { message = "Saldo insuficiente en la cuenta bancaria." });

            var movementType = NormalizeMovementType(request.MovementType, request.AmountIn > 0m);
            account.CurrentBalance += request.AmountIn - request.AmountOut;
            account.UpdatedAt = DateTime.UtcNow;
            account.UpdatedBy = "web-api";

            var entity = new BankMovement
            {
                TenantId = account.TenantId,
                CompanyId = account.CompanyId,
                BankAccountId = account.Id,
                MovementDate = request.MovementDate?.Date ?? DateTime.UtcNow.Date,
                MovementType = movementType,
                DocumentType = string.IsNullOrWhiteSpace(request.DocumentType) ? "manual" : request.DocumentType.Trim().ToLowerInvariant(),
                Reference = (request.Reference ?? string.Empty).Trim(),
                AmountIn = request.AmountIn,
                AmountOut = request.AmountOut,
                BalanceAfter = account.CurrentBalance,
                IsReconciled = request.IsReconciled,
                IsActive = request.IsActive,
                CreatedBy = "web-api"
            };
            db.BankMovements.Add(entity);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true, id = entity.Id });
        });

        group.MapPut("/{id:guid}", async (Guid id, BankMovementRequestDto request, NanchesoftDbContext db) =>
        {
            var entity = await db.BankMovements.FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return Results.NotFound(new { message = "No se encontró el movimiento." });
            if (entity.IsReconciled)
                return Results.BadRequest(new { message = "El movimiento ya fue conciliado y no se puede modificar." });
            if (!string.Equals(entity.DocumentType, "manual", StringComparison.OrdinalIgnoreCase))
                return Results.BadRequest(new { message = "Solo los movimientos manuales pueden editarse aquí." });

            var account = await db.BankAccounts.FirstOrDefaultAsync(x => x.Id == entity.BankAccountId);
            if (account is null) return Results.BadRequest(new { message = "Cuenta bancaria no encontrada." });

            // revert previous effect, apply new one
            account.CurrentBalance -= entity.AmountIn - entity.AmountOut;
            if (request.AmountOut > 0m && account.CurrentBalance < request.AmountOut)
            {
                // restore and bail
                account.CurrentBalance += entity.AmountIn - entity.AmountOut;
                return Results.BadRequest(new { message = "Saldo insuficiente para actualizar el movimiento." });
            }
            account.CurrentBalance += request.AmountIn - request.AmountOut;
            account.UpdatedAt = DateTime.UtcNow;
            account.UpdatedBy = "web-api";

            entity.MovementDate = request.MovementDate?.Date ?? entity.MovementDate;
            entity.MovementType = NormalizeMovementType(request.MovementType, request.AmountIn > 0m);
            entity.Reference = request.Reference?.Trim() ?? entity.Reference;
            entity.AmountIn = request.AmountIn;
            entity.AmountOut = request.AmountOut;
            entity.BalanceAfter = account.CurrentBalance;
            entity.IsReconciled = request.IsReconciled;
            entity.IsActive = request.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = "web-api";

            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });

        group.MapDelete("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.BankMovements.FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return Results.NotFound(new { message = "No se encontró el movimiento." });
            if (entity.IsReconciled)
                return Results.BadRequest(new { message = "El movimiento ya fue conciliado y no se puede eliminar." });
            if (!string.Equals(entity.DocumentType, "manual", StringComparison.OrdinalIgnoreCase))
                return Results.BadRequest(new { message = "Solo los movimientos manuales pueden eliminarse aquí." });

            var account = await db.BankAccounts.FirstOrDefaultAsync(x => x.Id == entity.BankAccountId);
            if (account is not null)
            {
                account.CurrentBalance -= entity.AmountIn - entity.AmountOut;
                account.UpdatedAt = DateTime.UtcNow;
                account.UpdatedBy = "web-api";
            }
            db.BankMovements.Remove(entity);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });

        // Account statement (movement ledger) view
        group.MapGet("/account/{bankAccountId:guid}/statement", async (Guid bankAccountId, DateTime? from, DateTime? to, NanchesoftDbContext db) =>
        {
            var account = await db.BankAccounts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == bankAccountId);
            if (account is null) return Results.NotFound(new { message = "Cuenta bancaria no encontrada." });

            var query = db.BankMovements.AsNoTracking().Where(x => x.BankAccountId == bankAccountId);
            if (from.HasValue) query = query.Where(x => x.MovementDate >= from.Value.Date);
            if (to.HasValue) query = query.Where(x => x.MovementDate <= to.Value.Date.AddDays(1).AddTicks(-1));

            var rows = await query.OrderBy(x => x.MovementDate).ThenBy(x => x.CreatedAt).ToListAsync();
            var dto = new BankAccountStatementDto
            {
                BankAccountId = account.Id,
                BankAccountName = account.Name,
                BankAccountCode = account.Code,
                InitialBalance = account.InitialBalance,
                CurrentBalance = account.CurrentBalance,
                ReconciledBalance = account.ReconciledBalance,
                TotalIn = rows.Sum(x => x.AmountIn),
                TotalOut = rows.Sum(x => x.AmountOut),
                Movements = rows.Select(x => new BankMovementListRowDto
                {
                    BankMovementId = x.Id,
                    CompanyId = x.CompanyId,
                    BankAccountId = x.BankAccountId,
                    MovementDate = x.MovementDate,
                    MovementType = x.MovementType,
                    DocumentType = x.DocumentType,
                    DocumentId = x.DocumentId,
                    Reference = x.Reference,
                    AmountIn = x.AmountIn,
                    AmountOut = x.AmountOut,
                    BalanceAfter = x.BalanceAfter,
                    IsReconciled = x.IsReconciled,
                    IsActive = x.IsActive
                }).ToList()
            };
            return Results.Ok(dto);
        });
    }

    // -------------------------------------------------------------------------
    // Internal transfers between bank/cash accounts
    // -------------------------------------------------------------------------
    private static void MapInternalTransferEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/finance/internal-transfers").WithTags("FinanceInternalTransfers");

        group.MapGet("/", async (DateTime? from, DateTime? to, NanchesoftDbContext db) =>
        {
            var query = db.InternalTransfers.AsNoTracking().AsQueryable();
            if (from.HasValue) query = query.Where(x => x.TransferDate >= from.Value.Date);
            if (to.HasValue) query = query.Where(x => x.TransferDate <= to.Value.Date.AddDays(1).AddTicks(-1));
            var rows = await query.OrderByDescending(x => x.TransferDate).ThenByDescending(x => x.CreatedAt)
                .Select(x => new InternalTransferRowDto
                {
                    InternalTransferId = x.Id,
                    CompanyId = x.CompanyId,
                    TransferDate = x.TransferDate,
                    SourceAccountType = x.SourceAccountType,
                    SourceAccountId = x.SourceAccountId,
                    DestinationAccountType = x.DestinationAccountType,
                    DestinationAccountId = x.DestinationAccountId,
                    Amount = x.Amount,
                    Reference = x.Reference,
                    Notes = x.Notes,
                    Status = x.Status,
                    IsActive = x.IsActive
                }).ToListAsync();
            return Results.Ok(rows);
        });

        group.MapPost("/", async (InternalTransferRequestDto request, NanchesoftDbContext db) =>
        {
            if (request.Amount <= 0m) return Results.BadRequest(new { message = "El importe debe ser mayor a cero." });
            if (request.SourceAccountId == Guid.Empty || request.DestinationAccountId == Guid.Empty)
                return Results.BadRequest(new { message = "Selecciona la cuenta origen y destino." });
            if (request.SourceAccountId == request.DestinationAccountId && string.Equals(request.SourceAccountType, request.DestinationAccountType, StringComparison.OrdinalIgnoreCase))
                return Results.BadRequest(new { message = "La cuenta origen y destino no pueden ser la misma." });

            var sourceType = NormalizeAccountType(request.SourceAccountType);
            var destinationType = NormalizeAccountType(request.DestinationAccountType);
            var transferDate = request.TransferDate?.Date ?? DateTime.UtcNow.Date;
            var reference = (request.Reference ?? string.Empty).Trim();
            var notes = (request.Notes ?? string.Empty).Trim();

            Guid tenantId;
            Guid companyId;
            Guid? sourceMovementId = null;
            Guid? destinationMovementId = null;

            // -- Source side --
            if (sourceType == "bank")
            {
                var sourceAccount = await db.BankAccounts.FirstOrDefaultAsync(x => x.Id == request.SourceAccountId);
                if (sourceAccount is null) return Results.BadRequest(new { message = "Cuenta origen no encontrada." });
                if (sourceAccount.CurrentBalance < request.Amount)
                    return Results.BadRequest(new { message = "Saldo insuficiente en la cuenta origen." });
                tenantId = sourceAccount.TenantId;
                companyId = sourceAccount.CompanyId;
                sourceAccount.CurrentBalance -= request.Amount;
                sourceAccount.UpdatedAt = DateTime.UtcNow;
                sourceAccount.UpdatedBy = "web-api";

                var outMovement = new BankMovement
                {
                    TenantId = tenantId,
                    CompanyId = companyId,
                    BankAccountId = sourceAccount.Id,
                    MovementDate = transferDate,
                    MovementType = "transfer_out",
                    DocumentType = "internal_transfer",
                    Reference = reference,
                    AmountIn = 0m,
                    AmountOut = request.Amount,
                    BalanceAfter = sourceAccount.CurrentBalance,
                    CreatedBy = "web-api"
                };
                db.BankMovements.Add(outMovement);
                sourceMovementId = outMovement.Id;
            }
            else
            {
                var sourceAccount = await db.CashAccounts.FirstOrDefaultAsync(x => x.Id == request.SourceAccountId);
                if (sourceAccount is null) return Results.BadRequest(new { message = "Caja origen no encontrada." });
                if (sourceAccount.CurrentBalance < request.Amount)
                    return Results.BadRequest(new { message = "Saldo insuficiente en la caja origen." });
                tenantId = sourceAccount.TenantId;
                companyId = sourceAccount.CompanyId;
                sourceAccount.CurrentBalance -= request.Amount;
                sourceAccount.UpdatedAt = DateTime.UtcNow;
                sourceAccount.UpdatedBy = "web-api";

                var outMovement = new CashMovement
                {
                    TenantId = tenantId,
                    CompanyId = companyId,
                    BranchId = sourceAccount.BranchId,
                    CashAccountId = sourceAccount.Id,
                    MovementDate = transferDate,
                    MovementType = "transfer_out",
                    DocumentType = "internal_transfer",
                    Reference = reference,
                    AmountIn = 0m,
                    AmountOut = request.Amount,
                    BalanceAfter = sourceAccount.CurrentBalance,
                    CreatedBy = "web-api"
                };
                db.CashMovements.Add(outMovement);
                sourceMovementId = outMovement.Id;
            }

            // -- Destination side --
            if (destinationType == "bank")
            {
                var destAccount = await db.BankAccounts.FirstOrDefaultAsync(x => x.Id == request.DestinationAccountId);
                if (destAccount is null) return Results.BadRequest(new { message = "Cuenta destino no encontrada." });
                destAccount.CurrentBalance += request.Amount;
                destAccount.UpdatedAt = DateTime.UtcNow;
                destAccount.UpdatedBy = "web-api";

                var inMovement = new BankMovement
                {
                    TenantId = destAccount.TenantId,
                    CompanyId = destAccount.CompanyId,
                    BankAccountId = destAccount.Id,
                    MovementDate = transferDate,
                    MovementType = "transfer_in",
                    DocumentType = "internal_transfer",
                    Reference = reference,
                    AmountIn = request.Amount,
                    AmountOut = 0m,
                    BalanceAfter = destAccount.CurrentBalance,
                    CreatedBy = "web-api"
                };
                db.BankMovements.Add(inMovement);
                destinationMovementId = inMovement.Id;
            }
            else
            {
                var destAccount = await db.CashAccounts.FirstOrDefaultAsync(x => x.Id == request.DestinationAccountId);
                if (destAccount is null) return Results.BadRequest(new { message = "Caja destino no encontrada." });
                destAccount.CurrentBalance += request.Amount;
                destAccount.UpdatedAt = DateTime.UtcNow;
                destAccount.UpdatedBy = "web-api";

                var inMovement = new CashMovement
                {
                    TenantId = destAccount.TenantId,
                    CompanyId = destAccount.CompanyId,
                    BranchId = destAccount.BranchId,
                    CashAccountId = destAccount.Id,
                    MovementDate = transferDate,
                    MovementType = "transfer_in",
                    DocumentType = "internal_transfer",
                    Reference = reference,
                    AmountIn = request.Amount,
                    AmountOut = 0m,
                    BalanceAfter = destAccount.CurrentBalance,
                    CreatedBy = "web-api"
                };
                db.CashMovements.Add(inMovement);
                destinationMovementId = inMovement.Id;
            }

            var transfer = new InternalTransfer
            {
                TenantId = tenantId,
                CompanyId = companyId,
                TransferDate = transferDate,
                SourceAccountType = sourceType,
                SourceAccountId = request.SourceAccountId,
                DestinationAccountType = destinationType,
                DestinationAccountId = request.DestinationAccountId,
                Amount = request.Amount,
                Reference = reference,
                Notes = notes,
                Status = "posted",
                SourceMovementId = sourceMovementId,
                DestinationMovementId = destinationMovementId,
                CreatedBy = "web-api"
            };
            db.InternalTransfers.Add(transfer);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true, id = transfer.Id });
        });
    }

    // -------------------------------------------------------------------------
    // Bank statements (manual capture + CSV import)
    // -------------------------------------------------------------------------
    private static void MapBankStatementEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/finance/bank-statements").WithTags("FinanceBankStatements");

        group.MapGet("/", async (Guid? bankAccountId, NanchesoftDbContext db) =>
        {
            var query = db.BankStatements.AsNoTracking().AsQueryable();
            if (bankAccountId.HasValue) query = query.Where(x => x.BankAccountId == bankAccountId.Value);
            var rows = await query.OrderByDescending(x => x.StatementDate).ThenByDescending(x => x.CreatedAt)
                .Select(x => new BankStatementListRowDto
                {
                    BankStatementId = x.Id,
                    CompanyId = x.CompanyId,
                    BankAccountId = x.BankAccountId,
                    StatementDate = x.StatementDate,
                    PeriodStart = x.PeriodStart,
                    PeriodEnd = x.PeriodEnd,
                    OpeningBalance = x.OpeningBalance,
                    ClosingBalance = x.ClosingBalance,
                    Source = x.Source,
                    Reference = x.Reference,
                    EntryCount = x.Entries.Count,
                    IsActive = x.IsActive
                }).ToListAsync();
            return Results.Ok(rows);
        });

        group.MapGet("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.BankStatements.AsNoTracking().Include(x => x.Entries).FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return Results.NotFound(new { message = "No se encontró el estado de cuenta." });
            return Results.Ok(new BankStatementDetailDto
            {
                BankStatementId = entity.Id,
                CompanyId = entity.CompanyId,
                BankAccountId = entity.BankAccountId,
                StatementDate = entity.StatementDate,
                PeriodStart = entity.PeriodStart,
                PeriodEnd = entity.PeriodEnd,
                OpeningBalance = entity.OpeningBalance,
                ClosingBalance = entity.ClosingBalance,
                Source = entity.Source,
                Reference = entity.Reference,
                Notes = entity.Notes,
                IsActive = entity.IsActive,
                Entries = entity.Entries.OrderBy(x => x.EntryDate).ThenBy(x => x.CreatedAt).Select(x => new BankStatementEntryDto
                {
                    Id = x.Id,
                    EntryDate = x.EntryDate,
                    Description = x.Description,
                    Reference = x.Reference,
                    AmountIn = x.AmountIn,
                    AmountOut = x.AmountOut,
                    BalanceAfter = x.BalanceAfter,
                    MatchedMovementId = x.MatchedMovementId,
                    IsMatched = x.IsMatched
                }).ToList()
            });
        });

        group.MapPost("/", async (BankStatementSaveRequestDto request, NanchesoftDbContext db) =>
        {
            var account = await db.BankAccounts.FirstOrDefaultAsync(x => x.Id == request.BankAccountId);
            if (account is null) return Results.BadRequest(new { message = "Cuenta bancaria no encontrada." });

            var statement = new BankStatement
            {
                TenantId = account.TenantId,
                CompanyId = account.CompanyId,
                BankAccountId = account.Id,
                StatementDate = request.StatementDate?.Date ?? DateTime.UtcNow.Date,
                PeriodStart = request.PeriodStart?.Date,
                PeriodEnd = request.PeriodEnd?.Date,
                OpeningBalance = request.OpeningBalance,
                ClosingBalance = request.ClosingBalance,
                Source = string.IsNullOrWhiteSpace(request.Source) ? "manual" : request.Source.Trim().ToLowerInvariant(),
                Reference = (request.Reference ?? string.Empty).Trim(),
                Notes = (request.Notes ?? string.Empty).Trim(),
                IsActive = request.IsActive,
                CreatedBy = "web-api"
            };

            foreach (var entry in request.Entries ?? new List<BankStatementEntryDto>())
            {
                if (entry.AmountIn == 0m && entry.AmountOut == 0m) continue;
                statement.Entries.Add(new BankStatementEntry
                {
                    Id = entry.Id == Guid.Empty ? Guid.NewGuid() : entry.Id,
                    TenantId = account.TenantId,
                    CompanyId = account.CompanyId,
                    BankAccountId = account.Id,
                    EntryDate = entry.EntryDate.Date,
                    Description = (entry.Description ?? string.Empty).Trim(),
                    Reference = (entry.Reference ?? string.Empty).Trim(),
                    AmountIn = entry.AmountIn,
                    AmountOut = entry.AmountOut,
                    BalanceAfter = entry.BalanceAfter,
                    IsMatched = entry.IsMatched,
                    MatchedMovementId = entry.MatchedMovementId,
                    CreatedBy = "web-api"
                });
            }

            db.BankStatements.Add(statement);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true, id = statement.Id, entries = statement.Entries.Count });
        });

        group.MapPost("/import-csv", async (BankStatementCsvImportRequestDto request, NanchesoftDbContext db) =>
        {
            var account = await db.BankAccounts.FirstOrDefaultAsync(x => x.Id == request.BankAccountId);
            if (account is null) return Results.BadRequest(new { message = "Cuenta bancaria no encontrada." });
            if (string.IsNullOrWhiteSpace(request.CsvText)) return Results.BadRequest(new { message = "El contenido CSV está vacío." });

            var entries = ParseCsv(request.CsvText, request.Delimiter ?? ",", request.HasHeader);
            if (entries.Count == 0)
                return Results.BadRequest(new { message = "No se pudieron interpretar movimientos del CSV." });

            var statement = new BankStatement
            {
                TenantId = account.TenantId,
                CompanyId = account.CompanyId,
                BankAccountId = account.Id,
                StatementDate = request.StatementDate?.Date ?? DateTime.UtcNow.Date,
                PeriodStart = entries.Min(x => x.EntryDate),
                PeriodEnd = entries.Max(x => x.EntryDate),
                OpeningBalance = request.OpeningBalance,
                ClosingBalance = request.ClosingBalance == 0m
                    ? request.OpeningBalance + entries.Sum(x => x.AmountIn - x.AmountOut)
                    : request.ClosingBalance,
                Source = "csv",
                Reference = (request.Reference ?? string.Empty).Trim(),
                Notes = (request.Notes ?? string.Empty).Trim(),
                IsActive = true,
                CreatedBy = "web-api"
            };

            foreach (var parsed in entries)
            {
                statement.Entries.Add(new BankStatementEntry
                {
                    Id = Guid.NewGuid(),
                    TenantId = account.TenantId,
                    CompanyId = account.CompanyId,
                    BankAccountId = account.Id,
                    EntryDate = parsed.EntryDate,
                    Description = parsed.Description,
                    Reference = parsed.Reference,
                    AmountIn = parsed.AmountIn,
                    AmountOut = parsed.AmountOut,
                    CreatedBy = "web-api"
                });
            }

            db.BankStatements.Add(statement);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true, id = statement.Id, entries = statement.Entries.Count });
        });

        group.MapDelete("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.BankStatements.Include(x => x.Entries).FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return Results.NotFound(new { message = "No se encontró el estado de cuenta." });
            db.BankStatementEntries.RemoveRange(entity.Entries);
            db.BankStatements.Remove(entity);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });
    }

    // -------------------------------------------------------------------------
    // Reconciliation suggestions (match statement entries with movements)
    // -------------------------------------------------------------------------
    private static void MapReconciliationSuggestionEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/finance/reconciliation").WithTags("FinanceReconciliation");

        group.MapGet("/suggestions", async (Guid bankAccountId, Guid? bankStatementId, int? toleranceDays, NanchesoftDbContext db) =>
        {
            var tolerance = Math.Max(0, toleranceDays ?? 3);

            var movements = await db.BankMovements.AsNoTracking()
                .Where(x => x.BankAccountId == bankAccountId && !x.IsReconciled && x.IsActive)
                .OrderBy(x => x.MovementDate).ToListAsync();

            List<BankStatementEntry> entries;
            if (bankStatementId.HasValue)
            {
                entries = await db.BankStatementEntries.AsNoTracking()
                    .Where(x => x.BankStatementId == bankStatementId.Value && !x.IsMatched)
                    .OrderBy(x => x.EntryDate).ToListAsync();
            }
            else
            {
                entries = await db.BankStatementEntries.AsNoTracking()
                    .Where(x => x.BankAccountId == bankAccountId && !x.IsMatched)
                    .OrderBy(x => x.EntryDate).ToListAsync();
            }

            var suggestions = new List<ReconciliationSuggestionDto>();
            var consumedMovements = new HashSet<Guid>();
            foreach (var entry in entries)
            {
                var entryAmount = entry.AmountIn - entry.AmountOut;
                var match = movements.FirstOrDefault(m =>
                    !consumedMovements.Contains(m.Id) &&
                    Math.Abs((m.AmountIn - m.AmountOut) - entryAmount) < 0.01m &&
                    Math.Abs((m.MovementDate.Date - entry.EntryDate.Date).TotalDays) <= tolerance);
                if (match is not null)
                {
                    consumedMovements.Add(match.Id);
                    suggestions.Add(new ReconciliationSuggestionDto
                    {
                        StatementEntryId = entry.Id,
                        BankMovementId = match.Id,
                        EntryDate = entry.EntryDate,
                        MovementDate = match.MovementDate,
                        Description = entry.Description,
                        Reference = entry.Reference,
                        Amount = entryAmount,
                        DaysDifference = (int)Math.Abs((match.MovementDate.Date - entry.EntryDate.Date).TotalDays),
                        Confidence = ComputeConfidence(entry, match)
                    });
                }
            }
            return Results.Ok(suggestions);
        });

        group.MapPost("/apply-matches", async (ApplyMatchesRequestDto request, NanchesoftDbContext db) =>
        {
            if (request.Matches.Count == 0)
                return Results.BadRequest(new { message = "No hay coincidencias para aplicar." });

            var movementIds = request.Matches.Select(x => x.BankMovementId).Distinct().ToList();
            var entryIds = request.Matches.Select(x => x.StatementEntryId).Distinct().ToList();
            var movements = await db.BankMovements.Where(x => movementIds.Contains(x.Id)).ToListAsync();
            var entries = await db.BankStatementEntries.Where(x => entryIds.Contains(x.Id)).ToListAsync();

            var matched = 0;
            foreach (var pair in request.Matches)
            {
                var movement = movements.FirstOrDefault(x => x.Id == pair.BankMovementId);
                var entry = entries.FirstOrDefault(x => x.Id == pair.StatementEntryId);
                if (movement is null || entry is null) continue;
                movement.IsReconciled = true;
                movement.UpdatedAt = DateTime.UtcNow;
                movement.UpdatedBy = "web-api";
                entry.IsMatched = true;
                entry.MatchedMovementId = movement.Id;
                entry.UpdatedAt = DateTime.UtcNow;
                entry.UpdatedBy = "web-api";
                matched++;
            }

            // Update reconciled balance on the account
            var accountIds = movements.Select(x => x.BankAccountId).Distinct().ToList();
            foreach (var accountId in accountIds)
            {
                var account = await db.BankAccounts.FirstOrDefaultAsync(x => x.Id == accountId);
                if (account is null) continue;
                var reconciled = await db.BankMovements.Where(x => x.BankAccountId == accountId && x.IsReconciled)
                    .SumAsync(x => x.AmountIn - x.AmountOut);
                account.ReconciledBalance = account.InitialBalance + reconciled;
                account.UpdatedAt = DateTime.UtcNow;
                account.UpdatedBy = "web-api";
            }

            await db.SaveChangesAsync();
            return Results.Ok(new { success = true, matched });
        });
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------
    private static string NormalizeMovementType(string? value, bool isInflow)
    {
        if (string.IsNullOrWhiteSpace(value))
            return isInflow ? "deposit" : "withdrawal";
        return value.Trim().ToLowerInvariant() switch
        {
            "deposit" or "withdrawal" or "transfer_in" or "transfer_out" or "fee" or "interest" or "charge" or "adjustment" => value.Trim().ToLowerInvariant(),
            _ => isInflow ? "deposit" : "withdrawal"
        };
    }

    private static string NormalizeAccountType(string? value)
        => string.Equals(value?.Trim(), "cash", StringComparison.OrdinalIgnoreCase) ? "cash" : "bank";

    private static decimal ComputeConfidence(BankStatementEntry entry, BankMovement movement)
    {
        var amountDiff = Math.Abs((movement.AmountIn - movement.AmountOut) - (entry.AmountIn - entry.AmountOut));
        var dateDiff = Math.Abs((movement.MovementDate.Date - entry.EntryDate.Date).TotalDays);
        var confidence = 100m;
        confidence -= (decimal)dateDiff * 5m;
        if (amountDiff > 0.01m) confidence -= 20m;
        if (!string.IsNullOrWhiteSpace(entry.Reference) && entry.Reference.Equals(movement.Reference, StringComparison.OrdinalIgnoreCase))
            confidence += 10m;
        return Math.Max(0m, Math.Min(100m, confidence));
    }

    private static List<ParsedBankStatementEntry> ParseCsv(string csv, string delimiter, bool hasHeader)
    {
        var rows = new List<ParsedBankStatementEntry>();
        var lines = csv.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var startIndex = hasHeader && lines.Length > 0 ? 1 : 0;
        for (int i = startIndex; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;
            var parts = SplitCsvLine(line, delimiter);
            if (parts.Length < 3) continue;
            DateTime entryDate = TryParseDate(parts[0]) ?? DateTime.UtcNow.Date;
            string description = parts.Length > 1 ? parts[1].Trim() : string.Empty;
            string reference = parts.Length > 2 ? parts[2].Trim() : string.Empty;
            decimal amountIn = parts.Length > 3 ? ParseDecimal(parts[3]) : 0m;
            decimal amountOut = parts.Length > 4 ? ParseDecimal(parts[4]) : 0m;
            // also support a signed single amount in column index 3 if only 4 columns
            if (parts.Length == 4 && amountIn != 0m && amountOut == 0m)
            {
                if (amountIn < 0m)
                {
                    amountOut = Math.Abs(amountIn);
                    amountIn = 0m;
                }
            }
            if (amountIn == 0m && amountOut == 0m) continue;
            rows.Add(new ParsedBankStatementEntry
            {
                EntryDate = entryDate,
                Description = description,
                Reference = reference,
                AmountIn = amountIn,
                AmountOut = amountOut
            });
        }
        return rows;
    }

    private static string[] SplitCsvLine(string line, string delimiter)
    {
        var result = new List<string>();
        var sb = new System.Text.StringBuilder();
        var inQuotes = false;
        foreach (var ch in line)
        {
            if (ch == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }
            if (!inQuotes && delimiter.Contains(ch))
            {
                result.Add(sb.ToString());
                sb.Clear();
                continue;
            }
            sb.Append(ch);
        }
        result.Add(sb.ToString());
        return result.ToArray();
    }

    private static DateTime? TryParseDate(string value)
    {
        var formats = new[] { "yyyy-MM-dd", "dd/MM/yyyy", "MM/dd/yyyy", "dd-MM-yyyy", "yyyy/MM/dd" };
        foreach (var format in formats)
        {
            if (DateTime.TryParseExact(value.Trim(), format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var parsed))
                return parsed.Date;
        }
        if (DateTime.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var fallback))
            return fallback.Date;
        return null;
    }

    private static decimal ParseDecimal(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return 0m;
        var clean = value.Replace("$", string.Empty).Replace(",", string.Empty).Trim();
        if (decimal.TryParse(clean, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var result))
            return result;
        return 0m;
    }

    private sealed class ParsedBankStatementEntry
    {
        public DateTime EntryDate { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public decimal AmountIn { get; set; }
        public decimal AmountOut { get; set; }
    }
}

// ============================================================================
// DTOs
// ============================================================================

public sealed class BankMovementListRowDto
{
    public Guid BankMovementId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BankAccountId { get; set; }
    public DateTime MovementDate { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public Guid? DocumentId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public decimal AmountIn { get; set; }
    public decimal AmountOut { get; set; }
    public decimal BalanceAfter { get; set; }
    public bool IsReconciled { get; set; }
    public bool IsActive { get; set; }
}

public sealed class BankMovementRequestDto
{
    public Guid? BankMovementId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BankAccountId { get; set; }
    public DateTime? MovementDate { get; set; }
    public string? MovementType { get; set; }
    public string? DocumentType { get; set; }
    public string? Reference { get; set; }
    public decimal AmountIn { get; set; }
    public decimal AmountOut { get; set; }
    public bool IsReconciled { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class BankAccountStatementDto
{
    public Guid BankAccountId { get; set; }
    public string BankAccountCode { get; set; } = string.Empty;
    public string BankAccountName { get; set; } = string.Empty;
    public decimal InitialBalance { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal ReconciledBalance { get; set; }
    public decimal TotalIn { get; set; }
    public decimal TotalOut { get; set; }
    public List<BankMovementListRowDto> Movements { get; set; } = new();
}

public sealed class InternalTransferRowDto
{
    public Guid InternalTransferId { get; set; }
    public Guid CompanyId { get; set; }
    public DateTime TransferDate { get; set; }
    public string SourceAccountType { get; set; } = string.Empty;
    public Guid SourceAccountId { get; set; }
    public string DestinationAccountType { get; set; } = string.Empty;
    public Guid DestinationAccountId { get; set; }
    public decimal Amount { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class InternalTransferRequestDto
{
    public DateTime? TransferDate { get; set; }
    public string SourceAccountType { get; set; } = "bank";
    public Guid SourceAccountId { get; set; }
    public string DestinationAccountType { get; set; } = "bank";
    public Guid DestinationAccountId { get; set; }
    public decimal Amount { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
}

public sealed class BankStatementListRowDto
{
    public Guid BankStatementId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BankAccountId { get; set; }
    public DateTime StatementDate { get; set; }
    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public int EntryCount { get; set; }
    public bool IsActive { get; set; }
}

public sealed class BankStatementDetailDto
{
    public Guid BankStatementId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BankAccountId { get; set; }
    public DateTime StatementDate { get; set; }
    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public List<BankStatementEntryDto> Entries { get; set; } = new();
}

public sealed class BankStatementEntryDto
{
    public Guid Id { get; set; }
    public DateTime EntryDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public decimal AmountIn { get; set; }
    public decimal AmountOut { get; set; }
    public decimal? BalanceAfter { get; set; }
    public Guid? MatchedMovementId { get; set; }
    public bool IsMatched { get; set; }
}

public sealed class BankStatementSaveRequestDto
{
    public Guid BankAccountId { get; set; }
    public DateTime? StatementDate { get; set; }
    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }
    public string? Source { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public List<BankStatementEntryDto>? Entries { get; set; }
}

public sealed class BankStatementCsvImportRequestDto
{
    public Guid BankAccountId { get; set; }
    public DateTime? StatementDate { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public string? Delimiter { get; set; }
    public bool HasHeader { get; set; } = true;
    public string CsvText { get; set; } = string.Empty;
}

public sealed class ReconciliationSuggestionDto
{
    public Guid StatementEntryId { get; set; }
    public Guid BankMovementId { get; set; }
    public DateTime EntryDate { get; set; }
    public DateTime MovementDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int DaysDifference { get; set; }
    public decimal Confidence { get; set; }
}

public sealed class ApplyMatchesRequestDto
{
    public List<ReconciliationMatchPairDto> Matches { get; set; } = new();
}

public sealed class ReconciliationMatchPairDto
{
    public Guid StatementEntryId { get; set; }
    public Guid BankMovementId { get; set; }
}
