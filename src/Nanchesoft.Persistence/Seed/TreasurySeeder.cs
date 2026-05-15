using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Persistence.Seed;

public static class TreasurySeeder
{
    public static async Task SeedAsync(NanchesoftDbContext dbContext)
    {
        const string seedUser = "seed";

        var tenant = await dbContext.Tenants.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        var company = await dbContext.Companies.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        var branch = company is null ? null : await dbContext.Branches.Where(x => x.CompanyId == company.Id).OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        var currency = await dbContext.Currencies.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync(x => x.Code == "MXN")
                      ?? await dbContext.Currencies.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        var bank = await dbContext.Banks.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        var customer = company is null ? null : await dbContext.Customers.Where(x => x.CompanyId == company.Id).OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        var supplier = company is null ? null : await dbContext.Suppliers.Where(x => x.CompanyId == company.Id).OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        var salesInvoice = company is null ? null : await dbContext.SalesInvoices.Where(x => x.CompanyId == company.Id).OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        var purchaseInvoice = company is null ? null : await dbContext.PurchaseInvoices.Where(x => x.CompanyId == company.Id).OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();

        if (tenant is null || company is null || branch is null || currency is null)
            return;

        await SeedPermissionsAndNavigationAsync(dbContext, seedUser);

        var cash = await dbContext.CashAccounts.FirstOrDefaultAsync(x => x.CompanyId == company.Id && x.Code == "CAJA-MAT");
        if (cash is null)
        {
            cash = new CashAccount
            {
                Id = Guid.Parse("D1000000-0000-0000-0000-000000000001"),
                TenantId = tenant.Id,
                CompanyId = company.Id,
                BranchId = branch.Id,
                CurrencyId = currency.Id,
                Code = "CAJA-MAT",
                Name = "Caja Matriz",
                Status = "active",
                CurrentBalance = 0m,
                CreatedBy = seedUser
            };
            dbContext.CashAccounts.Add(cash);
            await dbContext.SaveChangesAsync();
        }

        var bankAccount = await dbContext.BankAccounts.FirstOrDefaultAsync(x => x.CompanyId == company.Id && x.Code == "BBVA-001");
        if (bankAccount is null)
        {
            bankAccount = new BankAccount
            {
                Id = Guid.Parse("D1000000-0000-0000-0000-000000000002"),
                TenantId = tenant.Id,
                CompanyId = company.Id,
                BankId = bank?.Id,
                CurrencyId = currency.Id,
                Code = "BBVA-001",
                Name = "BBVA Cuenta Operativa",
                AccountHolder = company.Name,
                AccountNumber = "1234567890",
                Clabe = "012345678901234567",
                Status = "active",
                CurrentBalance = 0m,
                CreatedBy = seedUser
            };
            dbContext.BankAccounts.Add(bankAccount);
            await dbContext.SaveChangesAsync();
        }

        if (!await dbContext.TreasuryIncomes.AnyAsync(x => x.CompanyId == company.Id && x.Folio == "INGR-0001"))
        {
            dbContext.TreasuryIncomes.Add(new TreasuryIncome
            {
                Id = Guid.Parse("D1000000-0000-0000-0000-000000000010"),
                TenantId = tenant.Id,
                CompanyId = company.Id,
                BranchId = branch.Id,
                CurrencyId = currency.Id,
                CashAccountId = cash.Id,
                Folio = "INGR-0001",
                IncomeDate = DateTime.UtcNow.Date,
                TargetType = "cash",
                ExchangeRate = 1m,
                Status = "posted",
                Reference = "Ingreso demo",
                Notes = "Ingreso base sprint 8.",
                Total = 2500m,
                ApprovedAt = DateTime.UtcNow,
                PostedAt = DateTime.UtcNow,
                CreatedBy = seedUser,
                Lines = new List<TreasuryIncomeLine>
                {
                    new TreasuryIncomeLine
                    {
                        Id = Guid.Parse("D1000000-0000-0000-0000-000000000011"),
                        LineNumber = 1,
                        Description = "Aportación inicial caja",
                        Amount = 2500m,
                        CustomerId = customer?.Id,
                        SalesInvoiceId = salesInvoice?.Id,
                        CreatedBy = seedUser
                    }
                }
            });
            await dbContext.SaveChangesAsync();
        }

        if (!await dbContext.TreasuryExpenses.AnyAsync(x => x.CompanyId == company.Id && x.Folio == "EGR-0001"))
        {
            dbContext.TreasuryExpenses.Add(new TreasuryExpense
            {
                Id = Guid.Parse("D1000000-0000-0000-0000-000000000020"),
                TenantId = tenant.Id,
                CompanyId = company.Id,
                BranchId = branch.Id,
                CurrencyId = currency.Id,
                CashAccountId = cash.Id,
                Folio = "EGR-0001",
                ExpenseDate = DateTime.UtcNow.Date,
                SourceType = "cash",
                ExchangeRate = 1m,
                Status = "approved",
                Reference = "Gasto demo",
                Notes = "Gasto pendiente de posteo.",
                Total = 650m,
                ApprovedAt = DateTime.UtcNow,
                CreatedBy = seedUser,
                Lines = new List<TreasuryExpenseLine>
                {
                    new TreasuryExpenseLine
                    {
                        Id = Guid.Parse("D1000000-0000-0000-0000-000000000021"),
                        LineNumber = 1,
                        Description = "Papelería y suministros",
                        Amount = 650m,
                        SupplierId = supplier?.Id,
                        PurchaseInvoiceId = purchaseInvoice?.Id,
                        CreatedBy = seedUser
                    }
                }
            });
            await dbContext.SaveChangesAsync();
        }

        if (!await dbContext.Receipts.AnyAsync(x => x.CompanyId == company.Id && x.Folio == "REC-0001"))
        {
            dbContext.Receipts.Add(new Receipt
            {
                Id = Guid.Parse("D1000000-0000-0000-0000-000000000030"),
                TenantId = tenant.Id,
                CompanyId = company.Id,
                BranchId = branch.Id,
                CustomerId = customer?.Id,
                CurrencyId = currency.Id,
                BankAccountId = bankAccount.Id,
                Folio = "REC-0001",
                ReceiptDate = DateTime.UtcNow.Date,
                TargetType = "bank",
                ExchangeRate = 1m,
                Status = "posted",
                Reference = "Cobro demo",
                Total = 18560m,
                ApprovedAt = DateTime.UtcNow,
                PostedAt = DateTime.UtcNow,
                CreatedBy = seedUser,
                Lines = new List<ReceiptLine>
                {
                    new ReceiptLine
                    {
                        Id = Guid.Parse("D1000000-0000-0000-0000-000000000031"),
                        LineNumber = 1,
                        Description = "Cobro factura demo ventas",
                        Amount = 18560m,
                        SalesInvoiceId = salesInvoice?.Id,
                        CreatedBy = seedUser
                    }
                }
            });
            await dbContext.SaveChangesAsync();
        }

        if (!await dbContext.Payments.AnyAsync(x => x.CompanyId == company.Id && x.Folio == "PAG-0001"))
        {
            dbContext.Payments.Add(new Payment
            {
                Id = Guid.Parse("D1000000-0000-0000-0000-000000000040"),
                TenantId = tenant.Id,
                CompanyId = company.Id,
                BranchId = branch.Id,
                SupplierId = supplier?.Id,
                CurrencyId = currency.Id,
                BankAccountId = bankAccount.Id,
                Folio = "PAG-0001",
                PaymentDate = DateTime.UtcNow.Date,
                SourceType = "bank",
                ExchangeRate = 1m,
                Status = "approved",
                Reference = "Pago demo",
                Total = 11600m,
                ApprovedAt = DateTime.UtcNow,
                CreatedBy = seedUser,
                Lines = new List<PaymentLine>
                {
                    new PaymentLine
                    {
                        Id = Guid.Parse("D1000000-0000-0000-0000-000000000041"),
                        LineNumber = 1,
                        Description = "Pago factura demo compras",
                        Amount = 11600m,
                        PurchaseInvoiceId = purchaseInvoice?.Id,
                        CreatedBy = seedUser
                    }
                }
            });
            await dbContext.SaveChangesAsync();
        }

        await EnsurePostedMovementAsync(dbContext, cash, bankAccount, seedUser);
        await EnsureReconciliationAsync(dbContext, tenant.Id, company.Id, bankAccount.Id, seedUser);
    }

    private static async Task EnsurePostedMovementAsync(NanchesoftDbContext dbContext, CashAccount cash, BankAccount bankAccount, string seedUser)
    {
        cash.CurrentBalance = 0m;
        bankAccount.CurrentBalance = 0m;

        var bankMovementIds = await dbContext.BankMovements
            .Where(x => x.BankAccountId == bankAccount.Id && (x.DocumentType == "receipt" || x.DocumentType == "payment"))
            .Select(x => x.Id)
            .ToListAsync();

        if (bankMovementIds.Count > 0)
        {
            var reconciliationLines = await dbContext.ReconciliationLines
                .Where(x => bankMovementIds.Contains(x.BankMovementId))
                .ToListAsync();

            if (reconciliationLines.Count > 0)
            {
                dbContext.ReconciliationLines.RemoveRange(reconciliationLines);
                await dbContext.SaveChangesAsync();
            }
        }

        dbContext.CashMovements.RemoveRange(
            dbContext.CashMovements.Where(x => x.CashAccountId == cash.Id && (x.DocumentType == "treasury_income" || x.DocumentType == "treasury_expense")));

        dbContext.BankMovements.RemoveRange(
            dbContext.BankMovements.Where(x => x.BankAccountId == bankAccount.Id && (x.DocumentType == "receipt" || x.DocumentType == "payment")));

        await dbContext.SaveChangesAsync();

        foreach (var income in await dbContext.TreasuryIncomes.Where(x => x.CashAccountId == cash.Id && x.Status == "posted").OrderBy(x => x.IncomeDate).ToListAsync())
        {
            cash.CurrentBalance += income.Total;
            dbContext.CashMovements.Add(new CashMovement
            {
                Id = Guid.NewGuid(),
                TenantId = income.TenantId,
                CompanyId = income.CompanyId,
                BranchId = income.BranchId,
                CashAccountId = cash.Id,
                MovementDate = income.PostedAt ?? income.IncomeDate,
                MovementType = "income",
                DocumentType = "treasury_income",
                DocumentId = income.Id,
                Reference = income.Reference,
                AmountIn = income.Total,
                AmountOut = 0m,
                BalanceAfter = cash.CurrentBalance,
                CreatedBy = seedUser
            });
        }

        foreach (var receipt in await dbContext.Receipts.Where(x => x.BankAccountId == bankAccount.Id && x.Status == "posted").OrderBy(x => x.ReceiptDate).ToListAsync())
        {
            bankAccount.CurrentBalance += receipt.Total;
            dbContext.BankMovements.Add(new BankMovement
            {
                Id = Guid.NewGuid(),
                TenantId = receipt.TenantId,
                CompanyId = receipt.CompanyId,
                BankAccountId = bankAccount.Id,
                MovementDate = receipt.PostedAt ?? receipt.ReceiptDate,
                MovementType = "receipt",
                DocumentType = "receipt",
                DocumentId = receipt.Id,
                Reference = receipt.Reference,
                AmountIn = receipt.Total,
                AmountOut = 0m,
                BalanceAfter = bankAccount.CurrentBalance,
                CreatedBy = seedUser
            });
        }

        bankAccount.UpdatedAt = DateTime.UtcNow;
        bankAccount.UpdatedBy = seedUser;
        cash.UpdatedAt = DateTime.UtcNow;
        cash.UpdatedBy = seedUser;
        await dbContext.SaveChangesAsync();
    }

    private static async Task EnsureReconciliationAsync(NanchesoftDbContext dbContext, Guid tenantId, Guid companyId, Guid bankAccountId, string seedUser)
    {
        var bankMovements = await dbContext.BankMovements
            .Where(x => x.BankAccountId == bankAccountId)
            .OrderBy(x => x.MovementDate)
            .ToListAsync();

        var statementBalance = bankMovements.Sum(x => x.AmountIn - x.AmountOut);
        var bookBalance = bankMovements.Where(x => x.IsReconciled).Sum(x => x.AmountIn - x.AmountOut);

        var existing = await dbContext.Reconciliations
            .FirstOrDefaultAsync(x => x.BankAccountId == bankAccountId && x.Status != "cancelled");

        if (existing is not null)
        {
            var existingLines = await dbContext.ReconciliationLines
                .Where(x => x.ReconciliationId == existing.Id)
                .ToListAsync();

            if (existingLines.Count > 0)
            {
                dbContext.ReconciliationLines.RemoveRange(existingLines);
                await dbContext.SaveChangesAsync();
            }

            existing.StatementBalance = statementBalance;
            existing.BookBalance = bookBalance;
            existing.DifferenceAmount = statementBalance - bookBalance;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedBy = seedUser;

            await dbContext.SaveChangesAsync();

            if (bankMovements.Count > 0)
            {
                var refreshedLines = bankMovements.Select(x => new ReconciliationLine
                {
                    Id = Guid.NewGuid(),
                    ReconciliationId = existing.Id,
                    BankMovementId = x.Id,
                    IsChecked = x.IsReconciled,
                    MovementAmount = x.AmountIn - x.AmountOut,
                    CreatedBy = seedUser
                }).ToList();

                await dbContext.ReconciliationLines.AddRangeAsync(refreshedLines);
                await dbContext.SaveChangesAsync();
            }

            return;
        }

        dbContext.Reconciliations.Add(new Reconciliation
        {
            Id = Guid.Parse("D1000000-0000-0000-0000-000000000050"),
            TenantId = tenantId,
            CompanyId = companyId,
            BankAccountId = bankAccountId,
            ReconciliationDate = DateTime.UtcNow.Date,
            StatementBalance = statementBalance,
            BookBalance = bookBalance,
            DifferenceAmount = statementBalance - bookBalance,
            Status = "in_progress",
            CreatedBy = seedUser,
            Lines = bankMovements.Select(x => new ReconciliationLine
            {
                Id = Guid.NewGuid(),
                BankMovementId = x.Id,
                IsChecked = x.IsReconciled,
                MovementAmount = x.AmountIn - x.AmountOut,
                CreatedBy = seedUser
            }).ToList()
        });

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedPermissionsAndNavigationAsync(NanchesoftDbContext dbContext, string seedUser)
    {
        var permissions = new[]
        {
            new Permission { Id = Guid.Parse("7F000000-0000-0000-0000-000000000001"), Module = "treasury", Resource = "cashaccount", Action = "view", Code = "treasury.cashaccount.view", Name = "Ver cajas", CreatedBy = seedUser },
            new Permission { Id = Guid.Parse("7F000000-0000-0000-0000-000000000002"), Module = "treasury", Resource = "cashaccount", Action = "create", Code = "treasury.cashaccount.create", Name = "Crear cajas", CreatedBy = seedUser },
            new Permission { Id = Guid.Parse("7F000000-0000-0000-0000-000000000003"), Module = "treasury", Resource = "cashaccount", Action = "edit", Code = "treasury.cashaccount.edit", Name = "Editar cajas", CreatedBy = seedUser },
            new Permission { Id = Guid.Parse("7F000000-0000-0000-0000-000000000004"), Module = "treasury", Resource = "bankaccount", Action = "view", Code = "treasury.bankaccount.view", Name = "Ver cuentas bancarias", CreatedBy = seedUser },
            new Permission { Id = Guid.Parse("7F000000-0000-0000-0000-000000000005"), Module = "treasury", Resource = "bankaccount", Action = "create", Code = "treasury.bankaccount.create", Name = "Crear cuentas bancarias", CreatedBy = seedUser },
            new Permission { Id = Guid.Parse("7F000000-0000-0000-0000-000000000006"), Module = "treasury", Resource = "bankaccount", Action = "edit", Code = "treasury.bankaccount.edit", Name = "Editar cuentas bancarias", CreatedBy = seedUser },
            new Permission { Id = Guid.Parse("7F000000-0000-0000-0000-000000000007"), Module = "treasury", Resource = "income", Action = "view", Code = "treasury.income.view", Name = "Ver ingresos", CreatedBy = seedUser },
            new Permission { Id = Guid.Parse("7F000000-0000-0000-0000-000000000008"), Module = "treasury", Resource = "income", Action = "create", Code = "treasury.income.create", Name = "Crear ingresos", CreatedBy = seedUser },
            new Permission { Id = Guid.Parse("7F000000-0000-0000-0000-000000000009"), Module = "treasury", Resource = "income", Action = "approve", Code = "treasury.income.approve", Name = "Aprobar ingresos", CreatedBy = seedUser },
            new Permission { Id = Guid.Parse("7F000000-0000-0000-0000-000000000010"), Module = "treasury", Resource = "income", Action = "post", Code = "treasury.income.post", Name = "Postear ingresos", CreatedBy = seedUser },
            new Permission { Id = Guid.Parse("7F000000-0000-0000-0000-000000000011"), Module = "treasury", Resource = "expense", Action = "view", Code = "treasury.expense.view", Name = "Ver egresos", CreatedBy = seedUser },
            new Permission { Id = Guid.Parse("7F000000-0000-0000-0000-000000000012"), Module = "treasury", Resource = "expense", Action = "create", Code = "treasury.expense.create", Name = "Crear egresos", CreatedBy = seedUser },
            new Permission { Id = Guid.Parse("7F000000-0000-0000-0000-000000000013"), Module = "treasury", Resource = "expense", Action = "approve", Code = "treasury.expense.approve", Name = "Aprobar egresos", CreatedBy = seedUser },
            new Permission { Id = Guid.Parse("7F000000-0000-0000-0000-000000000014"), Module = "treasury", Resource = "expense", Action = "post", Code = "treasury.expense.post", Name = "Postear egresos", CreatedBy = seedUser },
            new Permission { Id = Guid.Parse("7F000000-0000-0000-0000-000000000015"), Module = "treasury", Resource = "receipt", Action = "view", Code = "treasury.receipt.view", Name = "Ver recibos", CreatedBy = seedUser },
            new Permission { Id = Guid.Parse("7F000000-0000-0000-0000-000000000016"), Module = "treasury", Resource = "receipt", Action = "create", Code = "treasury.receipt.create", Name = "Crear recibos", CreatedBy = seedUser },
            new Permission { Id = Guid.Parse("7F000000-0000-0000-0000-000000000017"), Module = "treasury", Resource = "receipt", Action = "approve", Code = "treasury.receipt.approve", Name = "Aprobar recibos", CreatedBy = seedUser },
            new Permission { Id = Guid.Parse("7F000000-0000-0000-0000-000000000018"), Module = "treasury", Resource = "receipt", Action = "post", Code = "treasury.receipt.post", Name = "Postear recibos", CreatedBy = seedUser },
            new Permission { Id = Guid.Parse("7F000000-0000-0000-0000-000000000019"), Module = "treasury", Resource = "payment", Action = "view", Code = "treasury.payment.view", Name = "Ver pagos", CreatedBy = seedUser },
            new Permission { Id = Guid.Parse("7F000000-0000-0000-0000-000000000020"), Module = "treasury", Resource = "payment", Action = "create", Code = "treasury.payment.create", Name = "Crear pagos", CreatedBy = seedUser },
            new Permission { Id = Guid.Parse("7F000000-0000-0000-0000-000000000021"), Module = "treasury", Resource = "payment", Action = "approve", Code = "treasury.payment.approve", Name = "Aprobar pagos", CreatedBy = seedUser },
            new Permission { Id = Guid.Parse("7F000000-0000-0000-0000-000000000022"), Module = "treasury", Resource = "payment", Action = "post", Code = "treasury.payment.post", Name = "Postear pagos", CreatedBy = seedUser },
            new Permission { Id = Guid.Parse("7F000000-0000-0000-0000-000000000023"), Module = "treasury", Resource = "reconciliation", Action = "view", Code = "treasury.reconciliation.view", Name = "Ver conciliaciones", CreatedBy = seedUser },
            new Permission { Id = Guid.Parse("7F000000-0000-0000-0000-000000000024"), Module = "treasury", Resource = "reconciliation", Action = "create", Code = "treasury.reconciliation.create", Name = "Crear conciliaciones", CreatedBy = seedUser },
            new Permission { Id = Guid.Parse("7F000000-0000-0000-0000-000000000025"), Module = "treasury", Resource = "reconciliation", Action = "close", Code = "treasury.reconciliation.close", Name = "Cerrar conciliaciones", CreatedBy = seedUser },
            new Permission { Id = Guid.Parse("7F000000-0000-0000-0000-000000000026"), Module = "treasury", Resource = "dashboard", Action = "view", Code = "treasury.dashboard.view", Name = "Ver dashboard tesorería", CreatedBy = seedUser }
        };

        foreach (var permission in permissions)
        {
            if (await dbContext.Permissions.AnyAsync(x => x.Code == permission.Code))
                continue;

            dbContext.Permissions.Add(permission);
        }

        if (!await dbContext.NavigationItems.AnyAsync(x => x.Code == "treasury"))
        {
            dbContext.NavigationItems.Add(new NavigationItem
            {
                Id = Guid.Parse("9F000000-0000-0000-0000-000000000001"),
                Code = "treasury",
                Module = "treasury",
                Title = "Tesorería",
                Route = "/treasury/dashboard",
                SortOrder = 9,
                RequiredPermission = "treasury.dashboard.view",
                IsVisible = true,
                CreatedBy = seedUser
            });
        }

        await dbContext.SaveChangesAsync();

        var ownerRole = await dbContext.Roles.FirstOrDefaultAsync(x => x.Code == "PLATFORM_OWNER");
        var tenantAdminRole = await dbContext.Roles.FirstOrDefaultAsync(x => x.Code == "TENANT_ADMIN");
        if (ownerRole is null || tenantAdminRole is null)
            return;

        var treasuryPermissions = await dbContext.Permissions.Where(x => x.Module == "treasury").ToListAsync();
        foreach (var permission in treasuryPermissions)
        {
            if (!await dbContext.RolePermissions.AnyAsync(x => x.RoleId == ownerRole.Id && x.PermissionId == permission.Id))
            {
                dbContext.RolePermissions.Add(new RolePermission
                {
                    Id = Guid.NewGuid(),
                    RoleId = ownerRole.Id,
                    PermissionId = permission.Id,
                    AssignedBy = seedUser,
                    CreatedBy = seedUser
                });
            }

            if (!await dbContext.RolePermissions.AnyAsync(x => x.RoleId == tenantAdminRole.Id && x.PermissionId == permission.Id))
            {
                dbContext.RolePermissions.Add(new RolePermission
                {
                    Id = Guid.NewGuid(),
                    RoleId = tenantAdminRole.Id,
                    PermissionId = permission.Id,
                    AssignedBy = seedUser,
                    CreatedBy = seedUser
                });
            }
        }

        await dbContext.SaveChangesAsync();
    }
}
