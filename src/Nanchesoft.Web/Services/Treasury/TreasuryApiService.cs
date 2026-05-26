using System.Net.Http.Json;
using Nanchesoft.Web.Services.Catalogs;

namespace Nanchesoft.Web.Services.Treasury;

public sealed class TreasuryApiService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public TreasuryApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public Task<CatalogViewDefinition> GetCatalogAsync(string catalogKey)
        => catalogKey switch
        {
            "cash-accounts" => GetCashAccountsAsync(),
            "bank-accounts-own" => GetBankAccountsAsync(),
            "treasury-incomes" => GetTreasuryDocumentsAsync(
                catalogKey,
                title: "Ingresos",
                subtitle: "Entradas de dinero que afectan caja o banco y actualizan saldo al postear.",
                keyExpr: "TreasuryIncomeId",
                endpoint: "/api/treasury/incomes",
                columnsBuilder: BuildIncomeColumnsAsync,
                rowsLoader: async client => (await client.GetFromJsonAsync<List<TreasuryDocumentListRow>>("/api/treasury/incomes")) ?? []),
            "treasury-expenses" => GetTreasuryDocumentsAsync(
                catalogKey,
                title: "Egresos",
                subtitle: "Salidas de dinero ligadas a caja o banco con validación de saldo.",
                keyExpr: "TreasuryExpenseId",
                endpoint: "/api/treasury/expenses",
                columnsBuilder: BuildExpenseColumnsAsync,
                rowsLoader: async client => (await client.GetFromJsonAsync<List<TreasuryDocumentListRow>>("/api/treasury/expenses")) ?? []),
            "treasury-receipts" => GetTreasuryDocumentsAsync(
                catalogKey,
                title: "Recibos",
                subtitle: "Cobros ligados a cliente y factura de venta futura.",
                keyExpr: "ReceiptId",
                endpoint: "/api/treasury/receipts",
                columnsBuilder: BuildReceiptColumnsAsync,
                rowsLoader: async client => (await client.GetFromJsonAsync<List<TreasuryDocumentListRow>>("/api/treasury/receipts")) ?? []),
            "treasury-payments" => GetTreasuryDocumentsAsync(
                catalogKey,
                title: "Pagos",
                subtitle: "Pagos a proveedor que afectan caja o banco y dejan trazabilidad.",
                keyExpr: "PaymentId",
                endpoint: "/api/treasury/payments",
                columnsBuilder: BuildPaymentColumnsAsync,
                rowsLoader: async client => (await client.GetFromJsonAsync<List<TreasuryDocumentListRow>>("/api/treasury/payments")) ?? []),
            "treasury-reconciliations" => GetReconciliationsAsync(),
            _ => throw new InvalidOperationException($"No se encontró el catálogo '{catalogKey}'.")
        };

    public async Task<CatalogViewDefinition> InsertAsync(string catalogKey, System.Text.Json.JsonElement payload)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var response = await client.PostAsJsonAsync(GetBaseEndpoint(catalogKey), System.Text.Json.JsonSerializer.Deserialize<object>(payload.GetRawText()));
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(await response.Content.ReadAsStringAsync());
        return await GetCatalogAsync(catalogKey);
    }

    public async Task<CatalogViewDefinition> UpdateAsync(string catalogKey, string key, System.Text.Json.JsonElement payload)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var response = await client.PutAsJsonAsync($"{GetBaseEndpoint(catalogKey)}/{key}", System.Text.Json.JsonSerializer.Deserialize<object>(payload.GetRawText()));
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(await response.Content.ReadAsStringAsync());
        return await GetCatalogAsync(catalogKey);
    }

    public async Task<CatalogViewDefinition> DeleteAsync(string catalogKey, string key)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var response = await client.DeleteAsync($"{GetBaseEndpoint(catalogKey)}/{key}");
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(await response.Content.ReadAsStringAsync());
        return await GetCatalogAsync(catalogKey);
    }

    public async Task<TreasuryDashboardSummaryDto> GetDashboardSummaryAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        return await client.GetFromJsonAsync<TreasuryDashboardSummaryDto>("/api/treasury/dashboard/summary") ?? new TreasuryDashboardSummaryDto();
    }

    public async Task<List<TreasuryBalanceRowDto>> GetDashboardBalancesAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        return await client.GetFromJsonAsync<List<TreasuryBalanceRowDto>>("/api/treasury/dashboard/balances") ?? [];
    }

    public async Task<List<TreasuryRecentRowDto>> GetDashboardRecentAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        return await client.GetFromJsonAsync<List<TreasuryRecentRowDto>>("/api/treasury/dashboard/recent") ?? [];
    }

    public async Task<TreasuryAccountEditorDefinition> GetAccountEditorDefinitionAsync(string catalogKey, Guid? id = null)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var lookups = await client.GetFromJsonAsync<TreasuryLookups>("/api/treasury/lookups") ?? new TreasuryLookups();
        var definition = BuildAccountEditorDefinition(catalogKey, lookups);
        definition.Account = id.HasValue ? await LoadAccountAsync(catalogKey, id.Value) : CreateEmptyAccount(catalogKey);
        return definition;
    }

    public async Task<Guid> SaveAccountAsync(string catalogKey, TreasuryAccountModel account)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        HttpResponseMessage response;

        if (account.Id.HasValue)
        {
            response = await client.PutAsJsonAsync($"{GetBaseEndpoint(catalogKey)}/{account.Id.Value}", BuildAccountRequest(catalogKey, account));
        }
        else
        {
            response = await client.PostAsJsonAsync(GetBaseEndpoint(catalogKey), BuildAccountRequest(catalogKey, account));
        }

        await EnsureSuccessAsync(response);
        return await ReadIdAsync(response, account.Id);
    }

    public async Task SetAccountActiveAsync(string catalogKey, Guid id, bool isActive)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var suffix = isActive ? "activate" : "deactivate";
        var response = await client.PostAsync($"{GetBaseEndpoint(catalogKey)}/{id}/{suffix}", content: null);
        await EnsureSuccessAsync(response);
    }

    public async Task<TreasuryDocumentEditorDefinition> GetDocumentEditorDefinitionAsync(string catalogKey, Guid? id = null)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var lookups = await client.GetFromJsonAsync<TreasuryLookups>("/api/treasury/lookups") ?? new TreasuryLookups();
        var definition = BuildDocumentEditorDefinition(catalogKey, lookups);
        definition.Document = id.HasValue ? await LoadDocumentAsync(catalogKey, id.Value) : CreateEmptyDocument(catalogKey);
        if (definition.Document.Lines.Count == 0)
        {
            definition.Document.Lines.Add(new TreasuryLineModel { LineNumber = 1, Amount = 0m });
        }
        return definition;
    }

    public async Task<Guid> SaveDocumentAsync(string catalogKey, TreasuryDocumentModel document)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        document.Total = document.Lines.Sum(x => x.Amount);
        HttpResponseMessage response;

        if (document.Id.HasValue)
        {
            response = await client.PutAsJsonAsync($"{GetBaseEndpoint(catalogKey)}/{document.Id.Value}", BuildDocumentRequest(catalogKey, document));
        }
        else
        {
            response = await client.PostAsJsonAsync(GetBaseEndpoint(catalogKey), BuildDocumentRequest(catalogKey, document));
        }

        await EnsureSuccessAsync(response);
        return await ReadIdAsync(response, document.Id);
    }

    public async Task ExecuteDocumentActionAsync(string catalogKey, Guid id, string action)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var response = await client.PostAsync($"{GetBaseEndpoint(catalogKey)}/{id}/{action}", content: null);
        await EnsureSuccessAsync(response);
    }

    public async Task<TreasuryReconciliationEditorDefinition> GetReconciliationEditorDefinitionAsync(Guid? id = null)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var lookups = await client.GetFromJsonAsync<TreasuryLookups>("/api/treasury/lookups") ?? new TreasuryLookups();
        var model = id.HasValue
            ? await client.GetFromJsonAsync<ReconciliationRequestDto>($"/api/treasury/reconciliations/{id.Value}") ?? new ReconciliationRequestDto()
            : new ReconciliationRequestDto { ReconciliationDate = DateTime.Today, Status = "in_progress", IsActive = true };

        return new TreasuryReconciliationEditorDefinition
        {
            Title = "Detalle de conciliación",
            Subtitle = "Compara saldo de estado de cuenta contra libros y marca movimientos conciliados.",
            Lookups = lookups,
            Reconciliation = Map(model)
        };
    }

    public async Task<Guid> SaveReconciliationAsync(TreasuryReconciliationModel reconciliation)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        reconciliation.BookBalance = reconciliation.Lines.Where(x => x.IsChecked).Sum(x => x.MovementAmount);
        reconciliation.DifferenceAmount = reconciliation.StatementBalance - reconciliation.BookBalance;

        var response = await client.PostAsJsonAsync("/api/treasury/reconciliations", BuildReconciliationRequest(reconciliation));
        await EnsureSuccessAsync(response);
        return await ReadIdAsync(response, reconciliation.Id);
    }

    public async Task CloseReconciliationAsync(Guid id)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var response = await client.PostAsync($"/api/treasury/reconciliations/{id}/close", content: null);
        await EnsureSuccessAsync(response);
    }

    private async Task<CatalogViewDefinition> GetCashAccountsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<CashAccountListRow>>("/api/treasury/cash-accounts") ?? [];
        var columns = await BuildCashAccountColumnsAsync();

        return BuildView(
            "cash-accounts",
            "Cajas",
            "Control de cajas por sucursal con saldo actual y moneda operativa.",
            "CashAccountId",
            columns,
            rows.Select(x => new Dictionary<string, object?>
            {
                ["CashAccountId"] = x.CashAccountId.ToString(),
                ["CompanyId"] = x.CompanyId?.ToString(),
                ["BranchId"] = x.BranchId?.ToString(),
                ["CurrencyId"] = x.CurrencyId?.ToString(),
                ["Code"] = x.Code,
                ["Name"] = x.Name,
                ["Status"] = x.Status,
                ["CurrentBalance"] = x.CurrentBalance,
                ["IsActive"] = x.IsActive
            }).ToList());
    }

    private async Task<CatalogViewDefinition> GetBankAccountsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<BankAccountListRow>>("/api/treasury/bank-accounts") ?? [];
        var columns = await BuildBankAccountColumnsAsync();

        return BuildView(
            "bank-accounts-own",
            "Bancos propios",
            "Cuentas bancarias de la empresa con banco, CLABE y saldo actual.",
            "BankAccountId",
            columns,
            rows.Select(x => new Dictionary<string, object?>
            {
                ["BankAccountId"] = x.BankAccountId.ToString(),
                ["CompanyId"] = x.CompanyId?.ToString(),
                ["BankId"] = x.BankId?.ToString(),
                ["CurrencyId"] = x.CurrencyId?.ToString(),
                ["Code"] = x.Code,
                ["Name"] = x.Name,
                ["AccountHolder"] = x.AccountHolder,
                ["AccountNumber"] = x.AccountNumber,
                ["Clabe"] = x.Clabe,
                ["Status"] = x.Status,
                ["CurrentBalance"] = x.CurrentBalance,
                ["IsActive"] = x.IsActive
            }).ToList());
    }

    private async Task<CatalogViewDefinition> GetTreasuryDocumentsAsync(
        string catalogKey,
        string title,
        string subtitle,
        string keyExpr,
        string endpoint,
        Func<TreasuryLookups, Task<List<CatalogColumnDefinition>>> columnsBuilder,
        Func<HttpClient, Task<List<TreasuryDocumentListRow>>> rowsLoader)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var lookups = await client.GetFromJsonAsync<TreasuryLookups>("/api/treasury/lookups") ?? new TreasuryLookups();
        var rows = await rowsLoader(client);
        var columns = await columnsBuilder(lookups);

        return BuildView(
            catalogKey,
            title,
            subtitle,
            keyExpr,
            columns,
            rows.Select(x => new Dictionary<string, object?>
            {
                [keyExpr] = x.Id.ToString(),
                ["CompanyId"] = x.CompanyId?.ToString(),
                ["BranchId"] = x.BranchId?.ToString(),
                ["CustomerId"] = x.CustomerId?.ToString(),
                ["SupplierId"] = x.SupplierId?.ToString(),
                ["CashAccountId"] = x.CashAccountId?.ToString(),
                ["BankAccountId"] = x.BankAccountId?.ToString(),
                ["CurrencyId"] = x.CurrencyId?.ToString(),
                ["Folio"] = x.Folio,
                ["DocumentDate"] = x.DocumentDate,
                ["TargetType"] = x.TargetType,
                ["SourceType"] = x.SourceType,
                ["Status"] = x.Status,
                ["Reference"] = x.Reference,
                ["Notes"] = x.Notes,
                ["Total"] = x.Total,
                ["ApprovedAt"] = x.ApprovedAt,
                ["PostedAt"] = x.PostedAt,
                ["IsActive"] = x.IsActive
            }).ToList());
    }

    private async Task<CatalogViewDefinition> GetReconciliationsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var lookups = await client.GetFromJsonAsync<TreasuryLookups>("/api/treasury/lookups") ?? new TreasuryLookups();
        var rows = await client.GetFromJsonAsync<List<ReconciliationListRow>>("/api/treasury/reconciliations") ?? [];
        var columns = await BuildReconciliationColumnsAsync(lookups);

        return BuildView(
            "treasury-reconciliations",
            "Conciliaciones",
            "Comparación entre saldo de estado de cuenta y saldo en libros por banco.",
            "ReconciliationId",
            columns,
            rows.Select(x => new Dictionary<string, object?>
            {
                ["ReconciliationId"] = x.ReconciliationId.ToString(),
                ["CompanyId"] = x.CompanyId?.ToString(),
                ["BankAccountId"] = x.BankAccountId?.ToString(),
                ["ReconciliationDate"] = x.ReconciliationDate,
                ["StatementBalance"] = x.StatementBalance,
                ["BookBalance"] = x.BookBalance,
                ["DifferenceAmount"] = x.DifferenceAmount,
                ["Status"] = x.Status,
                ["ClosedAt"] = x.ClosedAt,
                ["IsActive"] = x.IsActive
            }).ToList());
    }

    private async Task<TreasuryAccountModel> LoadAccountAsync(string catalogKey, Guid id)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        return catalogKey switch
        {
            "cash-accounts" => Map(await client.GetFromJsonAsync<CashAccountRequestDto>($"/api/treasury/cash-accounts/{id}") ?? new CashAccountRequestDto(), catalogKey),
            _ => Map(await client.GetFromJsonAsync<BankAccountRequestDto>($"/api/treasury/bank-accounts/{id}") ?? new BankAccountRequestDto(), catalogKey)
        };
    }

    private async Task<TreasuryDocumentModel> LoadDocumentAsync(string catalogKey, Guid id)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var dto = await client.GetFromJsonAsync<TreasuryDocumentRequestDto>($"{GetBaseEndpoint(catalogKey)}/{id}") ?? new TreasuryDocumentRequestDto();
        return Map(dto, catalogKey);
    }

    private static TreasuryAccountEditorDefinition BuildAccountEditorDefinition(string catalogKey, TreasuryLookups lookups)
        => catalogKey switch
        {
            "cash-accounts" => new TreasuryAccountEditorDefinition
            {
                CatalogKey = catalogKey,
                Title = "Detalle de caja",
                Subtitle = "Configura código, sucursal, moneda y saldo inicial de la caja.",
                IsCashAccount = true,
                Lookups = lookups
            },
            _ => new TreasuryAccountEditorDefinition
            {
                CatalogKey = catalogKey,
                Title = "Detalle de banco propio",
                Subtitle = "Configura banco, cuenta, CLABE, moneda y saldo inicial.",
                IsCashAccount = false,
                Lookups = lookups
            }
        };

    private static TreasuryDocumentEditorDefinition BuildDocumentEditorDefinition(string catalogKey, TreasuryLookups lookups)
        => catalogKey switch
        {
            "treasury-incomes" => new TreasuryDocumentEditorDefinition
            {
                CatalogKey = catalogKey,
                Title = "Detalle de ingreso",
                Subtitle = "Registra entrada de dinero a caja o banco.",
                LinesTitle = "Partidas del ingreso",
                UsesTargetType = true,
                UsesSourceType = false,
                RequiresCustomer = false,
                RequiresSupplier = false,
                Lookups = lookups
            },
            "treasury-expenses" => new TreasuryDocumentEditorDefinition
            {
                CatalogKey = catalogKey,
                Title = "Detalle de egreso",
                Subtitle = "Registra salida de dinero desde caja o banco.",
                LinesTitle = "Partidas del egreso",
                UsesTargetType = false,
                UsesSourceType = true,
                RequiresCustomer = false,
                RequiresSupplier = false,
                Lookups = lookups
            },
            "treasury-receipts" => new TreasuryDocumentEditorDefinition
            {
                CatalogKey = catalogKey,
                Title = "Detalle de recibo",
                Subtitle = "Cobro ligado a cliente y documento comercial cuando aplique.",
                LinesTitle = "Partidas del recibo",
                UsesTargetType = true,
                UsesSourceType = false,
                RequiresCustomer = true,
                RequiresSupplier = false,
                Lookups = lookups
            },
            _ => new TreasuryDocumentEditorDefinition
            {
                CatalogKey = catalogKey,
                Title = "Detalle de pago",
                Subtitle = "Pago ligado a proveedor y documento de compra cuando aplique.",
                LinesTitle = "Partidas del pago",
                UsesTargetType = false,
                UsesSourceType = true,
                RequiresCustomer = false,
                RequiresSupplier = true,
                Lookups = lookups
            }
        };

    private static TreasuryAccountModel CreateEmptyAccount(string catalogKey)
        => new()
        {
            CatalogKey = catalogKey,
            Status = "active",
            IsActive = true,
            CurrentBalance = 0m
        };

    private static TreasuryDocumentModel CreateEmptyDocument(string catalogKey)
        => new()
        {
            CatalogKey = catalogKey,
            DocumentDate = DateTime.Today,
            ExchangeRate = 1m,
            Status = "draft",
            IsActive = true,
            Lines = [new TreasuryLineModel { LineNumber = 1, Amount = 0m }]
        };

    private static object BuildAccountRequest(string catalogKey, TreasuryAccountModel account, bool _ = true)
        => catalogKey switch
        {
            "cash-accounts" => new CashAccountRequestDto
            {
                CashAccountId = account.Id,
                CompanyId = account.CompanyId,
                BranchId = account.BranchId,
                CurrencyId = account.CurrencyId,
                Code = account.Code,
                Name = account.Name,
                Status = account.Status,
                CurrentBalance = account.CurrentBalance,
                IsActive = account.IsActive
            },
            _ => new BankAccountRequestDto
            {
                BankAccountId = account.Id,
                CompanyId = account.CompanyId,
                BankId = account.BankId,
                CurrencyId = account.CurrencyId,
                Code = account.Code,
                Name = account.Name,
                AccountHolder = account.AccountHolder,
                AccountNumber = account.AccountNumber,
                Clabe = account.Clabe,
                Status = account.Status,
                CurrentBalance = account.CurrentBalance,
                IsActive = account.IsActive
            }
        };

    private static TreasuryDocumentRequestDto BuildDocumentRequest(string catalogKey, TreasuryDocumentModel document)
    {
        var dto = new TreasuryDocumentRequestDto
        {
            CompanyId = document.CompanyId,
            BranchId = document.BranchId,
            SeriesId = document.SeriesId,
            CustomerId = document.CustomerId,
            SupplierId = document.SupplierId,
            CurrencyId = document.CurrencyId,
            CashAccountId = document.CashAccountId,
            BankAccountId = document.BankAccountId,
            Folio = document.Folio,
            DocumentDate = document.DocumentDate,
            TargetType = document.TargetType,
            SourceType = document.SourceType,
            ExchangeRate = document.ExchangeRate,
            Status = document.Status,
            Reference = document.Reference,
            Notes = document.Notes,
            Total = document.Total,
            ApprovedAt = document.ApprovedAt,
            PostedAt = document.PostedAt,
            IsActive = document.IsActive,
            Lines = document.Lines.Select(x => new TreasuryLineRequestDto
            {
                Id = x.Id,
                LineNumber = x.LineNumber,
                Description = x.Description,
                Amount = x.Amount,
                CustomerId = x.CustomerId,
                SupplierId = x.SupplierId,
                SalesInvoiceId = x.SalesInvoiceId,
                PurchaseInvoiceId = x.PurchaseInvoiceId
            }).ToList()
        };

        switch (catalogKey)
        {
            case "treasury-incomes": dto.TreasuryIncomeId = document.Id; break;
            case "treasury-expenses": dto.TreasuryExpenseId = document.Id; break;
            case "treasury-receipts": dto.ReceiptId = document.Id; break;
            case "treasury-payments": dto.PaymentId = document.Id; break;
        }

        return dto;
    }

    private static ReconciliationRequestDto BuildReconciliationRequest(TreasuryReconciliationModel model)
        => new()
        {
            ReconciliationId = model.Id,
            CompanyId = model.CompanyId,
            BankAccountId = model.BankAccountId,
            ReconciliationDate = model.ReconciliationDate,
            StatementBalance = model.StatementBalance,
            BookBalance = model.BookBalance,
            DifferenceAmount = model.DifferenceAmount,
            Status = model.Status,
            ClosedAt = model.ClosedAt,
            IsActive = model.IsActive,
            Lines = model.Lines.Select(x => new ReconciliationLineRequestDto
            {
                Id = x.Id,
                BankMovementId = x.BankMovementId,
                IsChecked = x.IsChecked,
                MovementAmount = x.MovementAmount
            }).ToList()
        };

    private static TreasuryAccountModel Map(CashAccountRequestDto dto, string catalogKey)
        => new()
        {
            CatalogKey = catalogKey,
            Id = dto.CashAccountId,
            CompanyId = dto.CompanyId,
            BranchId = dto.BranchId,
            CurrencyId = dto.CurrencyId,
            Code = dto.Code ?? string.Empty,
            Name = dto.Name ?? string.Empty,
            Status = dto.Status ?? "active",
            CurrentBalance = dto.CurrentBalance,
            IsActive = dto.IsActive
        };

    private static TreasuryAccountModel Map(BankAccountRequestDto dto, string catalogKey)
        => new()
        {
            CatalogKey = catalogKey,
            Id = dto.BankAccountId,
            CompanyId = dto.CompanyId,
            BankId = dto.BankId,
            CurrencyId = dto.CurrencyId,
            Code = dto.Code ?? string.Empty,
            Name = dto.Name ?? string.Empty,
            AccountHolder = dto.AccountHolder ?? string.Empty,
            AccountNumber = dto.AccountNumber ?? string.Empty,
            Clabe = dto.Clabe ?? string.Empty,
            Status = dto.Status ?? "active",
            CurrentBalance = dto.CurrentBalance,
            IsActive = dto.IsActive
        };

    private static TreasuryDocumentModel Map(TreasuryDocumentRequestDto dto, string catalogKey)
        => new()
        {
            CatalogKey = catalogKey,
            Id = dto.TreasuryIncomeId ?? dto.TreasuryExpenseId ?? dto.ReceiptId ?? dto.PaymentId,
            CompanyId = dto.CompanyId,
            BranchId = dto.BranchId,
            SeriesId = dto.SeriesId,
            CustomerId = dto.CustomerId,
            SupplierId = dto.SupplierId,
            CurrencyId = dto.CurrencyId,
            CashAccountId = dto.CashAccountId,
            BankAccountId = dto.BankAccountId,
            Folio = dto.Folio ?? string.Empty,
            DocumentDate = dto.DocumentDate,
            TargetType = dto.TargetType ?? "cash",
            SourceType = dto.SourceType ?? "cash",
            ExchangeRate = dto.ExchangeRate <= 0 ? 1m : dto.ExchangeRate,
            Status = dto.Status ?? "draft",
            Reference = dto.Reference ?? string.Empty,
            Notes = dto.Notes ?? string.Empty,
            Total = dto.Total,
            ApprovedAt = dto.ApprovedAt,
            PostedAt = dto.PostedAt,
            IsActive = dto.IsActive,
            Lines = dto.Lines.Select(x => new TreasuryLineModel
            {
                Id = x.Id,
                LineNumber = x.LineNumber,
                Description = x.Description ?? string.Empty,
                Amount = x.Amount,
                CustomerId = x.CustomerId,
                SupplierId = x.SupplierId,
                SalesInvoiceId = x.SalesInvoiceId,
                PurchaseInvoiceId = x.PurchaseInvoiceId
            }).ToList()
        };

    private static TreasuryReconciliationModel Map(ReconciliationRequestDto dto)
        => new()
        {
            Id = dto.ReconciliationId,
            CompanyId = dto.CompanyId,
            BankAccountId = dto.BankAccountId,
            ReconciliationDate = dto.ReconciliationDate,
            StatementBalance = dto.StatementBalance,
            BookBalance = dto.BookBalance,
            DifferenceAmount = dto.DifferenceAmount,
            Status = dto.Status ?? "in_progress",
            ClosedAt = dto.ClosedAt,
            IsActive = dto.IsActive,
            Lines = dto.Lines.Select(x => new TreasuryReconciliationLineModel
            {
                Id = x.Id,
                BankMovementId = x.BankMovementId,
                IsChecked = x.IsChecked,
                MovementAmount = x.MovementAmount
            }).ToList()
        };

    private static CatalogViewDefinition BuildView(string catalogKey, string title, string subtitle, string keyExpr, List<CatalogColumnDefinition> columns, List<Dictionary<string, object?>> rows)
        => new()
        {
            CatalogKey = catalogKey,
            Title = title,
            Subtitle = subtitle,
            KeyExpr = keyExpr,
            AllowCreate = true,
            AllowUpdate = true,
            AllowDelete = false,
            TotalCount = rows.Count,
            ActiveCount = rows.Count(x => IsTrue(x.TryGetValue("IsActive", out var value) ? value : null)),
            InactiveCount = rows.Count(x => !IsTrue(x.TryGetValue("IsActive", out var value) ? value : null)),
            Columns = columns,
            Rows = rows
        };

    private async Task<List<CatalogColumnDefinition>> BuildCashAccountColumnsAsync()
    {
        var lookups = await GetLookupsAsync();
        return
        [
            LookupColumn("CompanyId", "Empresa", lookups.Companies, true, 220),
            LookupColumn("BranchId", "Sucursal", lookups.Branches, true, 220),
            TextColumn("Code", "Código", true, width: 120),
            TextColumn("Name", "Caja", true, width: 220),
            LookupColumn("CurrencyId", "Moneda", lookups.Currencies, true, 140, "currencies"),
            TextColumn("Status", "Estatus", true, width: 120),
            NumberColumn("CurrentBalance", "Saldo actual", width: 140),
            BoolColumn("IsActive", "Activo")
        ];
    }

    private async Task<List<CatalogColumnDefinition>> BuildBankAccountColumnsAsync()
    {
        var lookups = await GetLookupsAsync();
        return
        [
            LookupColumn("CompanyId", "Empresa", lookups.Companies, true, 220),
            LookupColumn("BankId", "Banco", lookups.Banks, true, 220),
            TextColumn("Code", "Código", true, width: 120),
            TextColumn("Name", "Cuenta", true, width: 220),
            TextColumn("AccountHolder", "Titular", width: 180),
            TextColumn("AccountNumber", "Cuenta", width: 160),
            TextColumn("Clabe", "CLABE", width: 180),
            LookupColumn("CurrencyId", "Moneda", lookups.Currencies, true, 140, "currencies"),
            TextColumn("Status", "Estatus", true, width: 120),
            NumberColumn("CurrentBalance", "Saldo actual", width: 140),
            BoolColumn("IsActive", "Activo")
        ];
    }

    private Task<List<CatalogColumnDefinition>> BuildIncomeColumnsAsync(TreasuryLookups lookups)
        => Task.FromResult(BuildDocumentColumns(lookups, includeCustomer: false, includeSupplier: false, typeField: "TargetType"));

    private Task<List<CatalogColumnDefinition>> BuildExpenseColumnsAsync(TreasuryLookups lookups)
        => Task.FromResult(BuildDocumentColumns(lookups, includeCustomer: false, includeSupplier: false, typeField: "SourceType"));

    private Task<List<CatalogColumnDefinition>> BuildReceiptColumnsAsync(TreasuryLookups lookups)
        => Task.FromResult(BuildDocumentColumns(lookups, includeCustomer: true, includeSupplier: false, typeField: "TargetType"));

    private Task<List<CatalogColumnDefinition>> BuildPaymentColumnsAsync(TreasuryLookups lookups)
        => Task.FromResult(BuildDocumentColumns(lookups, includeCustomer: false, includeSupplier: true, typeField: "SourceType"));

    private Task<List<CatalogColumnDefinition>> BuildReconciliationColumnsAsync(TreasuryLookups lookups)
        => Task.FromResult(new List<CatalogColumnDefinition>
        {
            LookupColumn("CompanyId", "Empresa", lookups.Companies, true, 220),
            LookupColumn("BankAccountId", "Cuenta bancaria", lookups.BankAccounts, true, 220),
            DateColumn("ReconciliationDate", "Fecha", true, 130),
            NumberColumn("StatementBalance", "Saldo estado cuenta", width: 160),
            NumberColumn("BookBalance", "Saldo libros", width: 140),
            NumberColumn("DifferenceAmount", "Diferencia", width: 140),
            TextColumn("Status", "Estatus", true, width: 120),
            DateColumn("ClosedAt", "Cierre", width: 140),
            BoolColumn("IsActive", "Activo")
        });

    private static List<CatalogColumnDefinition> BuildDocumentColumns(TreasuryLookups lookups, bool includeCustomer, bool includeSupplier, string typeField)
    {
        var columns = new List<CatalogColumnDefinition>
        {
            LookupColumn("CompanyId", "Empresa", lookups.Companies, true, 220),
            LookupColumn("BranchId", "Sucursal", lookups.Branches, true, 220),
            TextColumn("Folio", "Folio", true, width: 150),
            DateColumn("DocumentDate", "Fecha", true, 130),
            TextColumn(typeField, typeField == "TargetType" ? "Destino" : "Origen", true, width: 120),
            LookupColumn("CashAccountId", "Caja", lookups.CashAccounts, false, 180),
            LookupColumn("BankAccountId", "Banco", lookups.BankAccounts, false, 180),
            LookupColumn("CurrencyId", "Moneda", lookups.Currencies, true, 140, "currencies"),
            TextColumn("Status", "Estatus", true, width: 120),
            TextColumn("Reference", "Referencia", false, width: 180),
            NumberColumn("Total", "Total", width: 140),
            BoolColumn("IsActive", "Activo")
        };

        if (includeCustomer)
        {
            columns.Insert(2, LookupColumn("CustomerId", "Cliente", lookups.Customers, false, 220));
        }

        if (includeSupplier)
        {
            columns.Insert(2, LookupColumn("SupplierId", "Proveedor", lookups.Suppliers, false, 220));
        }

        return columns;
    }

    private async Task<TreasuryLookups> GetLookupsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        return await client.GetFromJsonAsync<TreasuryLookups>("/api/treasury/lookups") ?? new TreasuryLookups();
    }

    private static string GetBaseEndpoint(string catalogKey)
        => catalogKey switch
        {
            "cash-accounts" => "/api/treasury/cash-accounts",
            "bank-accounts-own" => "/api/treasury/bank-accounts",
            "treasury-incomes" => "/api/treasury/incomes",
            "treasury-expenses" => "/api/treasury/expenses",
            "treasury-receipts" => "/api/treasury/receipts",
            "treasury-payments" => "/api/treasury/payments",
            "treasury-reconciliations" => "/api/treasury/reconciliations",
            _ => throw new InvalidOperationException($"No se encontró el endpoint para '{catalogKey}'.")
        };

    private static async Task<Guid> ReadIdAsync(HttpResponseMessage response, Guid? currentId)
    {
        if (currentId.HasValue)
        {
            return currentId.Value;
        }

        var payload = await response.Content.ReadFromJsonAsync<CreatedResponse>();
        return payload?.Id ?? Guid.Empty;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;
        var content = await response.Content.ReadAsStringAsync();
        throw new InvalidOperationException(string.IsNullOrWhiteSpace(content) ? "La API devolvió un error sin detalle." : content);
    }

    private static bool IsTrue(object? value) => value?.ToString()?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;
    private static CatalogColumnDefinition TextColumn(string field, string caption, bool required = false, bool allowEditing = true, int width = 160) => new() { DataField = field, Caption = caption, DataType = "string", Required = required, AllowEditing = allowEditing, Width = width };
    private static CatalogColumnDefinition NumberColumn(string field, string caption, bool required = false, int width = 120) => new() { DataField = field, Caption = caption, DataType = "number", Required = required, Width = width };
    private static CatalogColumnDefinition DateColumn(string field, string caption, bool required = false, int width = 130) => new() { DataField = field, Caption = caption, DataType = "date", Required = required, Width = width };
    private static CatalogColumnDefinition BoolColumn(string field, string caption, int width = 90) => new() { DataField = field, Caption = caption, DataType = "boolean", Width = width };
    private static CatalogColumnDefinition LookupColumn(string field, string caption, List<CatalogLookupItem> lookupItems, bool required = false, int width = 180, string? quickCreateKey = null) => new() { DataField = field, Caption = caption, DataType = "string", Required = required, Width = width, UseLookup = true, LookupItems = lookupItems, QuickCreateKey = quickCreateKey };
}

public sealed class TreasuryAccountEditorDefinition
{
    public string CatalogKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public bool IsCashAccount { get; set; }
    public TreasuryLookups Lookups { get; set; } = new();
    public TreasuryAccountModel Account { get; set; } = new();
}

public sealed class TreasuryAccountModel
{
    public string CatalogKey { get; set; } = string.Empty;
    public Guid? Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? BankId { get; set; }
    public Guid? CurrencyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string AccountHolder { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string Clabe { get; set; } = string.Empty;
    public string Status { get; set; } = "active";
    public decimal CurrentBalance { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class TreasuryDocumentEditorDefinition
{
    public string CatalogKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string LinesTitle { get; set; } = string.Empty;
    public bool UsesTargetType { get; set; }
    public bool UsesSourceType { get; set; }
    public bool RequiresCustomer { get; set; }
    public bool RequiresSupplier { get; set; }
    public TreasuryLookups Lookups { get; set; } = new();
    public TreasuryDocumentModel Document { get; set; } = new();
}

public sealed class TreasuryDocumentModel
{
    public string CatalogKey { get; set; } = string.Empty;
    public Guid? Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? SeriesId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? SupplierId { get; set; }
    public Guid? CurrencyId { get; set; }
    public Guid? CashAccountId { get; set; }
    public Guid? BankAccountId { get; set; }
    public string Folio { get; set; } = string.Empty;
    public DateTime? DocumentDate { get; set; } = DateTime.Today;
    public string TargetType { get; set; } = "cash";
    public string SourceType { get; set; } = "cash";
    public decimal ExchangeRate { get; set; } = 1m;
    public string Status { get; set; } = "draft";
    public string Reference { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? PostedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public List<TreasuryLineModel> Lines { get; set; } = [];
}

public sealed class TreasuryLineModel
{
    public Guid? Id { get; set; }
    public int LineNumber { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? SupplierId { get; set; }
    public Guid? SalesInvoiceId { get; set; }
    public Guid? PurchaseInvoiceId { get; set; }

    public string CustomerIdString
    {
        get => CustomerId?.ToString() ?? string.Empty;
        set => CustomerId = Guid.TryParse(value, out var parsed) ? parsed : null;
    }

    public string SupplierIdString
    {
        get => SupplierId?.ToString() ?? string.Empty;
        set => SupplierId = Guid.TryParse(value, out var parsed) ? parsed : null;
    }

    public string SalesInvoiceIdString
    {
        get => SalesInvoiceId?.ToString() ?? string.Empty;
        set => SalesInvoiceId = Guid.TryParse(value, out var parsed) ? parsed : null;
    }

    public string PurchaseInvoiceIdString
    {
        get => PurchaseInvoiceId?.ToString() ?? string.Empty;
        set => PurchaseInvoiceId = Guid.TryParse(value, out var parsed) ? parsed : null;
    }
}

public sealed class TreasuryReconciliationEditorDefinition
{
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public TreasuryLookups Lookups { get; set; } = new();
    public TreasuryReconciliationModel Reconciliation { get; set; } = new();
}

public sealed class TreasuryReconciliationModel
{
    public Guid? Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BankAccountId { get; set; }
    public DateTime? ReconciliationDate { get; set; } = DateTime.Today;
    public decimal StatementBalance { get; set; }
    public decimal BookBalance { get; set; }
    public decimal DifferenceAmount { get; set; }
    public string Status { get; set; } = "in_progress";
    public DateTime? ClosedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public List<TreasuryReconciliationLineModel> Lines { get; set; } = [];
}

public sealed class TreasuryReconciliationLineModel
{
    public Guid? Id { get; set; }
    public Guid? BankMovementId { get; set; }
    public bool IsChecked { get; set; }
    public decimal MovementAmount { get; set; }
}

public sealed class TreasuryLookups
{
    public List<CatalogLookupItem> Companies { get; set; } = [];
    public List<CatalogLookupItem> Branches { get; set; } = [];
    public List<CatalogLookupItem> Currencies { get; set; } = [];
    public List<CatalogLookupItem> Banks { get; set; } = [];
    public List<CatalogLookupItem> CashAccounts { get; set; } = [];
    public List<CatalogLookupItem> BankAccounts { get; set; } = [];
    public List<CatalogLookupItem> Customers { get; set; } = [];
    public List<CatalogLookupItem> Suppliers { get; set; } = [];
    public List<CatalogLookupItem> SalesInvoices { get; set; } = [];
    public List<CatalogLookupItem> PurchaseInvoices { get; set; } = [];
}

public sealed class TreasuryDashboardSummaryDto
{
    public int CashAccounts { get; set; }
    public int BankAccounts { get; set; }
    public decimal CashBalance { get; set; }
    public decimal BankBalance { get; set; }
    public decimal PeriodInflow { get; set; }
    public decimal PeriodOutflow { get; set; }
    public int PendingReconciliations { get; set; }
}

public sealed class TreasuryBalanceRowDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class TreasuryRecentRowDto
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

public sealed class CashAccountListRow
{
    public Guid CashAccountId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? CurrencyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal CurrentBalance { get; set; }
    public bool IsActive { get; set; }
}

public sealed class BankAccountListRow
{
    public Guid BankAccountId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BankId { get; set; }
    public Guid? CurrencyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string AccountHolder { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string Clabe { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal CurrentBalance { get; set; }
    public bool IsActive { get; set; }
}

public sealed class TreasuryDocumentListRow
{
    public Guid? TreasuryIncomeId { get; set; }
    public Guid? TreasuryExpenseId { get; set; }
    public Guid? ReceiptId { get; set; }
    public Guid? PaymentId { get; set; }
    public Guid Id => TreasuryIncomeId ?? TreasuryExpenseId ?? ReceiptId ?? PaymentId ?? Guid.Empty;
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? SupplierId { get; set; }
    public Guid? CashAccountId { get; set; }
    public Guid? BankAccountId { get; set; }
    public Guid? CurrencyId { get; set; }
    public string Folio { get; set; } = string.Empty;
    public DateTime? DocumentDate { get; set; }
    public string? TargetType { get; set; }
    public string? SourceType { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? PostedAt { get; set; }
    public bool IsActive { get; set; }
}

public sealed class ReconciliationListRow
{
    public Guid ReconciliationId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BankAccountId { get; set; }
    public DateTime? ReconciliationDate { get; set; }
    public decimal StatementBalance { get; set; }
    public decimal BookBalance { get; set; }
    public decimal DifferenceAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? ClosedAt { get; set; }
    public bool IsActive { get; set; }
}

public sealed class CashAccountRequestDto
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

public sealed class BankAccountRequestDto
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

public sealed class TreasuryDocumentRequestDto
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
    public List<TreasuryLineRequestDto> Lines { get; set; } = [];
}

public sealed class TreasuryLineRequestDto
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

public sealed class ReconciliationRequestDto
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
    public List<ReconciliationLineRequestDto> Lines { get; set; } = [];
}

public sealed class ReconciliationLineRequestDto
{
    public Guid? Id { get; set; }
    public Guid? BankMovementId { get; set; }
    public bool IsChecked { get; set; }
    public decimal MovementAmount { get; set; }
}

public sealed class CreatedResponse
{
    public bool Success { get; set; }
    public Guid? Id { get; set; }
}
