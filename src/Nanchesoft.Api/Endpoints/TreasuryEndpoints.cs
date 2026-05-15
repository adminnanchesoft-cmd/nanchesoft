using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Common;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class TreasuryEndpoints
{
    public static IEndpointRouteBuilder MapTreasuryEndpoints(this IEndpointRouteBuilder app)
    {
        MapCashAccountEndpoints(app);
        MapBankAccountEndpoints(app);
        MapIncomeEndpoints(app);
        MapExpenseEndpoints(app);
        MapReceiptEndpoints(app);
        MapPaymentEndpoints(app);
        MapReconciliationEndpoints(app);
        MapLookupsAndDashboard(app);
        return app;
    }

    private static void MapCashAccountEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/treasury/cash-accounts").WithTags("TreasuryCashAccounts");

        group.MapGet("/", async (NanchesoftDbContext db) =>
            Results.Ok(await db.CashAccounts.AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new
                {
                    CashAccountId = x.Id,
                    x.CompanyId,
                    x.BranchId,
                    x.CurrencyId,
                    x.Code,
                    x.Name,
                    x.Status,
                    x.CurrentBalance,
                    x.IsActive
                }).ToListAsync()));

        group.MapGet("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.CashAccounts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null)
                return Results.NotFound(new { message = "No se encontró la caja." });

            return Results.Ok(new CashAccountRequest
            {
                CashAccountId = entity.Id,
                CompanyId = entity.CompanyId,
                BranchId = entity.BranchId,
                CurrencyId = entity.CurrencyId,
                Code = entity.Code,
                Name = entity.Name,
                Status = entity.Status,
                CurrentBalance = entity.CurrentBalance,
                IsActive = entity.IsActive
            });
        });

        group.MapPost("/", async (CashAccountRequest request, NanchesoftDbContext db) =>
        {
            var company = await GetCompanyAsync(db, request.CompanyId);
            var branch = await GetBranchAsync(db, company.Id, request.BranchId);
            var entity = new CashAccount
            {
                TenantId = company.TenantId,
                CompanyId = company.Id,
                BranchId = branch.Id,
                CurrencyId = request.CurrencyId ?? await GetDefaultCurrencyIdAsync(db),
                Code = NormalizeCode(request.Code, "CAJA"),
                Name = (request.Name ?? string.Empty).Trim(),
                Status = NormalizeStatus(request.Status, "active"),
                CurrentBalance = request.CurrentBalance,
                IsActive = request.IsActive,
                CreatedBy = "web-api"
            };

            db.CashAccounts.Add(entity);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true, id = entity.Id });
        });

        group.MapPut("/{id:guid}", async (Guid id, CashAccountRequest request, NanchesoftDbContext db) =>
        {
            var entity = await db.CashAccounts.FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null)
                return Results.NotFound(new { message = "No se encontró la caja." });

            entity.CompanyId = request.CompanyId ?? entity.CompanyId;
            entity.BranchId = request.BranchId ?? entity.BranchId;
            entity.CurrencyId = request.CurrencyId ?? entity.CurrencyId;
            entity.Code = NormalizeCode(request.Code, entity.Code);
            entity.Name = string.IsNullOrWhiteSpace(request.Name) ? entity.Name : request.Name.Trim();
            entity.Status = NormalizeStatus(request.Status, entity.Status);
            entity.CurrentBalance = request.CurrentBalance;
            entity.IsActive = request.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = "web-api";
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });

        group.MapPost("/{id:guid}/activate", async (Guid id, NanchesoftDbContext db) => await SetActiveAsync<CashAccount>(id, db, true, "No se encontró la caja."));
        group.MapPost("/{id:guid}/deactivate", async (Guid id, NanchesoftDbContext db) => await SetActiveAsync<CashAccount>(id, db, false, "No se encontró la caja."));
    }

    private static void MapBankAccountEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/treasury/bank-accounts").WithTags("TreasuryBankAccounts");

        group.MapGet("/", async (NanchesoftDbContext db) =>
            Results.Ok(await db.BankAccounts.AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new
                {
                    BankAccountId = x.Id,
                    x.CompanyId,
                    x.BankId,
                    x.CurrencyId,
                    x.Code,
                    x.Name,
                    x.AccountHolder,
                    x.AccountNumber,
                    x.Clabe,
                    x.Status,
                    x.CurrentBalance,
                    x.IsActive
                }).ToListAsync()));

        group.MapGet("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.BankAccounts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null)
                return Results.NotFound(new { message = "No se encontró la cuenta bancaria." });

            return Results.Ok(new TreasuryBankAccountRequest
            {
                BankAccountId = entity.Id,
                CompanyId = entity.CompanyId,
                BankId = entity.BankId,
                CurrencyId = entity.CurrencyId,
                Code = entity.Code,
                Name = entity.Name,
                AccountHolder = entity.AccountHolder,
                AccountNumber = entity.AccountNumber,
                Clabe = entity.Clabe,
                Status = entity.Status,
                CurrentBalance = entity.CurrentBalance,
                IsActive = entity.IsActive
            });
        });

        group.MapPost("/", async (TreasuryBankAccountRequest request, NanchesoftDbContext db) =>
        {
            var company = await GetCompanyAsync(db, request.CompanyId);
            var entity = new BankAccount
            {
                TenantId = company.TenantId,
                CompanyId = company.Id,
                BankId = request.BankId,
                CurrencyId = request.CurrencyId ?? await GetDefaultCurrencyIdAsync(db),
                Code = NormalizeCode(request.Code, "BANC"),
                Name = (request.Name ?? string.Empty).Trim(),
                AccountHolder = (request.AccountHolder ?? string.Empty).Trim(),
                AccountNumber = (request.AccountNumber ?? string.Empty).Trim(),
                Clabe = (request.Clabe ?? string.Empty).Trim(),
                Status = NormalizeStatus(request.Status, "active"),
                CurrentBalance = request.CurrentBalance,
                IsActive = request.IsActive,
                CreatedBy = "web-api"
            };
            db.BankAccounts.Add(entity);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true, id = entity.Id });
        });

        group.MapPut("/{id:guid}", async (Guid id, TreasuryBankAccountRequest request, NanchesoftDbContext db) =>
        {
            var entity = await db.BankAccounts.FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null)
                return Results.NotFound(new { message = "No se encontró la cuenta bancaria." });

            entity.CompanyId = request.CompanyId ?? entity.CompanyId;
            entity.BankId = request.BankId ?? entity.BankId;
            entity.CurrencyId = request.CurrencyId ?? entity.CurrencyId;
            entity.Code = NormalizeCode(request.Code, entity.Code);
            entity.Name = string.IsNullOrWhiteSpace(request.Name) ? entity.Name : request.Name.Trim();
            entity.AccountHolder = request.AccountHolder?.Trim() ?? entity.AccountHolder;
            entity.AccountNumber = request.AccountNumber?.Trim() ?? entity.AccountNumber;
            entity.Clabe = request.Clabe?.Trim() ?? entity.Clabe;
            entity.Status = NormalizeStatus(request.Status, entity.Status);
            entity.CurrentBalance = request.CurrentBalance;
            entity.IsActive = request.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = "web-api";
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });

        group.MapPost("/{id:guid}/activate", async (Guid id, NanchesoftDbContext db) => await SetActiveAsync<BankAccount>(id, db, true, "No se encontró la cuenta bancaria."));
        group.MapPost("/{id:guid}/deactivate", async (Guid id, NanchesoftDbContext db) => await SetActiveAsync<BankAccount>(id, db, false, "No se encontró la cuenta bancaria."));
    }

    private static void MapIncomeEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/treasury/incomes").WithTags("TreasuryIncomes");
        MapDocumentEndpoints(
            group,
            listQuery: db => db.TreasuryIncomes.AsNoTracking().OrderByDescending(x => x.IncomeDate).Select(x => new
            {
                TreasuryIncomeId = x.Id,
                x.CompanyId,
                x.BranchId,
                x.CashAccountId,
                x.BankAccountId,
                x.CurrencyId,
                x.Folio,
                DocumentDate = x.IncomeDate,
                x.TargetType,
                x.Status,
                x.Reference,
                x.Notes,
                x.Total,
                x.ApprovedAt,
                x.PostedAt,
                x.IsActive
            }),
            getById: async (id, db) =>
            {
                var entity = await db.TreasuryIncomes.AsNoTracking().Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
                return entity is null ? Results.NotFound(new { message = "No se encontró el ingreso." }) : Results.Ok(ToRequest(entity));
            },
            create: async (request, db) =>
            {
                var company = await GetCompanyAsync(db, request.CompanyId);
                var branch = await GetBranchAsync(db, company.Id, request.BranchId);
                var targetType = NormalizeTargetType(request.TargetType);
                var entity = new TreasuryIncome
                {
                    TenantId = company.TenantId,
                    CompanyId = company.Id,
                    BranchId = branch.Id,
                    SeriesId = request.SeriesId,
                    CurrencyId = request.CurrencyId ?? await GetDefaultCurrencyIdAsync(db),
                    CashAccountId = targetType == "cash" ? await ResolveCashAccountIdAsync(db, company.Id, request.CashAccountId) : null,
                    BankAccountId = targetType == "bank" ? await ResolveBankAccountIdAsync(db, company.Id, request.BankAccountId) : null,
                    Folio = NormalizeFolio(request.Folio, "INGR"),
                    IncomeDate = request.DocumentDate?.Date ?? DateTime.UtcNow.Date,
                    TargetType = targetType,
                    ExchangeRate = request.ExchangeRate <= 0 ? 1m : request.ExchangeRate,
                    Status = NormalizeStatus(request.Status),
                    Reference = (request.Reference ?? string.Empty).Trim(),
                    Notes = (request.Notes ?? string.Empty).Trim(),
                    Total = 0m,
                    ApprovedAt = request.ApprovedAt,
                    PostedAt = request.PostedAt,
                    IsActive = request.IsActive,
                    CreatedBy = "web-api"
                };
                ApplyIncomeLines(entity, request.Lines);
                entity.Total = entity.Lines.Sum(x => x.Amount);
                db.TreasuryIncomes.Add(entity);
                await db.SaveChangesAsync();
                return Results.Ok(new { success = true, id = entity.Id });
            },
            update: async (id, request, db) =>
            {
                var entity = await db.TreasuryIncomes.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
                if (entity is null)
                    return Results.NotFound(new { message = "No se encontró el ingreso." });

                entity.CompanyId = request.CompanyId ?? entity.CompanyId;
                entity.BranchId = request.BranchId ?? entity.BranchId;
                entity.SeriesId = request.SeriesId ?? entity.SeriesId;
                entity.CurrencyId = request.CurrencyId ?? entity.CurrencyId;
                entity.TargetType = NormalizeTargetType(request.TargetType, entity.TargetType);
                entity.CashAccountId = entity.TargetType == "cash" ? request.CashAccountId ?? entity.CashAccountId : null;
                entity.BankAccountId = entity.TargetType == "bank" ? request.BankAccountId ?? entity.BankAccountId : null;
                entity.Folio = NormalizeFolio(request.Folio, entity.Folio);
                entity.IncomeDate = request.DocumentDate?.Date ?? entity.IncomeDate;
                entity.ExchangeRate = request.ExchangeRate <= 0 ? entity.ExchangeRate : request.ExchangeRate;
                entity.Status = NormalizeStatus(request.Status, entity.Status);
                entity.Reference = request.Reference?.Trim() ?? entity.Reference;
                entity.Notes = request.Notes?.Trim() ?? entity.Notes;
                entity.ApprovedAt = request.ApprovedAt;
                entity.PostedAt = request.PostedAt;
                entity.IsActive = request.IsActive;
                entity.UpdatedAt = DateTime.UtcNow;
                entity.UpdatedBy = "web-api";
                ApplyIncomeLines(entity, request.Lines);
                entity.Total = entity.Lines.Sum(x => x.Amount);
                await db.SaveChangesAsync();
                return Results.Ok(new { success = true });
            });

        group.MapPost("/{id:guid}/approve", async (Guid id, NanchesoftDbContext db) => await ApproveDocumentAsync<TreasuryIncome>(id, db, "No se encontró el ingreso."));
        group.MapPost("/{id:guid}/post", async (Guid id, NanchesoftDbContext db) => await PostIncomeAsync(id, db));
        group.MapPost("/{id:guid}/cancel", async (Guid id, NanchesoftDbContext db) => await CancelDocumentAsync<TreasuryIncome>(id, db, "No se encontró el ingreso."));
    }

    private static void MapExpenseEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/treasury/expenses").WithTags("TreasuryExpenses");
        MapDocumentEndpoints(
            group,
            listQuery: db => db.TreasuryExpenses.AsNoTracking().OrderByDescending(x => x.ExpenseDate).Select(x => new
            {
                TreasuryExpenseId = x.Id,
                x.CompanyId,
                x.BranchId,
                x.CashAccountId,
                x.BankAccountId,
                x.CurrencyId,
                x.Folio,
                DocumentDate = x.ExpenseDate,
                x.SourceType,
                x.Status,
                x.Reference,
                x.Notes,
                x.Total,
                x.ApprovedAt,
                x.PostedAt,
                x.IsActive
            }),
            getById: async (id, db) =>
            {
                var entity = await db.TreasuryExpenses.AsNoTracking().Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
                return entity is null ? Results.NotFound(new { message = "No se encontró el egreso." }) : Results.Ok(ToRequest(entity));
            },
            create: async (request, db) =>
            {
                var company = await GetCompanyAsync(db, request.CompanyId);
                var branch = await GetBranchAsync(db, company.Id, request.BranchId);
                var sourceType = NormalizeSourceType(request.SourceType);
                var entity = new TreasuryExpense
                {
                    TenantId = company.TenantId,
                    CompanyId = company.Id,
                    BranchId = branch.Id,
                    SeriesId = request.SeriesId,
                    CurrencyId = request.CurrencyId ?? await GetDefaultCurrencyIdAsync(db),
                    CashAccountId = sourceType == "cash" ? await ResolveCashAccountIdAsync(db, company.Id, request.CashAccountId) : null,
                    BankAccountId = sourceType == "bank" ? await ResolveBankAccountIdAsync(db, company.Id, request.BankAccountId) : null,
                    Folio = NormalizeFolio(request.Folio, "EGR"),
                    ExpenseDate = request.DocumentDate?.Date ?? DateTime.UtcNow.Date,
                    SourceType = sourceType,
                    ExchangeRate = request.ExchangeRate <= 0 ? 1m : request.ExchangeRate,
                    Status = NormalizeStatus(request.Status),
                    Reference = (request.Reference ?? string.Empty).Trim(),
                    Notes = (request.Notes ?? string.Empty).Trim(),
                    Total = 0m,
                    ApprovedAt = request.ApprovedAt,
                    PostedAt = request.PostedAt,
                    IsActive = request.IsActive,
                    CreatedBy = "web-api"
                };
                ApplyExpenseLines(entity, request.Lines);
                entity.Total = entity.Lines.Sum(x => x.Amount);
                db.TreasuryExpenses.Add(entity);
                await db.SaveChangesAsync();
                return Results.Ok(new { success = true, id = entity.Id });
            },
            update: async (id, request, db) =>
            {
                var entity = await db.TreasuryExpenses.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
                if (entity is null)
                    return Results.NotFound(new { message = "No se encontró el egreso." });

                entity.CompanyId = request.CompanyId ?? entity.CompanyId;
                entity.BranchId = request.BranchId ?? entity.BranchId;
                entity.SeriesId = request.SeriesId ?? entity.SeriesId;
                entity.CurrencyId = request.CurrencyId ?? entity.CurrencyId;
                entity.SourceType = NormalizeSourceType(request.SourceType, entity.SourceType);
                entity.CashAccountId = entity.SourceType == "cash" ? request.CashAccountId ?? entity.CashAccountId : null;
                entity.BankAccountId = entity.SourceType == "bank" ? request.BankAccountId ?? entity.BankAccountId : null;
                entity.Folio = NormalizeFolio(request.Folio, entity.Folio);
                entity.ExpenseDate = request.DocumentDate?.Date ?? entity.ExpenseDate;
                entity.ExchangeRate = request.ExchangeRate <= 0 ? entity.ExchangeRate : request.ExchangeRate;
                entity.Status = NormalizeStatus(request.Status, entity.Status);
                entity.Reference = request.Reference?.Trim() ?? entity.Reference;
                entity.Notes = request.Notes?.Trim() ?? entity.Notes;
                entity.ApprovedAt = request.ApprovedAt;
                entity.PostedAt = request.PostedAt;
                entity.IsActive = request.IsActive;
                entity.UpdatedAt = DateTime.UtcNow;
                entity.UpdatedBy = "web-api";
                ApplyExpenseLines(entity, request.Lines);
                entity.Total = entity.Lines.Sum(x => x.Amount);
                await db.SaveChangesAsync();
                return Results.Ok(new { success = true });
            });

        group.MapPost("/{id:guid}/approve", async (Guid id, NanchesoftDbContext db) => await ApproveDocumentAsync<TreasuryExpense>(id, db, "No se encontró el egreso."));
        group.MapPost("/{id:guid}/post", async (Guid id, NanchesoftDbContext db) => await PostExpenseAsync(id, db));
        group.MapPost("/{id:guid}/cancel", async (Guid id, NanchesoftDbContext db) => await CancelDocumentAsync<TreasuryExpense>(id, db, "No se encontró el egreso."));
    }

    private static void MapReceiptEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/treasury/receipts").WithTags("TreasuryReceipts");
        MapDocumentEndpoints(
            group,
            listQuery: db => db.Receipts.AsNoTracking().OrderByDescending(x => x.ReceiptDate).Select(x => new
            {
                ReceiptId = x.Id,
                x.CompanyId,
                x.BranchId,
                x.CustomerId,
                x.CashAccountId,
                x.BankAccountId,
                x.CurrencyId,
                x.Folio,
                DocumentDate = x.ReceiptDate,
                x.TargetType,
                x.Status,
                x.Reference,
                x.Total,
                x.ApprovedAt,
                x.PostedAt,
                x.IsActive
            }),
            getById: async (id, db) =>
            {
                var entity = await db.Receipts.AsNoTracking().Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
                return entity is null ? Results.NotFound(new { message = "No se encontró el recibo." }) : Results.Ok(ToRequest(entity));
            },
            create: async (request, db) =>
            {
                var company = await GetCompanyAsync(db, request.CompanyId);
                var branch = await GetBranchAsync(db, company.Id, request.BranchId);
                var targetType = NormalizeTargetType(request.TargetType);
                var entity = new Receipt
                {
                    TenantId = company.TenantId,
                    CompanyId = company.Id,
                    BranchId = branch.Id,
                    SeriesId = request.SeriesId,
                    CustomerId = request.CustomerId,
                    CurrencyId = request.CurrencyId ?? await GetDefaultCurrencyIdAsync(db),
                    CashAccountId = targetType == "cash" ? await ResolveCashAccountIdAsync(db, company.Id, request.CashAccountId) : null,
                    BankAccountId = targetType == "bank" ? await ResolveBankAccountIdAsync(db, company.Id, request.BankAccountId) : null,
                    Folio = NormalizeFolio(request.Folio, "REC"),
                    ReceiptDate = request.DocumentDate?.Date ?? DateTime.UtcNow.Date,
                    TargetType = targetType,
                    ExchangeRate = request.ExchangeRate <= 0 ? 1m : request.ExchangeRate,
                    Status = NormalizeStatus(request.Status),
                    Reference = (request.Reference ?? string.Empty).Trim(),
                    Total = 0m,
                    ApprovedAt = request.ApprovedAt,
                    PostedAt = request.PostedAt,
                    IsActive = request.IsActive,
                    CreatedBy = "web-api"
                };
                ApplyReceiptLines(entity, request.Lines);
                entity.Total = entity.Lines.Sum(x => x.Amount);
                db.Receipts.Add(entity);
                await db.SaveChangesAsync();
                return Results.Ok(new { success = true, id = entity.Id });
            },
            update: async (id, request, db) =>
            {
                var entity = await db.Receipts.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
                if (entity is null)
                    return Results.NotFound(new { message = "No se encontró el recibo." });

                entity.CompanyId = request.CompanyId ?? entity.CompanyId;
                entity.BranchId = request.BranchId ?? entity.BranchId;
                entity.SeriesId = request.SeriesId ?? entity.SeriesId;
                entity.CustomerId = request.CustomerId ?? entity.CustomerId;
                entity.CurrencyId = request.CurrencyId ?? entity.CurrencyId;
                entity.TargetType = NormalizeTargetType(request.TargetType, entity.TargetType);
                entity.CashAccountId = entity.TargetType == "cash" ? request.CashAccountId ?? entity.CashAccountId : null;
                entity.BankAccountId = entity.TargetType == "bank" ? request.BankAccountId ?? entity.BankAccountId : null;
                entity.Folio = NormalizeFolio(request.Folio, entity.Folio);
                entity.ReceiptDate = request.DocumentDate?.Date ?? entity.ReceiptDate;
                entity.ExchangeRate = request.ExchangeRate <= 0 ? entity.ExchangeRate : request.ExchangeRate;
                entity.Status = NormalizeStatus(request.Status, entity.Status);
                entity.Reference = request.Reference?.Trim() ?? entity.Reference;
                entity.ApprovedAt = request.ApprovedAt;
                entity.PostedAt = request.PostedAt;
                entity.IsActive = request.IsActive;
                entity.UpdatedAt = DateTime.UtcNow;
                entity.UpdatedBy = "web-api";
                ApplyReceiptLines(entity, request.Lines);
                entity.Total = entity.Lines.Sum(x => x.Amount);
                await db.SaveChangesAsync();
                return Results.Ok(new { success = true });
            });

        group.MapPost("/{id:guid}/approve", async (Guid id, NanchesoftDbContext db) => await ApproveDocumentAsync<Receipt>(id, db, "No se encontró el recibo."));
        group.MapPost("/{id:guid}/post", async (Guid id, NanchesoftDbContext db) => await PostReceiptAsync(id, db));
        group.MapPost("/{id:guid}/cancel", async (Guid id, NanchesoftDbContext db) => await CancelDocumentAsync<Receipt>(id, db, "No se encontró el recibo."));
    }

    private static void MapPaymentEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/treasury/payments").WithTags("TreasuryPayments");
        MapDocumentEndpoints(
            group,
            listQuery: db => db.Payments.AsNoTracking().OrderByDescending(x => x.PaymentDate).Select(x => new
            {
                PaymentId = x.Id,
                x.CompanyId,
                x.BranchId,
                x.SupplierId,
                x.CashAccountId,
                x.BankAccountId,
                x.CurrencyId,
                x.Folio,
                DocumentDate = x.PaymentDate,
                x.SourceType,
                x.Status,
                x.Reference,
                x.Total,
                x.ApprovedAt,
                x.PostedAt,
                x.IsActive
            }),
            getById: async (id, db) =>
            {
                var entity = await db.Payments.AsNoTracking().Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
                return entity is null ? Results.NotFound(new { message = "No se encontró el pago." }) : Results.Ok(ToRequest(entity));
            },
            create: async (request, db) =>
            {
                var company = await GetCompanyAsync(db, request.CompanyId);
                var branch = await GetBranchAsync(db, company.Id, request.BranchId);
                var sourceType = NormalizeSourceType(request.SourceType);
                var entity = new Payment
                {
                    TenantId = company.TenantId,
                    CompanyId = company.Id,
                    BranchId = branch.Id,
                    SeriesId = request.SeriesId,
                    SupplierId = request.SupplierId,
                    CurrencyId = request.CurrencyId ?? await GetDefaultCurrencyIdAsync(db),
                    CashAccountId = sourceType == "cash" ? await ResolveCashAccountIdAsync(db, company.Id, request.CashAccountId) : null,
                    BankAccountId = sourceType == "bank" ? await ResolveBankAccountIdAsync(db, company.Id, request.BankAccountId) : null,
                    Folio = NormalizeFolio(request.Folio, "PAG"),
                    PaymentDate = request.DocumentDate?.Date ?? DateTime.UtcNow.Date,
                    SourceType = sourceType,
                    ExchangeRate = request.ExchangeRate <= 0 ? 1m : request.ExchangeRate,
                    Status = NormalizeStatus(request.Status),
                    Reference = (request.Reference ?? string.Empty).Trim(),
                    Total = 0m,
                    ApprovedAt = request.ApprovedAt,
                    PostedAt = request.PostedAt,
                    IsActive = request.IsActive,
                    CreatedBy = "web-api"
                };
                ApplyPaymentLines(entity, request.Lines);
                entity.Total = entity.Lines.Sum(x => x.Amount);
                db.Payments.Add(entity);
                await db.SaveChangesAsync();
                return Results.Ok(new { success = true, id = entity.Id });
            },
            update: async (id, request, db) =>
            {
                var entity = await db.Payments.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
                if (entity is null)
                    return Results.NotFound(new { message = "No se encontró el pago." });

                entity.CompanyId = request.CompanyId ?? entity.CompanyId;
                entity.BranchId = request.BranchId ?? entity.BranchId;
                entity.SeriesId = request.SeriesId ?? entity.SeriesId;
                entity.SupplierId = request.SupplierId ?? entity.SupplierId;
                entity.CurrencyId = request.CurrencyId ?? entity.CurrencyId;
                entity.SourceType = NormalizeSourceType(request.SourceType, entity.SourceType);
                entity.CashAccountId = entity.SourceType == "cash" ? request.CashAccountId ?? entity.CashAccountId : null;
                entity.BankAccountId = entity.SourceType == "bank" ? request.BankAccountId ?? entity.BankAccountId : null;
                entity.Folio = NormalizeFolio(request.Folio, entity.Folio);
                entity.PaymentDate = request.DocumentDate?.Date ?? entity.PaymentDate;
                entity.ExchangeRate = request.ExchangeRate <= 0 ? entity.ExchangeRate : request.ExchangeRate;
                entity.Status = NormalizeStatus(request.Status, entity.Status);
                entity.Reference = request.Reference?.Trim() ?? entity.Reference;
                entity.ApprovedAt = request.ApprovedAt;
                entity.PostedAt = request.PostedAt;
                entity.IsActive = request.IsActive;
                entity.UpdatedAt = DateTime.UtcNow;
                entity.UpdatedBy = "web-api";
                ApplyPaymentLines(entity, request.Lines);
                entity.Total = entity.Lines.Sum(x => x.Amount);
                await db.SaveChangesAsync();
                return Results.Ok(new { success = true });
            });

        group.MapPost("/{id:guid}/approve", async (Guid id, NanchesoftDbContext db) => await ApproveDocumentAsync<Payment>(id, db, "No se encontró el pago."));
        group.MapPost("/{id:guid}/post", async (Guid id, NanchesoftDbContext db) => await PostPaymentAsync(id, db));
        group.MapPost("/{id:guid}/cancel", async (Guid id, NanchesoftDbContext db) => await CancelDocumentAsync<Payment>(id, db, "No se encontró el pago."));
    }

    private static void MapReconciliationEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/treasury/reconciliations").WithTags("TreasuryReconciliations");

        group.MapGet("/", async (NanchesoftDbContext db) =>
            Results.Ok(await db.Reconciliations.AsNoTracking()
                .OrderByDescending(x => x.ReconciliationDate)
                .Select(x => new
                {
                    ReconciliationId = x.Id,
                    x.CompanyId,
                    x.BankAccountId,
                    x.ReconciliationDate,
                    x.StatementBalance,
                    x.BookBalance,
                    x.DifferenceAmount,
                    x.Status,
                    x.ClosedAt,
                    x.IsActive
                }).ToListAsync()));

        group.MapGet("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.Reconciliations.AsNoTracking().Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null)
                return Results.NotFound(new { message = "No se encontró la conciliación." });

            return Results.Ok(new ReconciliationRequest
            {
                ReconciliationId = entity.Id,
                CompanyId = entity.CompanyId,
                BankAccountId = entity.BankAccountId,
                ReconciliationDate = entity.ReconciliationDate,
                StatementBalance = entity.StatementBalance,
                BookBalance = entity.BookBalance,
                DifferenceAmount = entity.DifferenceAmount,
                Status = entity.Status,
                ClosedAt = entity.ClosedAt,
                IsActive = entity.IsActive,
                Lines = entity.Lines.Select(x => new ReconciliationLineRequest
                {
                    Id = x.Id,
                    BankMovementId = x.BankMovementId,
                    IsChecked = x.IsChecked,
                    MovementAmount = x.MovementAmount
                }).ToList()
            });
        });

        group.MapPost("/", async (ReconciliationRequest request, NanchesoftDbContext db) =>
        {
            var company = await GetCompanyAsync(db, request.CompanyId);
            var bankAccountId = (await ResolveBankAccountIdAsync(db, company.Id, request.BankAccountId)) ?? Guid.Empty;
            if (bankAccountId == Guid.Empty)
                return Results.BadRequest(new { message = "No existe una cuenta bancaria disponible." });

            var movementIds = request.Lines.Where(x => x.BankMovementId.HasValue).Select(x => x.BankMovementId!.Value).Distinct().ToList();
            var movements = movementIds.Count > 0
                ? await db.BankMovements.Where(x => movementIds.Contains(x.Id)).ToListAsync()
                : await db.BankMovements.Where(x => x.BankAccountId == bankAccountId && !x.IsReconciled).OrderBy(x => x.MovementDate).ToListAsync();

            var entity = new Reconciliation
            {
                TenantId = company.TenantId,
                CompanyId = company.Id,
                BankAccountId = bankAccountId,
                ReconciliationDate = request.ReconciliationDate?.Date ?? DateTime.UtcNow.Date,
                StatementBalance = request.StatementBalance,
                Status = NormalizeStatus(request.Status, "in_progress"),
                IsActive = request.IsActive,
                CreatedBy = "web-api",
                Lines = movements.Select(x => new ReconciliationLine
                {
                    Id = Guid.NewGuid(),
                    BankMovementId = x.Id,
                    IsChecked = request.Lines.Any(l => l.BankMovementId == x.Id && l.IsChecked) || x.IsReconciled,
                    MovementAmount = x.AmountIn - x.AmountOut,
                    CreatedBy = "web-api"
                }).ToList()
            };
            entity.BookBalance = entity.Lines.Where(x => x.IsChecked).Sum(x => x.MovementAmount);
            entity.DifferenceAmount = entity.StatementBalance - entity.BookBalance;
            db.Reconciliations.Add(entity);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true, id = entity.Id });
        });

        group.MapPost("/{id:guid}/close", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.Reconciliations.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null)
                return Results.NotFound(new { message = "No se encontró la conciliación." });

            var movementIds = entity.Lines.Where(x => x.IsChecked).Select(x => x.BankMovementId).ToList();
            var movements = await db.BankMovements.Where(x => movementIds.Contains(x.Id)).ToListAsync();
            foreach (var movement in movements)
            {
                movement.IsReconciled = true;
                movement.UpdatedAt = DateTime.UtcNow;
                movement.UpdatedBy = "web-api";
            }

            entity.BookBalance = entity.Lines.Where(x => x.IsChecked).Sum(x => x.MovementAmount);
            entity.DifferenceAmount = entity.StatementBalance - entity.BookBalance;
            entity.Status = "closed";
            entity.ClosedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = "web-api";
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });
    }

    private static void MapLookupsAndDashboard(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/treasury/lookups", async (NanchesoftDbContext db) =>
            Results.Ok(new TreasuryLookupsResponse
            {
                Companies = await db.Companies.AsNoTracking().OrderBy(x => x.Name).Select(x => new TreasuryLookupItem(x.Id, x.Name)).ToListAsync(),
                Branches = await db.Branches.AsNoTracking().OrderBy(x => x.Name).Select(x => new TreasuryLookupItem(x.Id, x.Name)).ToListAsync(),
                Currencies = await db.Currencies.AsNoTracking().OrderBy(x => x.Name).Select(x => new TreasuryLookupItem(x.Id, x.Name)).ToListAsync(),
                Banks = await db.Banks.AsNoTracking().OrderBy(x => x.Name).Select(x => new TreasuryLookupItem(x.Id, x.Name)).ToListAsync(),
                CashAccounts = await db.CashAccounts.AsNoTracking().OrderBy(x => x.Name).Select(x => new TreasuryLookupItem(x.Id, x.Name)).ToListAsync(),
                BankAccounts = await db.BankAccounts.AsNoTracking().OrderBy(x => x.Name).Select(x => new TreasuryLookupItem(x.Id, x.Name)).ToListAsync(),
                Customers = await db.Customers.AsNoTracking().OrderBy(x => x.Name).Select(x => new TreasuryLookupItem(x.Id, x.Name)).ToListAsync(),
                Suppliers = await db.Suppliers.AsNoTracking().OrderBy(x => x.Name).Select(x => new TreasuryLookupItem(x.Id, x.Name)).ToListAsync(),
                SalesInvoices = await db.SalesInvoices.AsNoTracking().OrderByDescending(x => x.InvoiceDate).Select(x => new TreasuryLookupItem(x.Id, x.Folio)).ToListAsync(),
                PurchaseInvoices = await db.PurchaseInvoices.AsNoTracking().OrderByDescending(x => x.InvoiceDate).Select(x => new TreasuryLookupItem(x.Id, x.Folio)).ToListAsync()
            })).WithTags("TreasuryLookups");

        app.MapGet("/api/treasury/dashboard/summary", async (NanchesoftDbContext db) =>
        {
            var inflow = await db.BankMovements.SumAsync(x => x.AmountIn) + await db.CashMovements.SumAsync(x => x.AmountIn);
            var outflow = await db.BankMovements.SumAsync(x => x.AmountOut) + await db.CashMovements.SumAsync(x => x.AmountOut);
            return Results.Ok(new TreasuryDashboardSummary
            {
                CashAccounts = await db.CashAccounts.CountAsync(),
                BankAccounts = await db.BankAccounts.CountAsync(),
                CashBalance = await db.CashAccounts.SumAsync(x => x.CurrentBalance),
                BankBalance = await db.BankAccounts.SumAsync(x => x.CurrentBalance),
                PeriodInflow = inflow,
                PeriodOutflow = outflow,
                PendingReconciliations = await db.Reconciliations.CountAsync(x => x.Status != "closed" && x.Status != "cancelled")
            });
        }).WithTags("TreasuryDashboard");

        app.MapGet("/api/treasury/dashboard/balances", async (NanchesoftDbContext db) =>
        {
            var cash = await db.CashAccounts.AsNoTracking().OrderBy(x => x.Name).Select(x => new TreasuryBalanceRow
            {
                Id = x.Id,
                Type = "cash",
                Code = x.Code,
                Name = x.Name,
                Balance = x.CurrentBalance,
                Status = x.Status
            }).ToListAsync();
            var bank = await db.BankAccounts.AsNoTracking().OrderBy(x => x.Name).Select(x => new TreasuryBalanceRow
            {
                Id = x.Id,
                Type = "bank",
                Code = x.Code,
                Name = x.Name,
                Balance = x.CurrentBalance,
                Status = x.Status
            }).ToListAsync();
            return Results.Ok(cash.Concat(bank));
        }).WithTags("TreasuryDashboard");

        app.MapGet("/api/treasury/dashboard/recent", async (NanchesoftDbContext db) =>
        {
            var cashRecent = await db.CashMovements.AsNoTracking().OrderByDescending(x => x.MovementDate).Take(10).Select(x => new TreasuryRecentRow
            {
                Id = x.Id,
                Source = "cash",
                MovementDate = x.MovementDate,
                MovementType = x.MovementType,
                DocumentType = x.DocumentType,
                Reference = x.Reference,
                AmountIn = x.AmountIn,
                AmountOut = x.AmountOut,
                BalanceAfter = x.BalanceAfter
            }).ToListAsync();
            var bankRecent = await db.BankMovements.AsNoTracking().OrderByDescending(x => x.MovementDate).Take(10).Select(x => new TreasuryRecentRow
            {
                Id = x.Id,
                Source = "bank",
                MovementDate = x.MovementDate,
                MovementType = x.MovementType,
                DocumentType = x.DocumentType,
                Reference = x.Reference,
                AmountIn = x.AmountIn,
                AmountOut = x.AmountOut,
                BalanceAfter = x.BalanceAfter
            }).ToListAsync();
            return Results.Ok(cashRecent.Concat(bankRecent).OrderByDescending(x => x.MovementDate).Take(15));
        }).WithTags("TreasuryDashboard");
    }

    private static void MapDocumentEndpoints<TProjection>(RouteGroupBuilder group,
        Func<NanchesoftDbContext, IQueryable<TProjection>> listQuery,
        Func<Guid, NanchesoftDbContext, Task<IResult>> getById,
        Func<TreasuryDocumentRequest, NanchesoftDbContext, Task<IResult>> create,
        Func<Guid, TreasuryDocumentRequest, NanchesoftDbContext, Task<IResult>> update)
    {
        group.MapGet("/", async (NanchesoftDbContext db) => Results.Ok(await listQuery(db).ToListAsync()));
        group.MapGet("/{id:guid}", getById);
        group.MapPost("/", create);
        group.MapPut("/{id:guid}", update);
    }

    private static TreasuryDocumentRequest ToRequest(TreasuryIncome entity) => new()
    {
        TreasuryIncomeId = entity.Id,
        CompanyId = entity.CompanyId,
        BranchId = entity.BranchId,
        SeriesId = entity.SeriesId,
        CurrencyId = entity.CurrencyId,
        CashAccountId = entity.CashAccountId,
        BankAccountId = entity.BankAccountId,
        Folio = entity.Folio,
        DocumentDate = entity.IncomeDate,
        TargetType = entity.TargetType,
        ExchangeRate = entity.ExchangeRate,
        Status = entity.Status,
        Reference = entity.Reference,
        Notes = entity.Notes,
        Total = entity.Total,
        ApprovedAt = entity.ApprovedAt,
        PostedAt = entity.PostedAt,
        IsActive = entity.IsActive,
        Lines = entity.Lines.OrderBy(x => x.LineNumber).Select(x => new TreasuryLineRequest { Id = x.Id, LineNumber = x.LineNumber, Description = x.Description, Amount = x.Amount, CustomerId = x.CustomerId, SalesInvoiceId = x.SalesInvoiceId }).ToList()
    };

    private static TreasuryDocumentRequest ToRequest(TreasuryExpense entity) => new()
    {
        TreasuryExpenseId = entity.Id,
        CompanyId = entity.CompanyId,
        BranchId = entity.BranchId,
        SeriesId = entity.SeriesId,
        CurrencyId = entity.CurrencyId,
        CashAccountId = entity.CashAccountId,
        BankAccountId = entity.BankAccountId,
        Folio = entity.Folio,
        DocumentDate = entity.ExpenseDate,
        SourceType = entity.SourceType,
        ExchangeRate = entity.ExchangeRate,
        Status = entity.Status,
        Reference = entity.Reference,
        Notes = entity.Notes,
        Total = entity.Total,
        ApprovedAt = entity.ApprovedAt,
        PostedAt = entity.PostedAt,
        IsActive = entity.IsActive,
        Lines = entity.Lines.OrderBy(x => x.LineNumber).Select(x => new TreasuryLineRequest { Id = x.Id, LineNumber = x.LineNumber, Description = x.Description, Amount = x.Amount, SupplierId = x.SupplierId, PurchaseInvoiceId = x.PurchaseInvoiceId }).ToList()
    };

    private static TreasuryDocumentRequest ToRequest(Receipt entity) => new()
    {
        ReceiptId = entity.Id,
        CompanyId = entity.CompanyId,
        BranchId = entity.BranchId,
        SeriesId = entity.SeriesId,
        CustomerId = entity.CustomerId,
        CurrencyId = entity.CurrencyId,
        CashAccountId = entity.CashAccountId,
        BankAccountId = entity.BankAccountId,
        Folio = entity.Folio,
        DocumentDate = entity.ReceiptDate,
        TargetType = entity.TargetType,
        ExchangeRate = entity.ExchangeRate,
        Status = entity.Status,
        Reference = entity.Reference,
        Total = entity.Total,
        ApprovedAt = entity.ApprovedAt,
        PostedAt = entity.PostedAt,
        IsActive = entity.IsActive,
        Lines = entity.Lines.OrderBy(x => x.LineNumber).Select(x => new TreasuryLineRequest { Id = x.Id, LineNumber = x.LineNumber, Description = x.Description, Amount = x.Amount, SalesInvoiceId = x.SalesInvoiceId }).ToList()
    };

    private static TreasuryDocumentRequest ToRequest(Payment entity) => new()
    {
        PaymentId = entity.Id,
        CompanyId = entity.CompanyId,
        BranchId = entity.BranchId,
        SeriesId = entity.SeriesId,
        SupplierId = entity.SupplierId,
        CurrencyId = entity.CurrencyId,
        CashAccountId = entity.CashAccountId,
        BankAccountId = entity.BankAccountId,
        Folio = entity.Folio,
        DocumentDate = entity.PaymentDate,
        SourceType = entity.SourceType,
        ExchangeRate = entity.ExchangeRate,
        Status = entity.Status,
        Reference = entity.Reference,
        Total = entity.Total,
        ApprovedAt = entity.ApprovedAt,
        PostedAt = entity.PostedAt,
        IsActive = entity.IsActive,
        Lines = entity.Lines.OrderBy(x => x.LineNumber).Select(x => new TreasuryLineRequest { Id = x.Id, LineNumber = x.LineNumber, Description = x.Description, Amount = x.Amount, PurchaseInvoiceId = x.PurchaseInvoiceId }).ToList()
    };

    private static void ApplyIncomeLines(TreasuryIncome entity, IEnumerable<TreasuryLineRequest> lines)
    {
        entity.Lines.Clear();
        foreach (var line in NormalizeLines(lines))
            entity.Lines.Add(new TreasuryIncomeLine { Id = line.Id ?? Guid.NewGuid(), TreasuryIncomeId = entity.Id, LineNumber = line.LineNumber, Description = line.Description?.Trim() ?? string.Empty, Amount = line.Amount, CustomerId = line.CustomerId, SalesInvoiceId = line.SalesInvoiceId, CreatedBy = "web-api" });
    }

    private static void ApplyExpenseLines(TreasuryExpense entity, IEnumerable<TreasuryLineRequest> lines)
    {
        entity.Lines.Clear();
        foreach (var line in NormalizeLines(lines))
            entity.Lines.Add(new TreasuryExpenseLine { Id = line.Id ?? Guid.NewGuid(), TreasuryExpenseId = entity.Id, LineNumber = line.LineNumber, Description = line.Description?.Trim() ?? string.Empty, Amount = line.Amount, SupplierId = line.SupplierId, PurchaseInvoiceId = line.PurchaseInvoiceId, CreatedBy = "web-api" });
    }

    private static void ApplyReceiptLines(Receipt entity, IEnumerable<TreasuryLineRequest> lines)
    {
        entity.Lines.Clear();
        foreach (var line in NormalizeLines(lines))
            entity.Lines.Add(new ReceiptLine { Id = line.Id ?? Guid.NewGuid(), ReceiptId = entity.Id, LineNumber = line.LineNumber, Description = line.Description?.Trim() ?? string.Empty, Amount = line.Amount, SalesInvoiceId = line.SalesInvoiceId, CreatedBy = "web-api" });
    }

    private static void ApplyPaymentLines(Payment entity, IEnumerable<TreasuryLineRequest> lines)
    {
        entity.Lines.Clear();
        foreach (var line in NormalizeLines(lines))
            entity.Lines.Add(new PaymentLine { Id = line.Id ?? Guid.NewGuid(), PaymentId = entity.Id, LineNumber = line.LineNumber, Description = line.Description?.Trim() ?? string.Empty, Amount = line.Amount, PurchaseInvoiceId = line.PurchaseInvoiceId, CreatedBy = "web-api" });
    }

    private static IEnumerable<TreasuryLineRequest> NormalizeLines(IEnumerable<TreasuryLineRequest> lines) =>
        lines.Where(x => !string.IsNullOrWhiteSpace(x.Description) || x.Amount != 0m)
             .Select((x, index) => { x.LineNumber = x.LineNumber <= 0 ? index + 1 : x.LineNumber; return x; })
             .OrderBy(x => x.LineNumber)
             .ToList();

    private static async Task<IResult> PostIncomeAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.TreasuryIncomes.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null) return Results.NotFound(new { message = "No se encontró el ingreso." });
        if (entity.PostedAt.HasValue || entity.Status == "posted") return Results.Ok(new { success = true, message = "El ingreso ya estaba posteado." });

        entity.Total = entity.Lines.Sum(x => x.Amount);
        entity.Status = "posted";
        entity.PostedAt = DateTime.UtcNow;
        entity.ApprovedAt ??= DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        if (entity.TargetType == "bank")
        {
            var account = await db.BankAccounts.FirstAsync(x => x.Id == entity.BankAccountId);
            account.CurrentBalance += entity.Total;
            account.UpdatedAt = DateTime.UtcNow;
            account.UpdatedBy = "web-api";
            db.BankMovements.Add(new BankMovement { TenantId = entity.TenantId, CompanyId = entity.CompanyId, BankAccountId = account.Id, MovementDate = entity.PostedAt.Value, MovementType = "income", DocumentType = "treasury_income", DocumentId = entity.Id, Reference = entity.Reference, AmountIn = entity.Total, AmountOut = 0m, BalanceAfter = account.CurrentBalance, CreatedBy = "web-api" });
        }
        else
        {
            var account = await db.CashAccounts.FirstAsync(x => x.Id == entity.CashAccountId);
            account.CurrentBalance += entity.Total;
            account.UpdatedAt = DateTime.UtcNow;
            account.UpdatedBy = "web-api";
            db.CashMovements.Add(new CashMovement { TenantId = entity.TenantId, CompanyId = entity.CompanyId, BranchId = entity.BranchId, CashAccountId = account.Id, MovementDate = entity.PostedAt.Value, MovementType = "income", DocumentType = "treasury_income", DocumentId = entity.Id, Reference = entity.Reference, AmountIn = entity.Total, AmountOut = 0m, BalanceAfter = account.CurrentBalance, CreatedBy = "web-api" });
        }
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> PostExpenseAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.TreasuryExpenses.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null) return Results.NotFound(new { message = "No se encontró el egreso." });
        if (entity.PostedAt.HasValue || entity.Status == "posted") return Results.Ok(new { success = true, message = "El egreso ya estaba posteado." });

        entity.Total = entity.Lines.Sum(x => x.Amount);
        entity.Status = "posted";
        entity.PostedAt = DateTime.UtcNow;
        entity.ApprovedAt ??= DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        if (entity.SourceType == "bank")
        {
            var account = await db.BankAccounts.FirstAsync(x => x.Id == entity.BankAccountId);
            if (account.CurrentBalance < entity.Total) return Results.BadRequest(new { message = "Saldo insuficiente en cuenta bancaria." });
            account.CurrentBalance -= entity.Total;
            account.UpdatedAt = DateTime.UtcNow;
            account.UpdatedBy = "web-api";
            db.BankMovements.Add(new BankMovement { TenantId = entity.TenantId, CompanyId = entity.CompanyId, BankAccountId = account.Id, MovementDate = entity.PostedAt.Value, MovementType = "expense", DocumentType = "treasury_expense", DocumentId = entity.Id, Reference = entity.Reference, AmountIn = 0m, AmountOut = entity.Total, BalanceAfter = account.CurrentBalance, CreatedBy = "web-api" });
        }
        else
        {
            var account = await db.CashAccounts.FirstAsync(x => x.Id == entity.CashAccountId);
            if (account.CurrentBalance < entity.Total) return Results.BadRequest(new { message = "Saldo insuficiente en caja." });
            account.CurrentBalance -= entity.Total;
            account.UpdatedAt = DateTime.UtcNow;
            account.UpdatedBy = "web-api";
            db.CashMovements.Add(new CashMovement { TenantId = entity.TenantId, CompanyId = entity.CompanyId, BranchId = entity.BranchId, CashAccountId = account.Id, MovementDate = entity.PostedAt.Value, MovementType = "expense", DocumentType = "treasury_expense", DocumentId = entity.Id, Reference = entity.Reference, AmountIn = 0m, AmountOut = entity.Total, BalanceAfter = account.CurrentBalance, CreatedBy = "web-api" });
        }
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> PostReceiptAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.Receipts.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null) return Results.NotFound(new { message = "No se encontró el recibo." });
        if (entity.PostedAt.HasValue || entity.Status == "posted") return Results.Ok(new { success = true, message = "El recibo ya estaba posteado." });

        entity.Total = entity.Lines.Sum(x => x.Amount);
        entity.Status = "posted";
        entity.PostedAt = DateTime.UtcNow;
        entity.ApprovedAt ??= DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        if (entity.TargetType == "bank")
        {
            var account = await db.BankAccounts.FirstAsync(x => x.Id == entity.BankAccountId);
            account.CurrentBalance += entity.Total;
            account.UpdatedAt = DateTime.UtcNow;
            account.UpdatedBy = "web-api";
            db.BankMovements.Add(new BankMovement { TenantId = entity.TenantId, CompanyId = entity.CompanyId, BankAccountId = account.Id, MovementDate = entity.PostedAt.Value, MovementType = "receipt", DocumentType = "receipt", DocumentId = entity.Id, Reference = entity.Reference, AmountIn = entity.Total, AmountOut = 0m, BalanceAfter = account.CurrentBalance, CreatedBy = "web-api" });
        }
        else
        {
            var account = await db.CashAccounts.FirstAsync(x => x.Id == entity.CashAccountId);
            account.CurrentBalance += entity.Total;
            account.UpdatedAt = DateTime.UtcNow;
            account.UpdatedBy = "web-api";
            db.CashMovements.Add(new CashMovement { TenantId = entity.TenantId, CompanyId = entity.CompanyId, BranchId = entity.BranchId, CashAccountId = account.Id, MovementDate = entity.PostedAt.Value, MovementType = "receipt", DocumentType = "receipt", DocumentId = entity.Id, Reference = entity.Reference, AmountIn = entity.Total, AmountOut = 0m, BalanceAfter = account.CurrentBalance, CreatedBy = "web-api" });
        }
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> PostPaymentAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.Payments.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null) return Results.NotFound(new { message = "No se encontró el pago." });
        if (entity.PostedAt.HasValue || entity.Status == "posted") return Results.Ok(new { success = true, message = "El pago ya estaba posteado." });

        entity.Total = entity.Lines.Sum(x => x.Amount);
        entity.Status = "posted";
        entity.PostedAt = DateTime.UtcNow;
        entity.ApprovedAt ??= DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        if (entity.SourceType == "bank")
        {
            var account = await db.BankAccounts.FirstAsync(x => x.Id == entity.BankAccountId);
            if (account.CurrentBalance < entity.Total) return Results.BadRequest(new { message = "Saldo insuficiente en cuenta bancaria." });
            account.CurrentBalance -= entity.Total;
            account.UpdatedAt = DateTime.UtcNow;
            account.UpdatedBy = "web-api";
            db.BankMovements.Add(new BankMovement { TenantId = entity.TenantId, CompanyId = entity.CompanyId, BankAccountId = account.Id, MovementDate = entity.PostedAt.Value, MovementType = "payment", DocumentType = "payment", DocumentId = entity.Id, Reference = entity.Reference, AmountIn = 0m, AmountOut = entity.Total, BalanceAfter = account.CurrentBalance, CreatedBy = "web-api" });
        }
        else
        {
            var account = await db.CashAccounts.FirstAsync(x => x.Id == entity.CashAccountId);
            if (account.CurrentBalance < entity.Total) return Results.BadRequest(new { message = "Saldo insuficiente en caja." });
            account.CurrentBalance -= entity.Total;
            account.UpdatedAt = DateTime.UtcNow;
            account.UpdatedBy = "web-api";
            db.CashMovements.Add(new CashMovement { TenantId = entity.TenantId, CompanyId = entity.CompanyId, BranchId = entity.BranchId, CashAccountId = account.Id, MovementDate = entity.PostedAt.Value, MovementType = "payment", DocumentType = "payment", DocumentId = entity.Id, Reference = entity.Reference, AmountIn = 0m, AmountOut = entity.Total, BalanceAfter = account.CurrentBalance, CreatedBy = "web-api" });
        }
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> ApproveDocumentAsync<TEntity>(Guid id, NanchesoftDbContext db, string notFoundMessage) where TEntity : BaseEntity
    {
        var entity = await db.Set<TEntity>().FindAsync(id);
        if (entity is null) return Results.NotFound(new { message = notFoundMessage });

        switch (entity)
        {
            case TreasuryIncome income:
                income.Status = "approved";
                income.ApprovedAt = DateTime.UtcNow;
                income.UpdatedAt = DateTime.UtcNow;
                income.UpdatedBy = "web-api";
                break;
            case TreasuryExpense expense:
                expense.Status = "approved";
                expense.ApprovedAt = DateTime.UtcNow;
                expense.UpdatedAt = DateTime.UtcNow;
                expense.UpdatedBy = "web-api";
                break;
            case Receipt receipt:
                receipt.Status = "approved";
                receipt.ApprovedAt = DateTime.UtcNow;
                receipt.UpdatedAt = DateTime.UtcNow;
                receipt.UpdatedBy = "web-api";
                break;
            case Payment payment:
                payment.Status = "approved";
                payment.ApprovedAt = DateTime.UtcNow;
                payment.UpdatedAt = DateTime.UtcNow;
                payment.UpdatedBy = "web-api";
                break;
            default:
                return Results.BadRequest(new { message = "Tipo de documento no soportado." });
        }

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> CancelDocumentAsync<TEntity>(Guid id, NanchesoftDbContext db, string notFoundMessage) where TEntity : BaseEntity
    {
        var entity = await db.Set<TEntity>().FindAsync(id);
        if (entity is null) return Results.NotFound(new { message = notFoundMessage });

        switch (entity)
        {
            case TreasuryIncome income when income.Status == "posted":
            case TreasuryExpense expense when expense.Status == "posted":
            case Receipt receipt when receipt.Status == "posted":
            case Payment payment when payment.Status == "posted":
                return Results.BadRequest(new { message = "No se puede cancelar un documento ya posteado sin reversión." });
            case TreasuryIncome income:
                income.Status = "cancelled"; income.UpdatedAt = DateTime.UtcNow; income.UpdatedBy = "web-api"; break;
            case TreasuryExpense expense:
                expense.Status = "cancelled"; expense.UpdatedAt = DateTime.UtcNow; expense.UpdatedBy = "web-api"; break;
            case Receipt receipt:
                receipt.Status = "cancelled"; receipt.UpdatedAt = DateTime.UtcNow; receipt.UpdatedBy = "web-api"; break;
            case Payment payment:
                payment.Status = "cancelled"; payment.UpdatedAt = DateTime.UtcNow; payment.UpdatedBy = "web-api"; break;
            default:
                return Results.BadRequest(new { message = "Tipo de documento no soportado." });
        }

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> SetActiveAsync<TEntity>(Guid id, NanchesoftDbContext db, bool isActive, string notFoundMessage) where TEntity : BaseEntity
    {
        var entity = await db.Set<TEntity>().FindAsync(id);
        if (entity is null) return Results.NotFound(new { message = notFoundMessage });
        entity.IsActive = isActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<Company> GetCompanyAsync(NanchesoftDbContext db, Guid? companyId)
    {
        if (companyId.HasValue)
        {
            var company = await db.Companies.FirstOrDefaultAsync(x => x.Id == companyId.Value);
            if (company is not null) return company;
        }
        return await db.Companies.OrderBy(x => x.CreatedAt).FirstAsync();
    }

    private static async Task<Branch> GetBranchAsync(NanchesoftDbContext db, Guid companyId, Guid? branchId)
    {
        if (branchId.HasValue)
        {
            var branch = await db.Branches.FirstOrDefaultAsync(x => x.Id == branchId.Value);
            if (branch is not null) return branch;
        }
        return await db.Branches.Where(x => x.CompanyId == companyId).OrderBy(x => x.CreatedAt).FirstAsync();
    }

    private static async Task<Guid> GetDefaultCurrencyIdAsync(NanchesoftDbContext db) => (await db.Currencies.OrderBy(x => x.CreatedAt).FirstAsync()).Id;

    private static async Task<Guid?> ResolveCashAccountIdAsync(NanchesoftDbContext db, Guid companyId, Guid? cashAccountId)
    {
        if (cashAccountId.HasValue) return cashAccountId.Value;
        return await db.CashAccounts.Where(x => x.CompanyId == companyId).OrderBy(x => x.CreatedAt).Select(x => x.Id).FirstOrDefaultAsync();
    }

    private static async Task<Guid?> ResolveBankAccountIdAsync(NanchesoftDbContext db, Guid companyId, Guid? bankAccountId)
    {
        if (bankAccountId.HasValue) return bankAccountId.Value;
        return await db.BankAccounts.Where(x => x.CompanyId == companyId).OrderBy(x => x.CreatedAt).Select(x => x.Id).FirstOrDefaultAsync();
    }

    private static string NormalizeFolio(string? folio, string defaultPrefix) => string.IsNullOrWhiteSpace(folio) ? $"{defaultPrefix}-{DateTime.UtcNow:yyyyMMddHHmmss}" : folio.Trim();
    private static string NormalizeCode(string? code, string defaultCode) => string.IsNullOrWhiteSpace(code) ? defaultCode : code.Trim().ToUpperInvariant();
    private static string NormalizeStatus(string? status, string fallback = "draft") => string.IsNullOrWhiteSpace(status) ? fallback : status.Trim().ToLowerInvariant();
    private static string NormalizeTargetType(string? targetType, string fallback = "cash") => string.IsNullOrWhiteSpace(targetType) ? fallback : targetType.Trim().ToLowerInvariant() == "bank" ? "bank" : "cash";
    private static string NormalizeSourceType(string? sourceType, string fallback = "cash") => string.IsNullOrWhiteSpace(sourceType) ? fallback : sourceType.Trim().ToLowerInvariant() == "bank" ? "bank" : "cash";
}

public sealed class CashAccountRequest
{
    public Guid? CashAccountId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? CurrencyId { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Status { get; set; }
    public decimal CurrentBalance { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class TreasuryBankAccountRequest
{
    public Guid? BankAccountId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BankId { get; set; }
    public Guid? CurrencyId { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? AccountHolder { get; set; }
    public string? AccountNumber { get; set; }
    public string? Clabe { get; set; }
    public string? Status { get; set; }
    public decimal CurrentBalance { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class TreasuryDocumentRequest
{
    public Guid? TreasuryIncomeId { get; set; }
    public Guid? TreasuryExpenseId { get; set; }
    public Guid? ReceiptId { get; set; }
    public Guid? PaymentId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? SeriesId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? SupplierId { get; set; }
    public Guid? CurrencyId { get; set; }
    public Guid? CashAccountId { get; set; }
    public Guid? BankAccountId { get; set; }
    public string? Folio { get; set; }
    public DateTime? DocumentDate { get; set; }
    public string? TargetType { get; set; }
    public string? SourceType { get; set; }
    public decimal ExchangeRate { get; set; } = 1m;
    public string? Status { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public decimal Total { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? PostedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public List<TreasuryLineRequest> Lines { get; set; } = new();
}

public sealed class TreasuryLineRequest
{
    public Guid? Id { get; set; }
    public int LineNumber { get; set; }
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? SupplierId { get; set; }
    public Guid? SalesInvoiceId { get; set; }
    public Guid? PurchaseInvoiceId { get; set; }
}

public sealed class ReconciliationRequest
{
    public Guid? ReconciliationId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BankAccountId { get; set; }
    public DateTime? ReconciliationDate { get; set; }
    public decimal StatementBalance { get; set; }
    public decimal BookBalance { get; set; }
    public decimal DifferenceAmount { get; set; }
    public string? Status { get; set; }
    public DateTime? ClosedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public List<ReconciliationLineRequest> Lines { get; set; } = new();
}

public sealed class ReconciliationLineRequest
{
    public Guid? Id { get; set; }
    public Guid? BankMovementId { get; set; }
    public bool IsChecked { get; set; }
    public decimal MovementAmount { get; set; }
}

public sealed class TreasuryLookupsResponse
{
    public List<TreasuryLookupItem> Companies { get; set; } = new();
    public List<TreasuryLookupItem> Branches { get; set; } = new();
    public List<TreasuryLookupItem> Currencies { get; set; } = new();
    public List<TreasuryLookupItem> Banks { get; set; } = new();
    public List<TreasuryLookupItem> CashAccounts { get; set; } = new();
    public List<TreasuryLookupItem> BankAccounts { get; set; } = new();
    public List<TreasuryLookupItem> Customers { get; set; } = new();
    public List<TreasuryLookupItem> Suppliers { get; set; } = new();
    public List<TreasuryLookupItem> SalesInvoices { get; set; } = new();
    public List<TreasuryLookupItem> PurchaseInvoices { get; set; } = new();
}

public sealed record TreasuryLookupItem(Guid Id, string Name);

public sealed class TreasuryDashboardSummary
{
    public int CashAccounts { get; set; }
    public int BankAccounts { get; set; }
    public decimal CashBalance { get; set; }
    public decimal BankBalance { get; set; }
    public decimal PeriodInflow { get; set; }
    public decimal PeriodOutflow { get; set; }
    public int PendingReconciliations { get; set; }
}

public sealed class TreasuryBalanceRow
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class TreasuryRecentRow
{
    public Guid Id { get; set; }
    public string Source { get; set; } = string.Empty;
    public DateTime MovementDate { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public decimal AmountIn { get; set; }
    public decimal AmountOut { get; set; }
    public decimal BalanceAfter { get; set; }
}
