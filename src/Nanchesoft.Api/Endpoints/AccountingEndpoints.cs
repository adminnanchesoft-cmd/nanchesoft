using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class AccountingEndpoints
{
    public static IEndpointRouteBuilder MapAccountingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/accounting");

        group.MapGet("/lookups", async (NanchesoftDbContext dbContext) =>
        {
            var context = await ResolveContextAsync(dbContext);
            if (context is null)
            {
                return Results.Ok(new AccountingLookupsDto(Guid.Empty, Guid.Empty, Guid.Empty, DateTime.UtcNow.Year, DateTime.UtcNow.Month, new List<AccountingLookupItemDto>(), new List<AccountingLookupItemDto>()));
            }

            var accounts = await dbContext.Set<AccountingAccount>()
                .Where(x => x.CompanyId == context.CompanyId)
                .OrderBy(x => x.Code)
                .Select(x => new AccountingLookupItemDto(
                    x.Id,
                    x.Code,
                    x.Name,
                    x.IsActive ? null : "Inactiva"))
                .ToListAsync();

            var periods = await dbContext.Set<AccountingFiscalPeriod>()
                .Where(x => x.CompanyId == context.CompanyId)
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .Select(x => new AccountingLookupItemDto(
                    x.Id,
                    $"{x.Year:D4}-{x.Month:D2}",
                    $"{GetMonthName(x.Month)} {x.Year}",
                    x.Status))
                .ToListAsync();

            return Results.Ok(new AccountingLookupsDto(
                context.TenantId,
                context.CompanyId,
                context.BranchId,
                DateTime.UtcNow.Year,
                DateTime.UtcNow.Month,
                accounts,
                periods));
        });

        group.MapGet("/dashboard", async (NanchesoftDbContext dbContext) =>
        {
            var context = await ResolveContextAsync(dbContext);
            if (context is null)
            {
                return Results.Ok(new AccountingDashboardDto(0, 0, 0, 0, 0, 0, 0m, 0m, 0m));
            }

            var accountsQuery = dbContext.Set<AccountingAccount>().Where(x => x.CompanyId == context.CompanyId);
            var entriesQuery = dbContext.Set<AccountingJournalEntry>().Where(x => x.CompanyId == context.CompanyId && x.IsActive);
            var periodsQuery = dbContext.Set<AccountingFiscalPeriod>().Where(x => x.CompanyId == context.CompanyId && x.IsActive);

            var activeAccounts = await accountsQuery.CountAsync(x => x.IsActive);
            var journalEntries = await entriesQuery.CountAsync();
            var draftEntries = await entriesQuery.CountAsync(x => x.Status == "draft");
            var approvedEntries = await entriesQuery.CountAsync(x => x.Status == "approved");
            var postedEntries = await entriesQuery.CountAsync(x => x.Status == "posted");
            var openPeriods = await periodsQuery.CountAsync(x => x.Status == "open");

            var reportRange = BuildReportRange(DateTime.UtcNow.Year, DateTime.UtcNow.Month);
            var postedLines = await GetPostedLineRowsAsync(dbContext, context.CompanyId, reportRange.StartUtc, reportRange.EndUtc);

            var accountMap = await accountsQuery.ToDictionaryAsync(x => x.Id);
            decimal assets = 0m;
            decimal liabilities = 0m;
            decimal equity = 0m;
            decimal income = 0m;
            decimal expenses = 0m;

            foreach (var groupLines in postedLines.GroupBy(x => x.AccountId))
            {
                if (!accountMap.TryGetValue(groupLines.Key, out var account))
                {
                    continue;
                }

                var debit = groupLines.Sum(x => x.Debit);
                var credit = groupLines.Sum(x => x.Credit);
                var amount = CalculateStatementAmount(account, debit, credit);

                switch (account.AccountType)
                {
                    case "Asset":
                        assets += amount;
                        break;
                    case "Liability":
                        liabilities += amount;
                        break;
                    case "Equity":
                        equity += amount;
                        break;
                    case "Income":
                        income += amount;
                        break;
                    case "Expense":
                        expenses += amount;
                        break;
                }
            }

            return Results.Ok(new AccountingDashboardDto(
                activeAccounts,
                journalEntries,
                draftEntries,
                approvedEntries,
                postedEntries,
                openPeriods,
                assets,
                liabilities + equity,
                income - expenses));
        });

        group.MapGet("/chart-of-accounts", async (NanchesoftDbContext dbContext) =>
        {
            var context = await ResolveContextAsync(dbContext);
            if (context is null)
            {
                return Results.Ok(new List<AccountingAccountRowDto>());
            }

            var accounts = await dbContext.Set<AccountingAccount>()
                .Where(x => x.CompanyId == context.CompanyId)
                .OrderBy(x => x.Code)
                .ToListAsync();

            var accountMap = accounts.ToDictionary(x => x.Id);
            var rows = accounts.Select(x =>
            {
                accountMap.TryGetValue(x.ParentAccountId ?? Guid.Empty, out var parent);
                return new AccountingAccountRowDto(
                    x.Id,
                    x.CompanyId,
                    x.Code,
                    x.Name,
                    x.AccountType,
                    x.Nature,
                    x.ParentAccountId,
                    parent?.Code,
                    parent?.Name,
                    x.AllowsPosting,
                    x.IsActive);
            }).ToList();

            return Results.Ok(rows);
        });

        group.MapPost("/chart-of-accounts", async (NanchesoftDbContext dbContext, SaveAccountingAccountRequest request) =>
        {
            if (request.CompanyId == Guid.Empty || request.TenantId == Guid.Empty)
            {
                return Results.BadRequest("No hay contexto contable válido para crear la cuenta.");
            }

            var code = (request.Code ?? string.Empty).Trim();
            var name = (request.Name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            {
                return Results.BadRequest("Código y nombre son obligatorios.");
            }

            var duplicate = await dbContext.Set<AccountingAccount>()
                .AnyAsync(x => x.CompanyId == request.CompanyId && x.Code == code);
            if (duplicate)
            {
                return Results.BadRequest("Ya existe una cuenta con ese código.");
            }

            if (request.ParentAccountId.HasValue)
            {
                var parentExists = await dbContext.Set<AccountingAccount>()
                    .AnyAsync(x => x.Id == request.ParentAccountId.Value && x.CompanyId == request.CompanyId);
                if (!parentExists)
                {
                    return Results.BadRequest("La cuenta padre no existe.");
                }
            }

            var entity = new AccountingAccount
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                CompanyId = request.CompanyId,
                Code = code,
                Name = name,
                AccountType = NormalizeAccountType(request.AccountType),
                Nature = NormalizeNature(request.Nature),
                ParentAccountId = request.ParentAccountId,
                AllowsPosting = request.AllowsPosting,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "api"
            };

            dbContext.Set<AccountingAccount>().Add(entity);
            await dbContext.SaveChangesAsync();
            return Results.Ok(entity.Id);
        });

        group.MapPut("/chart-of-accounts/{accountId:guid}", async (NanchesoftDbContext dbContext, Guid accountId, SaveAccountingAccountRequest request) =>
        {
            var entity = await dbContext.Set<AccountingAccount>().FirstOrDefaultAsync(x => x.Id == accountId);
            if (entity is null)
            {
                return Results.NotFound();
            }

            var code = (request.Code ?? string.Empty).Trim();
            var name = (request.Name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            {
                return Results.BadRequest("Código y nombre son obligatorios.");
            }

            var duplicate = await dbContext.Set<AccountingAccount>()
                .AnyAsync(x => x.CompanyId == entity.CompanyId && x.Code == code && x.Id != entity.Id);
            if (duplicate)
            {
                return Results.BadRequest("Ya existe una cuenta con ese código.");
            }

            if (request.ParentAccountId == entity.Id)
            {
                return Results.BadRequest("Una cuenta no puede ser padre de sí misma.");
            }

            if (request.ParentAccountId.HasValue)
            {
                var parentExists = await dbContext.Set<AccountingAccount>()
                    .AnyAsync(x => x.Id == request.ParentAccountId.Value && x.CompanyId == entity.CompanyId);
                if (!parentExists)
                {
                    return Results.BadRequest("La cuenta padre no existe.");
                }
            }

            entity.Code = code;
            entity.Name = name;
            entity.AccountType = NormalizeAccountType(request.AccountType);
            entity.Nature = NormalizeNature(request.Nature);
            entity.ParentAccountId = request.ParentAccountId;
            entity.AllowsPosting = request.AllowsPosting;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = "api";

            await dbContext.SaveChangesAsync();
            return Results.Ok();
        });

        group.MapPost("/chart-of-accounts/{accountId:guid}/activate", async (NanchesoftDbContext dbContext, Guid accountId) =>
        {
            var entity = await dbContext.Set<AccountingAccount>().FirstOrDefaultAsync(x => x.Id == accountId);
            if (entity is null)
            {
                return Results.NotFound();
            }

            entity.IsActive = true;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = "api";
            await dbContext.SaveChangesAsync();
            return Results.Ok();
        });

        group.MapPost("/chart-of-accounts/{accountId:guid}/deactivate", async (NanchesoftDbContext dbContext, Guid accountId) =>
        {
            var entity = await dbContext.Set<AccountingAccount>().FirstOrDefaultAsync(x => x.Id == accountId);
            if (entity is null)
            {
                return Results.NotFound();
            }

            entity.IsActive = false;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = "api";
            await dbContext.SaveChangesAsync();
            return Results.Ok();
        });

        group.MapGet("/fiscal-periods", async (NanchesoftDbContext dbContext, int? year) =>
        {
            var context = await ResolveContextAsync(dbContext);
            if (context is null)
            {
                return Results.Ok(new List<FiscalPeriodRowDto>());
            }

            var targetYear = year.GetValueOrDefault(DateTime.UtcNow.Year);
            var rows = await dbContext.Set<AccountingFiscalPeriod>()
                .Where(x => x.CompanyId == context.CompanyId && x.Year == targetYear)
                .OrderBy(x => x.Month)
                .Select(x => new FiscalPeriodRowDto(
                    x.Id,
                    x.Year,
                    x.Month,
                    x.StartDate,
                    x.EndDate,
                    x.Status,
                    x.IsActive))
                .ToListAsync();

            return Results.Ok(rows);
        });

        group.MapPost("/fiscal-periods/{periodId:guid}/close", async (NanchesoftDbContext dbContext, Guid periodId) =>
        {
            var period = await dbContext.Set<AccountingFiscalPeriod>().FirstOrDefaultAsync(x => x.Id == periodId);
            if (period is null)
            {
                return Results.NotFound();
            }

            period.Status = "closed";
            await dbContext.SaveChangesAsync();
            return Results.Ok();
        });

        group.MapPost("/fiscal-periods/{periodId:guid}/reopen", async (NanchesoftDbContext dbContext, Guid periodId) =>
        {
            var period = await dbContext.Set<AccountingFiscalPeriod>().FirstOrDefaultAsync(x => x.Id == periodId);
            if (period is null)
            {
                return Results.NotFound();
            }

            period.Status = "open";
            await dbContext.SaveChangesAsync();
            return Results.Ok();
        });

        group.MapGet("/journal-entries", async (NanchesoftDbContext dbContext) =>
        {
            var context = await ResolveContextAsync(dbContext);
            if (context is null)
            {
                return Results.Ok(new List<AccountingJournalEntryRowDto>());
            }

            var rows = await dbContext.Set<AccountingJournalEntry>()
                .Where(x => x.CompanyId == context.CompanyId && x.IsActive)
                .OrderByDescending(x => x.EntryDate)
                .ThenByDescending(x => x.Folio)
                .Select(x => new AccountingJournalEntryRowDto(
                    x.Id,
                    x.CompanyId,
                    x.BranchId,
                    x.Folio,
                    x.EntryDate,
                    x.EntryType,
                    x.Status,
                    x.Reference,
                    x.Concept,
                    x.TotalDebit,
                    x.TotalCredit,
                    x.TotalDebit == x.TotalCredit))
                .ToListAsync();

            return Results.Ok(rows);
        });

        group.MapGet("/journal-entries/{entryId:guid}", async (NanchesoftDbContext dbContext, Guid entryId) =>
        {
            var header = await dbContext.Set<AccountingJournalEntry>().FirstOrDefaultAsync(x => x.Id == entryId && x.IsActive);
            if (header is null)
            {
                return Results.NotFound();
            }

            var accounts = await dbContext.Set<AccountingAccount>()
                .Where(x => x.CompanyId == header.CompanyId)
                .ToDictionaryAsync(x => x.Id);

            var lines = await dbContext.Set<AccountingJournalEntryLine>()
                .Where(x => x.JournalEntryId == header.Id)
                .OrderBy(x => x.LineNumber)
                .ToListAsync();

            var detail = new AccountingJournalEntryDetailDto(
                header.Id,
                header.TenantId,
                header.CompanyId,
                header.BranchId,
                header.Folio,
                header.EntryDate,
                header.EntryType,
                header.Status,
                header.Reference,
                header.Concept,
                header.TotalDebit,
                header.TotalCredit,
                lines.Select(line =>
                {
                    accounts.TryGetValue(line.AccountId, out var account);
                    return new AccountingJournalEntryLineDto(
                        line.Id,
                        line.LineNumber,
                        line.AccountId,
                        account?.Code ?? string.Empty,
                        account?.Name ?? string.Empty,
                        line.Description,
                        line.Debit,
                        line.Credit,
                        line.CostCenterId);
                }).ToList());

            return Results.Ok(detail);
        });

        group.MapPost("/journal-entries", async (NanchesoftDbContext dbContext, SaveJournalEntryRequest request) =>
        {
            var validationError = await ValidateJournalEntryRequestAsync(dbContext, request, null);
            if (validationError is not null)
            {
                return Results.BadRequest(validationError);
            }

            var entryDateUtc = EnsureUtc(request.EntryDate);
            var periodError = await EnsureOpenPeriodAsync(dbContext, request.CompanyId, entryDateUtc);
            if (periodError is not null)
            {
                return Results.BadRequest(periodError);
            }

            var header = new AccountingJournalEntry
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                CompanyId = request.CompanyId,
                BranchId = request.BranchId,
                Folio = await ResolveEntryFolioAsync(dbContext, request.CompanyId, request.Folio),
                EntryDate = entryDateUtc,
                EntryType = NormalizeEntryType(request.EntryType),
                Status = "draft",
                Reference = (request.Reference ?? string.Empty).Trim(),
                Concept = (request.Concept ?? string.Empty).Trim(),
                TotalDebit = request.Lines.Sum(x => x.Debit),
                TotalCredit = request.Lines.Sum(x => x.Credit),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "api"
            };

            dbContext.Set<AccountingJournalEntry>().Add(header);
            AddJournalLines(dbContext, header.Id, request.Lines);
            await dbContext.SaveChangesAsync();
            return Results.Ok(header.Id);
        });

        group.MapPut("/journal-entries/{entryId:guid}", async (NanchesoftDbContext dbContext, Guid entryId, SaveJournalEntryRequest request) =>
        {
            var header = await dbContext.Set<AccountingJournalEntry>().FirstOrDefaultAsync(x => x.Id == entryId && x.IsActive);
            if (header is null)
            {
                return Results.NotFound();
            }

            if (header.Status != "draft")
            {
                return Results.BadRequest("Solo se pueden editar pólizas en borrador.");
            }

            var validationError = await ValidateJournalEntryRequestAsync(dbContext, request, header.Id);
            if (validationError is not null)
            {
                return Results.BadRequest(validationError);
            }

            var entryDateUtc = EnsureUtc(request.EntryDate);
            var periodError = await EnsureOpenPeriodAsync(dbContext, header.CompanyId, entryDateUtc);
            if (periodError is not null)
            {
                return Results.BadRequest(periodError);
            }

            header.Folio = await ResolveEntryFolioAsync(dbContext, header.CompanyId, request.Folio, header.Id);
            header.EntryDate = entryDateUtc;
            header.EntryType = NormalizeEntryType(request.EntryType);
            header.Reference = (request.Reference ?? string.Empty).Trim();
            header.Concept = (request.Concept ?? string.Empty).Trim();
            header.TotalDebit = request.Lines.Sum(x => x.Debit);
            header.TotalCredit = request.Lines.Sum(x => x.Credit);
            header.UpdatedAt = DateTime.UtcNow;
            header.UpdatedBy = "api";

            var existingLines = await dbContext.Set<AccountingJournalEntryLine>()
                .Where(x => x.JournalEntryId == header.Id)
                .ToListAsync();
            dbContext.Set<AccountingJournalEntryLine>().RemoveRange(existingLines);
            AddJournalLines(dbContext, header.Id, request.Lines);

            await dbContext.SaveChangesAsync();
            return Results.Ok();
        });

        group.MapPost("/journal-entries/{entryId:guid}/approve", async (NanchesoftDbContext dbContext, Guid entryId) =>
        {
            var header = await dbContext.Set<AccountingJournalEntry>().FirstOrDefaultAsync(x => x.Id == entryId && x.IsActive);
            if (header is null)
            {
                return Results.NotFound();
            }

            if (header.Status != "draft")
            {
                return Results.BadRequest("Solo se pueden aprobar pólizas en borrador.");
            }

            var periodError = await EnsureOpenPeriodAsync(dbContext, header.CompanyId, header.EntryDate);
            if (periodError is not null)
            {
                return Results.BadRequest(periodError);
            }

            if (header.TotalDebit <= 0 || header.TotalCredit <= 0 || header.TotalDebit != header.TotalCredit)
            {
                return Results.BadRequest("La póliza no está cuadrada.");
            }

            header.Status = "approved";
            header.UpdatedAt = DateTime.UtcNow;
            header.UpdatedBy = "api";
            await dbContext.SaveChangesAsync();
            return Results.Ok();
        });

        group.MapPost("/journal-entries/{entryId:guid}/post", async (NanchesoftDbContext dbContext, Guid entryId) =>
        {
            var header = await dbContext.Set<AccountingJournalEntry>().FirstOrDefaultAsync(x => x.Id == entryId && x.IsActive);
            if (header is null)
            {
                return Results.NotFound();
            }

            if (header.Status != "approved")
            {
                return Results.BadRequest("Solo se pueden postear pólizas aprobadas.");
            }

            var periodError = await EnsureOpenPeriodAsync(dbContext, header.CompanyId, header.EntryDate);
            if (periodError is not null)
            {
                return Results.BadRequest(periodError);
            }

            header.Status = "posted";
            header.UpdatedAt = DateTime.UtcNow;
            header.UpdatedBy = "api";
            await dbContext.SaveChangesAsync();
            return Results.Ok();
        });

        group.MapPost("/journal-entries/{entryId:guid}/cancel", async (NanchesoftDbContext dbContext, Guid entryId) =>
        {
            var header = await dbContext.Set<AccountingJournalEntry>().FirstOrDefaultAsync(x => x.Id == entryId && x.IsActive);
            if (header is null)
            {
                return Results.NotFound();
            }

            if (header.Status == "posted")
            {
                return Results.BadRequest("No se puede cancelar una póliza posteada desde esta etapa base.");
            }

            header.Status = "cancelled";
            header.UpdatedAt = DateTime.UtcNow;
            header.UpdatedBy = "api";
            await dbContext.SaveChangesAsync();
            return Results.Ok();
        });

        group.MapGet("/trial-balance", async (NanchesoftDbContext dbContext, int? year, int? month) =>
        {
            var context = await ResolveContextAsync(dbContext);
            if (context is null)
            {
                return Results.Ok(new List<TrialBalanceRowDto>());
            }

            var range = BuildReportRange(year, month);
            var accounts = await dbContext.Set<AccountingAccount>()
                .Where(x => x.CompanyId == context.CompanyId)
                .OrderBy(x => x.Code)
                .ToListAsync();

            var postedLines = await GetPostedLineRowsAsync(dbContext, context.CompanyId, range.StartUtc, range.EndUtc);
            var grouped = postedLines.GroupBy(x => x.AccountId).ToDictionary(x => x.Key, x => x.ToList());

            var rows = accounts.Select(account =>
            {
                grouped.TryGetValue(account.Id, out var lines);
                var debit = lines?.Sum(x => x.Debit) ?? 0m;
                var credit = lines?.Sum(x => x.Credit) ?? 0m;
                var balance = NormalizeBalance(account, debit, credit);
                return new TrialBalanceRowDto(account.Id, account.Code, account.Name, debit, credit, balance);
            }).ToList();

            return Results.Ok(rows);
        });

        group.MapGet("/ledger", async (NanchesoftDbContext dbContext, Guid accountId, int? year, int? month) =>
        {
            var range = BuildReportRange(year, month);
            var query =
                from line in dbContext.Set<AccountingJournalEntryLine>()
                join header in dbContext.Set<AccountingJournalEntry>() on line.JournalEntryId equals header.Id
                where line.AccountId == accountId
                      && header.Status == "posted"
                      && header.IsActive
                      && header.EntryDate >= range.StartUtc
                      && header.EntryDate <= range.EndUtc
                orderby header.EntryDate, header.Folio, line.LineNumber
                select new
                {
                    header.Id,
                    header.Folio,
                    header.EntryDate,
                    header.Reference,
                    header.Concept,
                    line.Description,
                    line.Debit,
                    line.Credit
                };

            var data = await query.ToListAsync();
            decimal running = 0m;
            var rows = data.Select(item =>
            {
                running += item.Debit - item.Credit;
                return new LedgerRowDto(
                    item.Id,
                    item.Folio,
                    item.EntryDate,
                    item.Reference,
                    item.Concept,
                    item.Description,
                    item.Debit,
                    item.Credit,
                    running);
            }).ToList();

            return Results.Ok(rows);
        });

        group.MapGet("/balance-sheet", async (NanchesoftDbContext dbContext, int? year, int? month) =>
        {
            var context = await ResolveContextAsync(dbContext);
            if (context is null)
            {
                return Results.Ok(new BalanceSheetDto(new List<FinancialStatementRowDto>(), new List<FinancialStatementRowDto>(), new List<FinancialStatementRowDto>(), 0m, 0m, 0m));
            }

            var range = BuildReportRange(year, month);
            var accounts = await dbContext.Set<AccountingAccount>()
                .Where(x => x.CompanyId == context.CompanyId && x.IsActive)
                .OrderBy(x => x.Code)
                .ToListAsync();
            var postedLines = await GetPostedLineRowsAsync(dbContext, context.CompanyId, range.StartUtc, range.EndUtc);
            var grouped = postedLines.GroupBy(x => x.AccountId).ToDictionary(x => x.Key, x => x.ToList());

            var assets = new List<FinancialStatementRowDto>();
            var liabilities = new List<FinancialStatementRowDto>();
            var equity = new List<FinancialStatementRowDto>();

            foreach (var account in accounts)
            {
                grouped.TryGetValue(account.Id, out var lines);
                var debit = lines?.Sum(x => x.Debit) ?? 0m;
                var credit = lines?.Sum(x => x.Credit) ?? 0m;
                var amount = CalculateStatementAmount(account, debit, credit);
                if (amount == 0m)
                {
                    continue;
                }

                var row = new FinancialStatementRowDto(account.Id, account.Code, account.Name, amount);
                switch (account.AccountType)
                {
                    case "Asset":
                        assets.Add(row);
                        break;
                    case "Liability":
                        liabilities.Add(row);
                        break;
                    case "Equity":
                        equity.Add(row);
                        break;
                }
            }

            var result = new BalanceSheetDto(
                assets,
                liabilities,
                equity,
                assets.Sum(x => x.Amount),
                liabilities.Sum(x => x.Amount),
                equity.Sum(x => x.Amount));

            return Results.Ok(result);
        });

        group.MapGet("/income-statement", async (NanchesoftDbContext dbContext, int? year, int? month) =>
        {
            var context = await ResolveContextAsync(dbContext);
            if (context is null)
            {
                return Results.Ok(new IncomeStatementDto(new List<FinancialStatementRowDto>(), new List<FinancialStatementRowDto>(), 0m, 0m, 0m));
            }

            var range = BuildReportRange(year, month);
            var accounts = await dbContext.Set<AccountingAccount>()
                .Where(x => x.CompanyId == context.CompanyId && x.IsActive)
                .OrderBy(x => x.Code)
                .ToListAsync();
            var postedLines = await GetPostedLineRowsAsync(dbContext, context.CompanyId, range.StartUtc, range.EndUtc);
            var grouped = postedLines.GroupBy(x => x.AccountId).ToDictionary(x => x.Key, x => x.ToList());

            var incomeRows = new List<FinancialStatementRowDto>();
            var expenseRows = new List<FinancialStatementRowDto>();

            foreach (var account in accounts)
            {
                grouped.TryGetValue(account.Id, out var lines);
                var debit = lines?.Sum(x => x.Debit) ?? 0m;
                var credit = lines?.Sum(x => x.Credit) ?? 0m;
                var amount = CalculateStatementAmount(account, debit, credit);
                if (amount == 0m)
                {
                    continue;
                }

                var row = new FinancialStatementRowDto(account.Id, account.Code, account.Name, amount);
                switch (account.AccountType)
                {
                    case "Income":
                        incomeRows.Add(row);
                        break;
                    case "Expense":
                        expenseRows.Add(row);
                        break;
                }
            }

            var totalIncome = incomeRows.Sum(x => x.Amount);
            var totalExpense = expenseRows.Sum(x => x.Amount);
            var result = new IncomeStatementDto(incomeRows, expenseRows, totalIncome, totalExpense, totalIncome - totalExpense);
            return Results.Ok(result);
        });

        group.MapGet("/auto-policies/summary", async (NanchesoftDbContext dbContext) =>
        {
            var context = await ResolveContextAsync(dbContext);
            if (context is null)
            {
                return Results.Ok(new AccountingAutoPolicySummaryDto());
            }

            var summary = await BuildAutoPolicySummaryAsync(dbContext, context.CompanyId);
            return Results.Ok(summary);
        });

        group.MapGet("/auto-policies/sources", async (NanchesoftDbContext dbContext, string? sourceType, bool includeGenerated) =>
        {
            var context = await ResolveContextAsync(dbContext);
            if (context is null)
            {
                return Results.Ok(new List<AccountingAutoPolicySourceDto>());
            }

            var rows = await BuildAutoPolicySourcesAsync(dbContext, context.CompanyId, NormalizeAutoPolicySourceType(sourceType), includeGenerated);
            return Results.Ok(rows);
        });

        group.MapGet("/auto-policies/preview", async (NanchesoftDbContext dbContext, string sourceType, Guid sourceId) =>
        {
            var preview = await BuildAutoPolicyPreviewAsync(dbContext, NormalizeAutoPolicySourceType(sourceType), sourceId);
            if (preview is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(preview);
        });

        group.MapPost("/auto-policies/generate", async (NanchesoftDbContext dbContext, AccountingAutoPolicyGenerateRequest request) =>
        {
            var result = await GenerateAutoPolicyAsync(dbContext, NormalizeAutoPolicySourceType(request.SourceType), request.SourceId);
            return Results.Ok(result);
        });

        group.MapPost("/auto-policies/generate-pending", async (NanchesoftDbContext dbContext, string? sourceType) =>
        {
            var context = await ResolveContextAsync(dbContext);
            if (context is null)
            {
                return Results.Ok(new AccountingAutoPolicyGenerateResultDto(false, 0, 0, 0, "No hay contexto contable para generar pólizas automáticas.", new List<string>()));
            }

            var rows = await BuildAutoPolicySourcesAsync(dbContext, context.CompanyId, NormalizeAutoPolicySourceType(sourceType), false);
            var created = 0;
            var skipped = 0;
            var failed = 0;
            var messages = new List<string>();

            foreach (var row in rows)
            {
                var result = await GenerateAutoPolicyAsync(dbContext, row.SourceType, row.SourceId);
                if (result.Success)
                {
                    created += result.CreatedCount;
                    skipped += result.SkippedCount;
                    if (!string.IsNullOrWhiteSpace(result.Message))
                    {
                        messages.Add(result.Message);
                    }
                }
                else
                {
                    failed += 1;
                    messages.Add($"{GetSourceTypeDisplayName(row.SourceType)} {row.Folio}: {result.Message}");
                }
            }

            return Results.Ok(new AccountingAutoPolicyGenerateResultDto(
                failed == 0,
                created,
                skipped,
                failed,
                $"Proceso terminado. Generadas: {created}, omitidas: {skipped}, fallidas: {failed}.",
                messages));
        });



        group.MapGet("/monthly-close/preview", async (NanchesoftDbContext dbContext, int year, int month) =>
        {
            var context = await ResolveContextAsync(dbContext);
            if (context is null)
            {
                return Results.Ok(new AccountingMonthlyClosePreviewDto());
            }

            var preview = await BuildMonthlyClosePreviewAsync(dbContext, context, year, month);
            return Results.Ok(preview);
        });

        group.MapPost("/monthly-close/generate", async (NanchesoftDbContext dbContext, AccountingMonthlyCloseGenerateRequest request) =>
        {
            var context = await ResolveContextAsync(dbContext);
            if (context is null)
            {
                return Results.Ok(new AccountingMonthlyCloseGenerateResultDto(false, Guid.Empty, null, "No hay contexto contable disponible."));
            }

            var result = await GenerateMonthlyCloseAsync(dbContext, context, request.Year, request.Month);
            return Results.Ok(result);
        });

        return app;
    }

    private static async Task<AccountingMonthlyClosePreviewDto> BuildMonthlyClosePreviewAsync(NanchesoftDbContext dbContext, ContextInfo context, int year, int month)
    {
        month = Math.Clamp(month, 1, 12);
        var range = BuildMonthlyPeriodRange(year, month);
        var closingDate = range.EndUtc;
        var period = await dbContext.Set<AccountingFiscalPeriod>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CompanyId == context.CompanyId && x.Year == year && x.Month == month && x.IsActive);

        var accounts = await dbContext.Set<AccountingAccount>()
            .Where(x => x.CompanyId == context.CompanyId && x.IsActive && x.AllowsPosting)
            .OrderBy(x => x.Code)
            .ToListAsync();

        var lines = await GetPostedLineRowsAsync(dbContext, context.CompanyId, range.StartUtc, range.EndUtc);
        var grouped = lines.GroupBy(x => x.AccountId)
            .ToDictionary(g => g.Key, g => new { Debit = g.Sum(x => x.Debit), Credit = g.Sum(x => x.Credit) });

        var monthlyLines = new List<AccountingMonthlyCloseLineDto>();
        decimal totalIncome = 0m;
        decimal totalExpense = 0m;

        foreach (var account in accounts.Where(x => x.AccountType is "Income" or "Expense"))
        {
            if (!grouped.TryGetValue(account.Id, out var totals))
            {
                continue;
            }

            var balance = CalculateStatementAmount(account, totals.Debit, totals.Credit);
            if (balance == 0m)
            {
                continue;
            }

            if (account.AccountType == "Income")
            {
                totalIncome += balance;
            }
            else
            {
                totalExpense += balance;
            }

            monthlyLines.Add(new AccountingMonthlyCloseLineDto(
                account.Id,
                account.Code,
                account.Name,
                account.AccountType,
                balance,
                account.AccountType == "Income" ? balance : 0m,
                account.AccountType == "Expense" ? balance : 0m));
        }

        var existing = await FindExistingClosingEntryAsync(dbContext, context.CompanyId, year, month);
        var closingAccount = ResolveClosingAccount(accounts);
        var netResult = totalIncome - totalExpense;
        var closeLines = BuildMonthlyCloseEntryLines(monthlyLines, closingAccount, netResult);

        var messages = new List<string>();
        if (period is null)
        {
            messages.Add("No existe el periodo fiscal seleccionado.");
        }
        else if (!string.Equals(period.Status, "open", StringComparison.OrdinalIgnoreCase))
        {
            messages.Add("El periodo fiscal seleccionado está cerrado.");
        }

        if (closingAccount is null)
        {
            messages.Add("No existe una cuenta de capital válida para enviar el resultado del periodo.");
        }

        if (existing is not null)
        {
            messages.Add($"Ya existe la póliza de cierre {existing.Folio} con estatus {existing.Status}.");
        }

        if (monthlyLines.Count == 0)
        {
            messages.Add("No hay movimientos de ingresos o gastos posteados en el periodo seleccionado.");
        }

        return new AccountingMonthlyClosePreviewDto
        {
            Year = year,
            Month = month,
            ClosingDate = closingDate,
            FiscalPeriodId = period?.Id,
            PeriodStatus = period?.Status ?? "missing",
            ClosingAccountId = closingAccount?.Id,
            ClosingAccountCode = closingAccount?.Code,
            ClosingAccountName = closingAccount?.Name,
            TotalIncome = totalIncome,
            TotalExpense = totalExpense,
            NetResult = netResult,
            ExistingJournalEntryId = existing?.JournalEntryId,
            ExistingJournalFolio = existing?.Folio,
            ExistingJournalStatus = existing?.Status,
            IsReady = closeLines.Count > 0 && messages.Count == 0,
            Messages = messages,
            SourceLines = monthlyLines,
            CloseLines = closeLines
        };
    }

    private static async Task<AccountingMonthlyCloseGenerateResultDto> GenerateMonthlyCloseAsync(NanchesoftDbContext dbContext, ContextInfo context, int year, int month)
    {
        var preview = await BuildMonthlyClosePreviewAsync(dbContext, context, year, month);
        if (!preview.IsReady)
        {
            return new AccountingMonthlyCloseGenerateResultDto(false, Guid.Empty, null, string.Join(" ", preview.Messages));
        }

        var reference = BuildMonthlyCloseReference(year, month);
        var header = new AccountingJournalEntry
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            CompanyId = context.CompanyId,
            BranchId = context.BranchId,
            Folio = await ResolveEntryFolioAsync(dbContext, context.CompanyId, $"CIE-{year}{month:00}"),
            EntryDate = preview.ClosingDate,
            EntryType = "closing",
            Status = "draft",
            Reference = reference,
            Concept = $"Póliza de cierre mensual {year}-{month:00}",
            TotalDebit = preview.CloseLines.Sum(x => x.Debit),
            TotalCredit = preview.CloseLines.Sum(x => x.Credit),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "api"
        };

        dbContext.Set<AccountingJournalEntry>().Add(header);
        AddJournalLines(dbContext, header.Id, preview.CloseLines.Select(line => new SaveJournalEntryLineRequest(
            line.AccountId,
            line.Description,
            line.Debit,
            line.Credit,
            null)));

        await dbContext.SaveChangesAsync();
        return new AccountingMonthlyCloseGenerateResultDto(true, header.Id, header.Folio, $"Se generó la póliza de cierre {header.Folio} en borrador.");
    }

    private static string BuildMonthlyCloseReference(int year, int month)
        => $"CLOSE|{year:0000}|{month:00}";

    private static async Task<ExistingAutomaticEntryInfo?> FindExistingClosingEntryAsync(NanchesoftDbContext dbContext, Guid companyId, int year, int month)
    {
        var reference = BuildMonthlyCloseReference(year, month);
        return await dbContext.Set<AccountingJournalEntry>()
            .Where(x => x.CompanyId == companyId && x.IsActive && x.Reference == reference)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new ExistingAutomaticEntryInfo(x.Id, x.Folio, x.Status))
            .FirstOrDefaultAsync();
    }

    private static AccountingAccount? ResolveClosingAccount(List<AccountingAccount> accounts)
    {
        return accounts.FirstOrDefault(x => x.Code == "3000" && x.AccountType == "Equity" && x.AllowsPosting)
               ?? accounts.FirstOrDefault(x => x.AccountType == "Equity" && x.AllowsPosting)
               ?? accounts.FirstOrDefault(x => x.AccountType == "Liability" && x.AllowsPosting);
    }

    private static List<AccountingMonthlyCloseEntryLineDto> BuildMonthlyCloseEntryLines(List<AccountingMonthlyCloseLineDto> monthlyLines, AccountingAccount? closingAccount, decimal netResult)
    {
        var rows = new List<AccountingMonthlyCloseEntryLineDto>();
        if (closingAccount is null || monthlyLines.Count == 0)
        {
            return rows;
        }

        foreach (var line in monthlyLines)
        {
            if (line.AccountType == "Income" && line.Balance > 0m)
            {
                rows.Add(new AccountingMonthlyCloseEntryLineDto(line.AccountId, line.AccountCode, line.AccountName, $"Cierre ingreso {line.AccountCode}", line.Balance, 0m));
            }
            else if (line.AccountType == "Expense" && line.Balance > 0m)
            {
                rows.Add(new AccountingMonthlyCloseEntryLineDto(line.AccountId, line.AccountCode, line.AccountName, $"Cierre gasto {line.AccountCode}", 0m, line.Balance));
            }
        }

        if (netResult > 0m)
        {
            rows.Add(new AccountingMonthlyCloseEntryLineDto(closingAccount.Id, closingAccount.Code, closingAccount.Name, "Resultado del periodo", 0m, netResult));
        }
        else if (netResult < 0m)
        {
            rows.Add(new AccountingMonthlyCloseEntryLineDto(closingAccount.Id, closingAccount.Code, closingAccount.Name, "Resultado del periodo", Math.Abs(netResult), 0m));
        }

        return rows;
    }

    private static AccountingReportRange BuildMonthlyPeriodRange(int year, int month)
    {
        month = Math.Clamp(month, 1, 12);
        var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(year, month, DateTime.DaysInMonth(year, month), 23, 59, 59, DateTimeKind.Utc);
        return new AccountingReportRange(start, end);
    }


    private static void AddJournalLines(NanchesoftDbContext dbContext, Guid journalEntryId, IEnumerable<SaveJournalEntryLineRequest> lines)
    {
        var lineNumber = 1;
        foreach (var line in lines)
        {
            dbContext.Set<AccountingJournalEntryLine>().Add(new AccountingJournalEntryLine
            {
                Id = Guid.NewGuid(),
                JournalEntryId = journalEntryId,
                LineNumber = lineNumber++,
                AccountId = line.AccountId,
                Description = (line.Description ?? string.Empty).Trim(),
                Debit = line.Debit,
                Credit = line.Credit,
                CostCenterId = line.CostCenterId
            });
        }
    }

    private static async Task<string?> ValidateJournalEntryRequestAsync(NanchesoftDbContext dbContext, SaveJournalEntryRequest request, Guid? currentEntryId)
    {
        if (request.TenantId == Guid.Empty || request.CompanyId == Guid.Empty || request.BranchId == Guid.Empty)
        {
            return "No hay contexto válido para guardar la póliza.";
        }

        if (request.Lines is null || request.Lines.Count == 0)
        {
            return "La póliza debe tener al menos una línea.";
        }

        var entryDate = EnsureUtc(request.EntryDate);
        if (entryDate == default)
        {
            return "La fecha de la póliza es obligatoria.";
        }

        var hasInvalidLine = request.Lines.Any(x => x.AccountId == Guid.Empty || (x.Debit <= 0m && x.Credit <= 0m) || (x.Debit > 0m && x.Credit > 0m));
        if (hasInvalidLine)
        {
            return "Cada línea debe tener cuenta y solo un lado con importe: cargo o abono.";
        }

        var totalDebit = request.Lines.Sum(x => x.Debit);
        var totalCredit = request.Lines.Sum(x => x.Credit);
        if (totalDebit <= 0m || totalCredit <= 0m || totalDebit != totalCredit)
        {
            return "La póliza debe estar cuadrada.";
        }

        var accountIds = request.Lines.Select(x => x.AccountId).Distinct().ToList();
        var accounts = await dbContext.Set<AccountingAccount>()
            .Where(x => x.CompanyId == request.CompanyId && accountIds.Contains(x.Id))
            .ToListAsync();

        if (accounts.Count != accountIds.Count)
        {
            return "Una o más cuentas no existen.";
        }

        if (accounts.Any(x => !x.IsActive))
        {
            return "No puedes usar cuentas inactivas en la póliza.";
        }

        if (accounts.Any(x => !x.AllowsPosting))
        {
            return "Todas las líneas deben usar cuentas de detalle que permitan posteo.";
        }

        var folio = (request.Folio ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(folio))
        {
            var duplicate = await dbContext.Set<AccountingJournalEntry>()
                .AnyAsync(x => x.CompanyId == request.CompanyId && x.Folio == folio && x.Id != currentEntryId);
            if (duplicate)
            {
                return "Ya existe una póliza con ese folio.";
            }
        }

        return null;
    }

    private static async Task<string?> EnsureOpenPeriodAsync(NanchesoftDbContext dbContext, Guid companyId, DateTime entryDateUtc)
    {
        var year = entryDateUtc.Year;
        var month = entryDateUtc.Month;
        var period = await dbContext.Set<AccountingFiscalPeriod>()
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Year == year && x.Month == month && x.IsActive);

        if (period is null)
        {
            return "No existe periodo fiscal para la fecha de la póliza.";
        }

        if (!string.Equals(period.Status, "open", StringComparison.OrdinalIgnoreCase))
        {
            return "El periodo fiscal está cerrado.";
        }

        return null;
    }

    private static async Task<string> ResolveEntryFolioAsync(NanchesoftDbContext dbContext, Guid companyId, string? requestedFolio, Guid? currentEntryId = null)
    {
        var folio = (requestedFolio ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(folio))
        {
            return folio;
        }

        var count = await dbContext.Set<AccountingJournalEntry>()
            .CountAsync(x => x.CompanyId == companyId && x.Id != currentEntryId);
        return $"POL-{count + 1:000000}";
    }

    private static async Task<List<PostedLineRow>> GetPostedLineRowsAsync(NanchesoftDbContext dbContext, Guid companyId, DateTime startUtc, DateTime endUtc)
    {
        var rows =
            await (
                from line in dbContext.Set<AccountingJournalEntryLine>()
                join header in dbContext.Set<AccountingJournalEntry>() on line.JournalEntryId equals header.Id
                where header.CompanyId == companyId
                      && header.IsActive
                      && header.Status == "posted"
                      && header.EntryDate >= startUtc
                      && header.EntryDate <= endUtc
                select new PostedLineRow(line.AccountId, line.Debit, line.Credit)
            ).ToListAsync();

        return rows;
    }

    private static AccountingReportRange BuildReportRange(int? year, int? month)
    {
        var targetYear = year.GetValueOrDefault(DateTime.UtcNow.Year);
        var targetMonth = month.GetValueOrDefault(DateTime.UtcNow.Month);
        targetMonth = Math.Clamp(targetMonth, 1, 12);

        var start = new DateTime(targetYear, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(targetYear, targetMonth, DateTime.DaysInMonth(targetYear, targetMonth), 23, 59, 59, DateTimeKind.Utc);
        return new AccountingReportRange(start, end);
    }

    private static decimal NormalizeBalance(AccountingAccount account, decimal debit, decimal credit)
    {
        return account.Nature switch
        {
            "Credit" => credit - debit,
            _ => debit - credit
        };
    }

    private static decimal CalculateStatementAmount(AccountingAccount account, decimal debit, decimal credit)
    {
        return account.AccountType switch
        {
            "Asset" => debit - credit,
            "Expense" => debit - credit,
            "Liability" => credit - debit,
            "Equity" => credit - debit,
            "Income" => credit - debit,
            _ => debit - credit
        };
    }

    private static string NormalizeAccountType(string? accountType)
    {
        return (accountType ?? string.Empty).Trim() switch
        {
            "Liability" => "Liability",
            "Equity" => "Equity",
            "Income" => "Income",
            "Expense" => "Expense",
            _ => "Asset"
        };
    }

    private static string NormalizeNature(string? nature)
    {
        return string.Equals((nature ?? string.Empty).Trim(), "Credit", StringComparison.OrdinalIgnoreCase)
            ? "Credit"
            : "Debit";
    }

    private static string NormalizeEntryType(string? entryType)
    {
        return string.IsNullOrWhiteSpace(entryType)
            ? "manual"
            : entryType.Trim().ToLowerInvariant();
    }

    private static DateTime EnsureUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }

    private static async Task<ContextInfo?> ResolveContextAsync(NanchesoftDbContext dbContext)
    {
        var company = await dbContext.Companies.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        if (company is null)
        {
            return null;
        }

        var branch = await dbContext.Branches
            .Where(x => x.CompanyId == company.Id)
            .OrderBy(x => x.CreatedAt)
            .FirstOrDefaultAsync();

        return new ContextInfo(company.TenantId, company.Id, branch?.Id ?? Guid.Empty);
    }


    private static async Task<AccountingAutoPolicySummaryDto> BuildAutoPolicySummaryAsync(NanchesoftDbContext dbContext, Guid companyId)
    {
        var referenceSet = await dbContext.Set<AccountingJournalEntry>()
            .Where(x => x.CompanyId == companyId && x.IsActive && x.EntryType == "automatic")
            .Select(x => x.Reference)
            .ToHashSetAsync();

        var salesInvoiceIds = await dbContext.Set<SalesInvoice>()
            .Where(x => x.CompanyId == companyId && x.IsActive && x.Status == "posted")
            .Select(x => x.Id)
            .ToListAsync();

        var creditNoteIds = await dbContext.Set<CreditNote>()
            .Where(x => x.CompanyId == companyId && x.IsActive && x.Status == "posted")
            .Select(x => x.Id)
            .ToListAsync();

        var purchaseInvoiceIds = await dbContext.Set<PurchaseInvoice>()
            .Where(x => x.CompanyId == companyId && x.IsActive && x.Status == "posted")
            .Select(x => x.Id)
            .ToListAsync();

        var receiptIds = await dbContext.Set<Receipt>()
            .Where(x => x.CompanyId == companyId && x.IsActive && x.Status == "posted")
            .Select(x => x.Id)
            .ToListAsync();

        var paymentIds = await dbContext.Set<Payment>()
            .Where(x => x.CompanyId == companyId && x.IsActive && x.Status == "posted")
            .Select(x => x.Id)
            .ToListAsync();

        return new AccountingAutoPolicySummaryDto
        {
            PendingSalesInvoices = salesInvoiceIds.Count(x => !referenceSet.Contains(BuildAutoPolicyReference("sales_invoice", x))),
            PendingCreditNotes = creditNoteIds.Count(x => !referenceSet.Contains(BuildAutoPolicyReference("credit_note", x))),
            PendingPurchaseInvoices = purchaseInvoiceIds.Count(x => !referenceSet.Contains(BuildAutoPolicyReference("purchase_invoice", x))),
            PendingReceipts = receiptIds.Count(x => !referenceSet.Contains(BuildAutoPolicyReference("receipt", x))),
            PendingPayments = paymentIds.Count(x => !referenceSet.Contains(BuildAutoPolicyReference("payment", x))),
            DraftAutomaticEntries = await dbContext.Set<AccountingJournalEntry>().CountAsync(x => x.CompanyId == companyId && x.IsActive && x.EntryType == "automatic" && x.Status == "draft"),
            ApprovedAutomaticEntries = await dbContext.Set<AccountingJournalEntry>().CountAsync(x => x.CompanyId == companyId && x.IsActive && x.EntryType == "automatic" && x.Status == "approved"),
            PostedAutomaticEntries = await dbContext.Set<AccountingJournalEntry>().CountAsync(x => x.CompanyId == companyId && x.IsActive && x.EntryType == "automatic" && x.Status == "posted")
        };
    }

    private static async Task<List<AccountingAutoPolicySourceDto>> BuildAutoPolicySourcesAsync(NanchesoftDbContext dbContext, Guid companyId, string sourceType, bool includeGenerated)
    {
        var normalizedType = NormalizeAutoPolicySourceType(sourceType);
        var rows = new List<AccountingAutoPolicySourceDto>();
        var existingEntries = await dbContext.Set<AccountingJournalEntry>()
            .Where(x => x.CompanyId == companyId && x.IsActive && x.EntryType == "automatic")
            .Select(x => new { x.Id, x.Folio, x.Status, x.Reference })
            .ToListAsync();
        var existingMap = existingEntries.ToDictionary(x => x.Reference, x => x);
        var defaults = await LoadDefaultAccountBundleAsync(dbContext, companyId);

        if (normalizedType is "all" or "sales_invoice")
        {
            var documents = await (
                from invoice in dbContext.Set<SalesInvoice>().AsNoTracking()
                join customer in dbContext.Set<Customer>().AsNoTracking() on invoice.CustomerId equals customer.Id into customerJoin
                from customer in customerJoin.DefaultIfEmpty()
                where invoice.CompanyId == companyId && invoice.IsActive && invoice.Status == "posted"
                orderby invoice.InvoiceDate descending, invoice.Folio descending
                select new
                {
                    invoice.Id,
                    invoice.InvoiceDate,
                    invoice.Folio,
                    invoice.Total,
                    ThirdPartyName = customer != null ? customer.Name : string.Empty,
                    invoice.CustomerId
                }).ToListAsync();

            foreach (var doc in documents)
            {
                var reference = BuildAutoPolicyReference("sales_invoice", doc.Id);
                existingMap.TryGetValue(reference, out var existing);
                var ready = defaults.Customers is not null && defaults.Sales is not null && doc.CustomerId.HasValue;
                if (!includeGenerated && existing is not null)
                {
                    continue;
                }

                rows.Add(new AccountingAutoPolicySourceDto(
                    "sales_invoice",
                    doc.Id,
                    doc.InvoiceDate,
                    doc.Folio,
                    doc.ThirdPartyName,
                    doc.Total,
                    defaults.Customers?.Code ?? string.Empty,
                    defaults.Customers?.Name ?? string.Empty,
                    defaults.Sales?.Code ?? string.Empty,
                    defaults.Sales?.Name ?? string.Empty,
                    $"Factura de venta {doc.Folio}",
                    existing?.Id,
                    existing?.Folio,
                    existing?.Status,
                    ready,
                    ready ? null : "Falta cuenta 1050 Clientes, 4000 Ventas o cliente en el documento."));
            }
        }

        if (normalizedType is "all" or "credit_note")
        {
            var documents = await (
                from note in dbContext.Set<CreditNote>().AsNoTracking()
                join customer in dbContext.Set<Customer>().AsNoTracking() on note.CustomerId equals customer.Id into customerJoin
                from customer in customerJoin.DefaultIfEmpty()
                where note.CompanyId == companyId && note.IsActive && note.Status == "posted"
                orderby note.CreditNoteDate descending, note.Folio descending
                select new
                {
                    note.Id,
                    note.CreditNoteDate,
                    note.Folio,
                    note.Total,
                    ThirdPartyName = customer != null ? customer.Name : string.Empty,
                    note.CustomerId
                }).ToListAsync();

            foreach (var doc in documents)
            {
                var reference = BuildAutoPolicyReference("credit_note", doc.Id);
                existingMap.TryGetValue(reference, out var existing);
                var ready = defaults.Customers is not null && defaults.Sales is not null && doc.CustomerId.HasValue;
                if (!includeGenerated && existing is not null)
                {
                    continue;
                }

                rows.Add(new AccountingAutoPolicySourceDto(
                    "credit_note",
                    doc.Id,
                    doc.CreditNoteDate,
                    doc.Folio,
                    doc.ThirdPartyName,
                    doc.Total,
                    defaults.Sales?.Code ?? string.Empty,
                    defaults.Sales?.Name ?? string.Empty,
                    defaults.Customers?.Code ?? string.Empty,
                    defaults.Customers?.Name ?? string.Empty,
                    $"Nota de crédito {doc.Folio}",
                    existing?.Id,
                    existing?.Folio,
                    existing?.Status,
                    ready,
                    ready ? null : "Falta cuenta 4000 Ventas, 1050 Clientes o cliente en el documento."));
            }
        }

        if (normalizedType is "all" or "purchase_invoice")
        {
            var documents = await (
                from invoice in dbContext.Set<PurchaseInvoice>().AsNoTracking()
                join supplier in dbContext.Set<Supplier>().AsNoTracking() on invoice.SupplierId equals supplier.Id into supplierJoin
                from supplier in supplierJoin.DefaultIfEmpty()
                where invoice.CompanyId == companyId && invoice.IsActive && invoice.Status == "posted"
                orderby invoice.InvoiceDate descending, invoice.Folio descending
                select new
                {
                    invoice.Id,
                    invoice.InvoiceDate,
                    invoice.Folio,
                    invoice.Total,
                    ThirdPartyName = supplier != null ? supplier.Name : string.Empty,
                    invoice.SupplierId
                }).ToListAsync();

            foreach (var doc in documents)
            {
                var reference = BuildAutoPolicyReference("purchase_invoice", doc.Id);
                existingMap.TryGetValue(reference, out var existing);
                var ready = defaults.Expenses is not null && defaults.Suppliers is not null && doc.SupplierId.HasValue;
                if (!includeGenerated && existing is not null)
                {
                    continue;
                }

                rows.Add(new AccountingAutoPolicySourceDto(
                    "purchase_invoice",
                    doc.Id,
                    doc.InvoiceDate,
                    doc.Folio,
                    doc.ThirdPartyName,
                    doc.Total,
                    defaults.Expenses?.Code ?? string.Empty,
                    defaults.Expenses?.Name ?? string.Empty,
                    defaults.Suppliers?.Code ?? string.Empty,
                    defaults.Suppliers?.Name ?? string.Empty,
                    $"Factura de proveedor {doc.Folio}",
                    existing?.Id,
                    existing?.Folio,
                    existing?.Status,
                    ready,
                    ready ? null : "Falta cuenta 6000 Gastos, 2000 Proveedores o proveedor en el documento."));
            }
        }

        if (normalizedType is "all" or "receipt")
        {
            var documents = await (
                from receipt in dbContext.Set<Receipt>().AsNoTracking()
                join customer in dbContext.Set<Customer>().AsNoTracking() on receipt.CustomerId equals customer.Id into customerJoin
                from customer in customerJoin.DefaultIfEmpty()
                where receipt.CompanyId == companyId && receipt.IsActive && receipt.Status == "posted"
                orderby receipt.ReceiptDate descending, receipt.Folio descending
                select new
                {
                    receipt.Id,
                    receipt.ReceiptDate,
                    receipt.Folio,
                    receipt.Total,
                    receipt.TargetType,
                    receipt.BankAccountId,
                    receipt.CashAccountId,
                    ThirdPartyName = customer != null ? customer.Name : string.Empty,
                    receipt.CustomerId
                }).ToListAsync();

            foreach (var doc in documents)
            {
                var reference = BuildAutoPolicyReference("receipt", doc.Id);
                existingMap.TryGetValue(reference, out var existing);
                var debitAccount = doc.BankAccountId.HasValue || string.Equals(doc.TargetType, "bank", StringComparison.OrdinalIgnoreCase)
                    ? defaults.Banks
                    : defaults.Cash;
                var ready = defaults.Customers is not null && debitAccount is not null && doc.CustomerId.HasValue;
                if (!includeGenerated && existing is not null)
                {
                    continue;
                }

                rows.Add(new AccountingAutoPolicySourceDto(
                    "receipt",
                    doc.Id,
                    doc.ReceiptDate,
                    doc.Folio,
                    doc.ThirdPartyName,
                    doc.Total,
                    debitAccount?.Code ?? string.Empty,
                    debitAccount?.Name ?? string.Empty,
                    defaults.Customers?.Code ?? string.Empty,
                    defaults.Customers?.Name ?? string.Empty,
                    $"Recibo {doc.Folio}",
                    existing?.Id,
                    existing?.Folio,
                    existing?.Status,
                    ready,
                    ready ? null : "Falta cuenta 1000 Caja o 1010 Bancos, 1050 Clientes o cliente en el documento."));
            }
        }

        if (normalizedType is "all" or "payment")
        {
            var documents = await (
                from payment in dbContext.Set<Payment>().AsNoTracking()
                join supplier in dbContext.Set<Supplier>().AsNoTracking() on payment.SupplierId equals supplier.Id into supplierJoin
                from supplier in supplierJoin.DefaultIfEmpty()
                where payment.CompanyId == companyId && payment.IsActive && payment.Status == "posted"
                orderby payment.PaymentDate descending, payment.Folio descending
                select new
                {
                    payment.Id,
                    payment.PaymentDate,
                    payment.Folio,
                    payment.Total,
                    payment.SourceType,
                    payment.BankAccountId,
                    payment.CashAccountId,
                    ThirdPartyName = supplier != null ? supplier.Name : string.Empty,
                    payment.SupplierId
                }).ToListAsync();

            foreach (var doc in documents)
            {
                var reference = BuildAutoPolicyReference("payment", doc.Id);
                existingMap.TryGetValue(reference, out var existing);
                var creditAccount = doc.BankAccountId.HasValue || string.Equals(doc.SourceType, "bank", StringComparison.OrdinalIgnoreCase)
                    ? defaults.Banks
                    : defaults.Cash;
                var ready = defaults.Suppliers is not null && creditAccount is not null && doc.SupplierId.HasValue;
                if (!includeGenerated && existing is not null)
                {
                    continue;
                }

                rows.Add(new AccountingAutoPolicySourceDto(
                    "payment",
                    doc.Id,
                    doc.PaymentDate,
                    doc.Folio,
                    doc.ThirdPartyName,
                    doc.Total,
                    defaults.Suppliers?.Code ?? string.Empty,
                    defaults.Suppliers?.Name ?? string.Empty,
                    creditAccount?.Code ?? string.Empty,
                    creditAccount?.Name ?? string.Empty,
                    $"Pago {doc.Folio}",
                    existing?.Id,
                    existing?.Folio,
                    existing?.Status,
                    ready,
                    ready ? null : "Falta cuenta 1000 Caja o 1010 Bancos, 2000 Proveedores o proveedor en el documento."));
            }
        }

        return rows
            .OrderByDescending(x => x.DocumentDate)
            .ThenByDescending(x => x.Folio)
            .ToList();
    }

    private static async Task<AccountingAutoPolicyPreviewDto?> BuildAutoPolicyPreviewAsync(NanchesoftDbContext dbContext, string sourceType, Guid sourceId)
    {
        sourceType = NormalizeAutoPolicySourceType(sourceType);
        switch (sourceType)
        {
            case "sales_invoice":
            {
                var doc = await dbContext.Set<SalesInvoice>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == sourceId && x.IsActive);
                if (doc is null)
                {
                    return null;
                }

                var defaults = await LoadDefaultAccountBundleAsync(dbContext, doc.CompanyId);
                var customerName = doc.CustomerId.HasValue
                    ? await dbContext.Set<Customer>().Where(x => x.Id == doc.CustomerId.Value).Select(x => x.Name).FirstOrDefaultAsync() ?? string.Empty
                    : string.Empty;
                var existing = await FindExistingAutomaticEntryAsync(dbContext, doc.CompanyId, sourceType, doc.Id);
                return BuildAutoPolicyPreview(
                    doc.TenantId,
                    doc.CompanyId,
                    doc.BranchId,
                    sourceType,
                    doc.Id,
                    doc.InvoiceDate,
                    doc.Folio,
                    customerName,
                    doc.Total,
                    $"Factura de venta {doc.Folio}",
                    defaults.Customers,
                    defaults.Sales,
                    doc.CustomerId.HasValue,
                    existing,
                    "Falta cuenta 1050 Clientes, 4000 Ventas o cliente en el documento.");
            }
            case "credit_note":
            {
                var doc = await dbContext.Set<CreditNote>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == sourceId && x.IsActive);
                if (doc is null)
                {
                    return null;
                }

                var defaults = await LoadDefaultAccountBundleAsync(dbContext, doc.CompanyId);
                var customerName = doc.CustomerId.HasValue
                    ? await dbContext.Set<Customer>().Where(x => x.Id == doc.CustomerId.Value).Select(x => x.Name).FirstOrDefaultAsync() ?? string.Empty
                    : string.Empty;
                var existing = await FindExistingAutomaticEntryAsync(dbContext, doc.CompanyId, sourceType, doc.Id);
                return BuildAutoPolicyPreview(
                    doc.TenantId,
                    doc.CompanyId,
                    doc.BranchId,
                    sourceType,
                    doc.Id,
                    doc.CreditNoteDate,
                    doc.Folio,
                    customerName,
                    doc.Total,
                    $"Nota de crédito {doc.Folio}",
                    defaults.Sales,
                    defaults.Customers,
                    doc.CustomerId.HasValue,
                    existing,
                    "Falta cuenta 4000 Ventas, 1050 Clientes o cliente en el documento.");
            }
            case "purchase_invoice":
            {
                var doc = await dbContext.Set<PurchaseInvoice>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == sourceId && x.IsActive);
                if (doc is null)
                {
                    return null;
                }

                var defaults = await LoadDefaultAccountBundleAsync(dbContext, doc.CompanyId);
                var supplierName = doc.SupplierId.HasValue
                    ? await dbContext.Set<Supplier>().Where(x => x.Id == doc.SupplierId.Value).Select(x => x.Name).FirstOrDefaultAsync() ?? string.Empty
                    : string.Empty;
                var existing = await FindExistingAutomaticEntryAsync(dbContext, doc.CompanyId, sourceType, doc.Id);
                return BuildAutoPolicyPreview(
                    doc.TenantId,
                    doc.CompanyId,
                    doc.BranchId,
                    sourceType,
                    doc.Id,
                    doc.InvoiceDate,
                    doc.Folio,
                    supplierName,
                    doc.Total,
                    $"Factura de proveedor {doc.Folio}",
                    defaults.Expenses,
                    defaults.Suppliers,
                    doc.SupplierId.HasValue,
                    existing,
                    "Falta cuenta 6000 Gastos, 2000 Proveedores o proveedor en el documento.");
            }
            case "receipt":
            {
                var doc = await dbContext.Set<Receipt>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == sourceId && x.IsActive);
                if (doc is null)
                {
                    return null;
                }

                var defaults = await LoadDefaultAccountBundleAsync(dbContext, doc.CompanyId);
                var customerName = doc.CustomerId.HasValue
                    ? await dbContext.Set<Customer>().Where(x => x.Id == doc.CustomerId.Value).Select(x => x.Name).FirstOrDefaultAsync() ?? string.Empty
                    : string.Empty;
                var debitAccount = doc.BankAccountId.HasValue || string.Equals(doc.TargetType, "bank", StringComparison.OrdinalIgnoreCase)
                    ? defaults.Banks
                    : defaults.Cash;
                var existing = await FindExistingAutomaticEntryAsync(dbContext, doc.CompanyId, sourceType, doc.Id);
                return BuildAutoPolicyPreview(
                    doc.TenantId,
                    doc.CompanyId,
                    doc.BranchId,
                    sourceType,
                    doc.Id,
                    doc.ReceiptDate,
                    doc.Folio,
                    customerName,
                    doc.Total,
                    $"Recibo {doc.Folio}",
                    debitAccount,
                    defaults.Customers,
                    doc.CustomerId.HasValue,
                    existing,
                    "Falta cuenta 1000 Caja o 1010 Bancos, 1050 Clientes o cliente en el documento.");
            }
            case "payment":
            {
                var doc = await dbContext.Set<Payment>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == sourceId && x.IsActive);
                if (doc is null)
                {
                    return null;
                }

                var defaults = await LoadDefaultAccountBundleAsync(dbContext, doc.CompanyId);
                var supplierName = doc.SupplierId.HasValue
                    ? await dbContext.Set<Supplier>().Where(x => x.Id == doc.SupplierId.Value).Select(x => x.Name).FirstOrDefaultAsync() ?? string.Empty
                    : string.Empty;
                var creditAccount = doc.BankAccountId.HasValue || string.Equals(doc.SourceType, "bank", StringComparison.OrdinalIgnoreCase)
                    ? defaults.Banks
                    : defaults.Cash;
                var existing = await FindExistingAutomaticEntryAsync(dbContext, doc.CompanyId, sourceType, doc.Id);
                return BuildAutoPolicyPreview(
                    doc.TenantId,
                    doc.CompanyId,
                    doc.BranchId,
                    sourceType,
                    doc.Id,
                    doc.PaymentDate,
                    doc.Folio,
                    supplierName,
                    doc.Total,
                    $"Pago {doc.Folio}",
                    defaults.Suppliers,
                    creditAccount,
                    doc.SupplierId.HasValue,
                    existing,
                    "Falta cuenta 1000 Caja o 1010 Bancos, 2000 Proveedores o proveedor en el documento.");
            }
            default:
                return null;
        }
    }

    private static AccountingAutoPolicyPreviewDto BuildAutoPolicyPreview(
        Guid tenantId,
        Guid companyId,
        Guid branchId,
        string sourceType,
        Guid sourceId,
        DateTime documentDate,
        string folio,
        string thirdPartyName,
        decimal total,
        string concept,
        AccountingAccount? debitAccount,
        AccountingAccount? creditAccount,
        bool hasThirdParty,
        ExistingAutomaticEntryInfo? existing,
        string missingMessage)
    {
        var lines = new List<AccountingAutoPolicyPreviewLineDto>();
        var ready = debitAccount is not null && creditAccount is not null && hasThirdParty && total > 0m;

        if (debitAccount is not null)
        {
            lines.Add(new AccountingAutoPolicyPreviewLineDto(
                debitAccount.Id,
                debitAccount.Code,
                debitAccount.Name,
                concept,
                total,
                0m));
        }

        if (creditAccount is not null)
        {
            lines.Add(new AccountingAutoPolicyPreviewLineDto(
                creditAccount.Id,
                creditAccount.Code,
                creditAccount.Name,
                concept,
                0m,
                total));
        }

        return new AccountingAutoPolicyPreviewDto(
            tenantId,
            companyId,
            branchId,
            sourceType,
            sourceId,
            EnsureUtc(documentDate),
            folio,
            thirdPartyName,
            total,
            concept,
            BuildAutoPolicyReference(sourceType, sourceId),
            ready,
            existing?.JournalEntryId,
            existing?.Folio,
            existing?.Status,
            ready ? null : missingMessage,
            lines);
    }

    private static async Task<AccountingAutoPolicyGenerateResultDto> GenerateAutoPolicyAsync(NanchesoftDbContext dbContext, string sourceType, Guid sourceId)
    {
        var preview = await BuildAutoPolicyPreviewAsync(dbContext, sourceType, sourceId);
        if (preview is null)
        {
            return new AccountingAutoPolicyGenerateResultDto(false, 0, 0, 1, "El documento origen no existe.", new List<string>());
        }

        if (preview.ExistingJournalEntryId.HasValue)
        {
            return new AccountingAutoPolicyGenerateResultDto(true, 0, 1, 0, $"Ya existe la póliza {preview.ExistingJournalFolio} para este documento.", new List<string>());
        }

        if (!preview.IsReady)
        {
            return new AccountingAutoPolicyGenerateResultDto(false, 0, 0, 1, preview.Message ?? "La póliza automática no está lista para generarse.", new List<string>());
        }

        var periodError = await EnsureOpenPeriodAsync(dbContext, preview.CompanyId, preview.EntryDate);
        if (periodError is not null)
        {
            return new AccountingAutoPolicyGenerateResultDto(false, 0, 0, 1, periodError, new List<string>());
        }

        var header = new AccountingJournalEntry
        {
            Id = Guid.NewGuid(),
            TenantId = preview.TenantId,
            CompanyId = preview.CompanyId,
            BranchId = preview.BranchId,
            Folio = await ResolveAutomaticEntryFolioAsync(dbContext, preview.CompanyId),
            EntryDate = preview.EntryDate,
            EntryType = "automatic",
            Status = "draft",
            Reference = preview.ReferenceKey,
            Concept = preview.Concept,
            TotalDebit = preview.Lines.Sum(x => x.Debit),
            TotalCredit = preview.Lines.Sum(x => x.Credit),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "auto-policy"
        };

        dbContext.Set<AccountingJournalEntry>().Add(header);
        AddJournalLines(
            dbContext,
            header.Id,
            preview.Lines.Select(x => new SaveJournalEntryLineRequest(x.AccountId, x.Description, x.Debit, x.Credit, null)));

        await dbContext.SaveChangesAsync();
        return new AccountingAutoPolicyGenerateResultDto(true, 1, 0, 0, $"Se generó la póliza automática {header.Folio} en borrador.", new List<string> { header.Folio });
    }

    private static async Task<ExistingAutomaticEntryInfo?> FindExistingAutomaticEntryAsync(NanchesoftDbContext dbContext, Guid companyId, string sourceType, Guid sourceId)
    {
        var reference = BuildAutoPolicyReference(sourceType, sourceId);
        var row = await dbContext.Set<AccountingJournalEntry>()
            .Where(x => x.CompanyId == companyId && x.IsActive && x.Reference == reference)
            .Select(x => new ExistingAutomaticEntryInfo(x.Id, x.Folio, x.Status))
            .FirstOrDefaultAsync();

        return row;
    }

    private static async Task<DefaultAccountBundle> LoadDefaultAccountBundleAsync(NanchesoftDbContext dbContext, Guid? companyId)
    {
        var accountsQuery = dbContext.Set<AccountingAccount>().AsNoTracking().Where(x => x.IsActive);
        if (companyId.HasValue)
        {
            accountsQuery = accountsQuery.Where(x => x.CompanyId == companyId.Value);
        }

        var accounts = await accountsQuery
            .Where(x => x.Code == "1000" || x.Code == "1010" || x.Code == "1050" || x.Code == "2000" || x.Code == "4000" || x.Code == "6000")
            .ToListAsync();

        return new DefaultAccountBundle(
            accounts.FirstOrDefault(x => x.Code == "1000"),
            accounts.FirstOrDefault(x => x.Code == "1010"),
            accounts.FirstOrDefault(x => x.Code == "1050"),
            accounts.FirstOrDefault(x => x.Code == "2000"),
            accounts.FirstOrDefault(x => x.Code == "4000"),
            accounts.FirstOrDefault(x => x.Code == "6000"));
    }

    private static async Task<string> ResolveAutomaticEntryFolioAsync(NanchesoftDbContext dbContext, Guid companyId)
    {
        var count = await dbContext.Set<AccountingJournalEntry>()
            .CountAsync(x => x.CompanyId == companyId && x.EntryType == "automatic");
        return $"AUT-{count + 1:000000}";
    }

    private static string BuildAutoPolicyReference(string sourceType, Guid sourceId)
        => $"AUTO|{NormalizeAutoPolicySourceType(sourceType)}|{sourceId:D}";

    private static string NormalizeAutoPolicySourceType(string? sourceType)
    {
        return (sourceType ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "sales_invoice" => "sales_invoice",
            "credit_note" => "credit_note",
            "purchase_invoice" => "purchase_invoice",
            "receipt" => "receipt",
            "payment" => "payment",
            _ => "all"
        };
    }

    private static string GetSourceTypeDisplayName(string sourceType)
    {
        return NormalizeAutoPolicySourceType(sourceType) switch
        {
            "sales_invoice" => "Factura de venta",
            "credit_note" => "Nota de crédito",
            "purchase_invoice" => "Factura de proveedor",
            "receipt" => "Recibo",
            "payment" => "Pago",
            _ => "Documento"
        };
    }

    private static string GetMonthName(int month)
    {
        return month switch
        {
            1 => "Enero",
            2 => "Febrero",
            3 => "Marzo",
            4 => "Abril",
            5 => "Mayo",
            6 => "Junio",
            7 => "Julio",
            8 => "Agosto",
            9 => "Septiembre",
            10 => "Octubre",
            11 => "Noviembre",
            12 => "Diciembre",
            _ => "Mes"
        };
    }

    private sealed record ContextInfo(Guid TenantId, Guid CompanyId, Guid BranchId);
    private sealed record PostedLineRow(Guid AccountId, decimal Debit, decimal Credit);
    private sealed record AccountingReportRange(DateTime StartUtc, DateTime EndUtc);

    public sealed record AccountingLookupsDto(
        Guid TenantId,
        Guid CompanyId,
        Guid BranchId,
        int CurrentYear,
        int CurrentMonth,
        List<AccountingLookupItemDto> Accounts,
        List<AccountingLookupItemDto> Periods);

    public sealed record AccountingLookupItemDto(
        Guid Id,
        string Code,
        string Name,
        string? Extra);

    public sealed record AccountingDashboardDto(
        int ActiveAccounts,
        int JournalEntries,
        int DraftEntries,
        int ApprovedEntries,
        int PostedEntries,
        int OpenPeriods,
        decimal AssetsTotal,
        decimal LiabilitiesAndEquityTotal,
        decimal ResultOfPeriod);

    public sealed record AccountingAccountRowDto(
        Guid Id,
        Guid CompanyId,
        string Code,
        string Name,
        string AccountType,
        string Nature,
        Guid? ParentAccountId,
        string? ParentAccountCode,
        string? ParentAccountName,
        bool AllowsPosting,
        bool IsActive);

    public sealed record SaveAccountingAccountRequest(
        Guid TenantId,
        Guid CompanyId,
        string Code,
        string Name,
        string AccountType,
        string Nature,
        Guid? ParentAccountId,
        bool AllowsPosting);

    public sealed record FiscalPeriodRowDto(
        Guid Id,
        int Year,
        int Month,
        DateTime StartDate,
        DateTime EndDate,
        string Status,
        bool IsActive);

    public sealed record AccountingJournalEntryRowDto(
        Guid Id,
        Guid CompanyId,
        Guid BranchId,
        string Folio,
        DateTime EntryDate,
        string EntryType,
        string Status,
        string Reference,
        string Concept,
        decimal TotalDebit,
        decimal TotalCredit,
        bool IsBalanced);

    public sealed record AccountingJournalEntryDetailDto(
        Guid Id,
        Guid TenantId,
        Guid CompanyId,
        Guid BranchId,
        string Folio,
        DateTime EntryDate,
        string EntryType,
        string Status,
        string Reference,
        string Concept,
        decimal TotalDebit,
        decimal TotalCredit,
        List<AccountingJournalEntryLineDto> Lines);

    public sealed record AccountingJournalEntryLineDto(
        Guid Id,
        int LineNumber,
        Guid AccountId,
        string AccountCode,
        string AccountName,
        string Description,
        decimal Debit,
        decimal Credit,
        Guid? CostCenterId);

    public sealed record SaveJournalEntryRequest(
        Guid TenantId,
        Guid CompanyId,
        Guid BranchId,
        string Folio,
        DateTime EntryDate,
        string EntryType,
        string Reference,
        string Concept,
        List<SaveJournalEntryLineRequest> Lines);

    public sealed record SaveJournalEntryLineRequest(
        Guid AccountId,
        string Description,
        decimal Debit,
        decimal Credit,
        Guid? CostCenterId);

    public sealed record TrialBalanceRowDto(
        Guid AccountId,
        string Code,
        string Name,
        decimal Debit,
        decimal Credit,
        decimal Balance);

    public sealed record LedgerRowDto(
        Guid JournalEntryId,
        string Folio,
        DateTime EntryDate,
        string Reference,
        string Concept,
        string LineDescription,
        decimal Debit,
        decimal Credit,
        decimal RunningBalance);

    public sealed record BalanceSheetDto(
        List<FinancialStatementRowDto> Assets,
        List<FinancialStatementRowDto> Liabilities,
        List<FinancialStatementRowDto> Equity,
        decimal TotalAssets,
        decimal TotalLiabilities,
        decimal TotalEquity);

    public sealed record IncomeStatementDto(
        List<FinancialStatementRowDto> Income,
        List<FinancialStatementRowDto> Expenses,
        decimal TotalIncome,
        decimal TotalExpense,
        decimal NetIncome);

    public sealed record FinancialStatementRowDto(
        Guid AccountId,
        string Code,
        string Name,
        decimal Amount);

    public sealed record AccountingAutoPolicySourceDto(
        string SourceType,
        Guid SourceId,
        DateTime DocumentDate,
        string Folio,
        string ThirdPartyName,
        decimal Total,
        string DebitAccountCode,
        string DebitAccountName,
        string CreditAccountCode,
        string CreditAccountName,
        string Concept,
        Guid? ExistingJournalEntryId,
        string? ExistingJournalFolio,
        string? ExistingJournalStatus,
        bool IsReady,
        string? Message);

    public sealed record AccountingAutoPolicyPreviewDto(
        Guid TenantId,
        Guid CompanyId,
        Guid BranchId,
        string SourceType,
        Guid SourceId,
        DateTime EntryDate,
        string Folio,
        string ThirdPartyName,
        decimal Total,
        string Concept,
        string ReferenceKey,
        bool IsReady,
        Guid? ExistingJournalEntryId,
        string? ExistingJournalFolio,
        string? ExistingJournalStatus,
        string? Message,
        List<AccountingAutoPolicyPreviewLineDto> Lines);

    public sealed record AccountingAutoPolicyPreviewLineDto(
        Guid AccountId,
        string AccountCode,
        string AccountName,
        string Description,
        decimal Debit,
        decimal Credit);

    public sealed record AccountingAutoPolicyGenerateRequest(string SourceType, Guid SourceId);

    public sealed record AccountingAutoPolicyGenerateResultDto(
        bool Success,
        int CreatedCount,
        int SkippedCount,
        int FailedCount,
        string Message,
        List<string> Details);

    public sealed class AccountingAutoPolicySummaryDto
    {
        public int PendingSalesInvoices { get; set; }
        public int PendingCreditNotes { get; set; }
        public int PendingPurchaseInvoices { get; set; }
        public int PendingReceipts { get; set; }
        public int PendingPayments { get; set; }
        public int DraftAutomaticEntries { get; set; }
        public int ApprovedAutomaticEntries { get; set; }
        public int PostedAutomaticEntries { get; set; }
        public int PendingTotal => PendingSalesInvoices + PendingCreditNotes + PendingPurchaseInvoices + PendingReceipts + PendingPayments;
    }


    public sealed class AccountingMonthlyClosePreviewDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public DateTime ClosingDate { get; set; }
        public Guid? FiscalPeriodId { get; set; }
        public string PeriodStatus { get; set; } = "missing";
        public Guid? ClosingAccountId { get; set; }
        public string? ClosingAccountCode { get; set; }
        public string? ClosingAccountName { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal TotalExpense { get; set; }
        public decimal NetResult { get; set; }
        public Guid? ExistingJournalEntryId { get; set; }
        public string? ExistingJournalFolio { get; set; }
        public string? ExistingJournalStatus { get; set; }
        public bool IsReady { get; set; }
        public List<string> Messages { get; set; } = new();
        public List<AccountingMonthlyCloseLineDto> SourceLines { get; set; } = new();
        public List<AccountingMonthlyCloseEntryLineDto> CloseLines { get; set; } = new();
    }

    public sealed record AccountingMonthlyCloseLineDto(
        Guid AccountId,
        string AccountCode,
        string AccountName,
        string AccountType,
        decimal Balance,
        decimal IncomeAmount,
        decimal ExpenseAmount);

    public sealed record AccountingMonthlyCloseEntryLineDto(
        Guid AccountId,
        string AccountCode,
        string AccountName,
        string Description,
        decimal Debit,
        decimal Credit);

    public sealed record AccountingMonthlyCloseGenerateRequest(int Year, int Month);

    public sealed record AccountingMonthlyCloseGenerateResultDto(bool Success, Guid JournalEntryId, string? JournalFolio, string Message);


    private sealed record DefaultAccountBundle(AccountingAccount? Cash, AccountingAccount? Banks, AccountingAccount? Customers, AccountingAccount? Suppliers, AccountingAccount? Sales, AccountingAccount? Expenses);
    private sealed record ExistingAutomaticEntryInfo(Guid JournalEntryId, string Folio, string Status);
}
