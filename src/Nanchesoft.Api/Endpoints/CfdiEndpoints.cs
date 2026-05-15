using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class CfdiEndpoints
{
    private const string Marker = "[[CFDI]]";
    private const string PayrollRunMarker = "[[CFDI_PAYROLL_RUN]]";
    private const string PayrollReceiptMarker = "[[CFDI_PAYROLL_RECEIPT]]";
    private const int SalesInvoiceNotesMaxLength = 500;
    private const int CreditNoteReasonMaxLength = 240;
    private const int PayrollRunNotesMaxLength = 1500;
    private const int PayrollRunLineNotesMaxLength = 1500;

    public static IEndpointRouteBuilder MapCfdiEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/cfdi").WithTags("CFDI");

        group.MapGet("/configuration", () => Results.Ok(new
        {
            mode = "demo",
            provider = "demo-local",
            environment = "sandbox",
            emitter = "Nanchesoft Demo",
            notes = "Implementación demo ligada a ventas y nómina. Timbra y cancela sin PAC real, guardando estado CFDI dentro del documento o recibo de nómina."
        }));

        group.MapGet("/dashboard", async (NanchesoftDbContext db) =>
        {
            var documents = await BuildDocumentsAsync(db);
            return Results.Ok(new
            {
                pending = documents.Count(x => x.CfdiStatus is "pending" or "not-generated"),
                stamped = documents.Count(x => x.CfdiStatus == "stamped"),
                cancelled = documents.Count(x => x.CfdiStatus == "cancelled"),
                failed = documents.Count(x => x.CfdiStatus == "failed"),
                recent = documents.OrderByDescending(x => x.DocumentDate).Take(10).ToList()
            });
        });

        group.MapGet("/documents", async (NanchesoftDbContext db) =>
            Results.Ok((await BuildDocumentsAsync(db)).OrderByDescending(x => x.DocumentDate).ThenByDescending(x => x.Folio).ToList()));

        group.MapGet("/stamp-queue", async (NanchesoftDbContext db) =>
            Results.Ok((await BuildDocumentsAsync(db))
                .Where(x => x.BusinessStatus is "approved" or "posted" or "generated" or "calculated")
                .Where(x => x.CfdiStatus is "pending" or "not-generated")
                .OrderByDescending(x => x.DocumentDate)
                .ToList()));

        group.MapGet("/cancellation", async (NanchesoftDbContext db) =>
            Results.Ok((await BuildDocumentsAsync(db))
                .Where(x => x.CfdiStatus is "stamped" or "cancelled")
                .OrderByDescending(x => x.DocumentDate)
                .ToList()));

        group.MapGet("/sources/sales-invoices", async (NanchesoftDbContext db) =>
        {
            var rows = await db.SalesInvoices
                .AsNoTracking()
                .OrderByDescending(x => x.InvoiceDate)
                .Select(x => new
                {
                    x.Id,
                    x.Folio,
                    x.InvoiceDate,
                    x.Total,
                    x.Status,
                    CustomerName = x.Customer != null ? x.Customer.Name : string.Empty,
                    x.Notes
                })
                .ToListAsync();

            return Results.Ok(rows.Select(x =>
            {
                var meta = ParseMeta(x.Notes);
                return new
                {
                    x.Id,
                    x.Folio,
                    customerName = x.CustomerName,
                    documentDate = x.InvoiceDate,
                    x.Total,
                    businessStatus = x.Status,
                    cfdiStatus = meta.Status,
                    uuid = meta.Uuid,
                    stampedAt = meta.StampedAt,
                    cancelledAt = meta.CancelledAt,
                    editRoute = $"/sales/invoices/detail/{x.Id}"
                };
            }).ToList());
        });

        group.MapGet("/sources/credit-notes", async (NanchesoftDbContext db) =>
        {
            var rows = await db.CreditNotes
                .AsNoTracking()
                .OrderByDescending(x => x.CreditNoteDate)
                .Select(x => new
                {
                    x.Id,
                    x.Folio,
                    DocumentDate = x.CreditNoteDate,
                    x.Total,
                    x.Status,
                    CustomerName = x.Customer != null ? x.Customer.Name : string.Empty,
                    Text = x.Reason
                })
                .ToListAsync();

            return Results.Ok(rows.Select(x =>
            {
                var meta = ParseMeta(x.Text);
                return new
                {
                    x.Id,
                    x.Folio,
                    customerName = x.CustomerName,
                    x.DocumentDate,
                    x.Total,
                    businessStatus = x.Status,
                    cfdiStatus = meta.Status,
                    uuid = meta.Uuid,
                    stampedAt = meta.StampedAt,
                    cancelledAt = meta.CancelledAt,
                    editRoute = $"/sales/credit-notes/detail/{x.Id}"
                };
            }).ToList());
        });

        group.MapGet("/sources/payroll-runs", async (NanchesoftDbContext db) =>
        {
            var runs = await db.PayrollRuns
                .AsNoTracking()
                .OrderByDescending(x => x.RunDate)
                .Select(x => new
                {
                    x.Id,
                    x.Folio,
                    x.RunDate,
                    x.Status,
                    x.EmployeeCount,
                    x.NetAmount,
                    x.Notes,
                    PayrollPeriodName = x.PayrollPeriod != null ? x.PayrollPeriod.Name : string.Empty,
                    PaymentDate = x.PayrollPeriod != null ? x.PayrollPeriod.PaymentDate : (DateTime?)null
                })
                .ToListAsync();

            var runIds = runs.Select(x => x.Id).ToList();
            var receiptNotes = await db.PayrollRunLines
                .AsNoTracking()
                .Where(x => runIds.Contains(x.PayrollRunId))
                .Select(x => new { x.PayrollRunId, x.Notes })
                .ToListAsync();

            var notesByRun = receiptNotes.GroupBy(x => x.PayrollRunId)
                .ToDictionary(x => x.Key, x => x.Select(r => r.Notes).ToList());

            return Results.Ok(runs.Select(x =>
            {
                var aggregate = BuildPayrollAggregate(
                    notesByRun.TryGetValue(x.Id, out var notes) ? notes : new List<string?>(),
                    ParsePayrollRunMeta(x.Notes));

                return new
                {
                    x.Id,
                    x.Folio,
                    documentDate = x.RunDate,
                    businessStatus = x.Status,
                    cfdiStatus = aggregate.Status,
                    uuid = aggregate.PrimaryUuid,
                    stampedAt = aggregate.StampedAt,
                    cancelledAt = aggregate.CancelledAt,
                    payrollPeriodName = x.PayrollPeriodName,
                    paymentDate = x.PaymentDate,
                    employeeCount = x.EmployeeCount,
                    total = x.NetAmount,
                    stampedReceipts = aggregate.StampedReceipts,
                    cancelledReceipts = aggregate.CancelledReceipts,
                    pendingReceipts = aggregate.PendingReceipts,
                    editRoute = $"/payroll/runs",
                    receiptsRoute = $"/cfdi/payroll-receipts/{x.Id}"
                };
            }).ToList());
        });

        group.MapGet("/sources/shipments", async (NanchesoftDbContext db) =>
        {
            var invoiceCounts = await db.SalesInvoices
                .AsNoTracking()
                .Where(x => x.SalesShipmentId != null)
                .GroupBy(x => x.SalesShipmentId)
                .Select(x => new { SalesShipmentId = x.Key!.Value, Count = x.Count() })
                .ToDictionaryAsync(x => x.SalesShipmentId, x => x.Count);

            var rows = await db.SalesShipments
                .AsNoTracking()
                .OrderByDescending(x => x.ShipmentDate)
                .Select(x => new
                {
                    x.Id,
                    x.Folio,
                    x.ShipmentDate,
                    x.Status,
                    CustomerName = x.Customer != null ? x.Customer.Name : string.Empty,
                    LinesCount = x.Lines.Count,
                    TotalQuantity = x.Lines.Sum(l => (decimal?)l.Quantity) ?? 0m
                })
                .ToListAsync();

            return Results.Ok(rows.Select(x => new
            {
                x.Id,
                x.Folio,
                customerName = x.CustomerName,
                x.ShipmentDate,
                businessStatus = x.Status,
                x.LinesCount,
                x.TotalQuantity,
                linkedInvoices = invoiceCounts.TryGetValue(x.Id, out var count) ? count : 0,
                editRoute = $"/sales/shipments/detail/{x.Id}",
                invoiceRoute = $"/sales/invoices/detail?shipmentId={x.Id}"
            }).ToList());
        });

        group.MapGet("/sources/shipments/{id:guid}/invoice-draft", async (Guid id, NanchesoftDbContext db) =>
        {
            var shipment = await db.SalesShipments
                .AsNoTracking()
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (shipment is null)
            {
                return Results.NotFound(new { message = "No se encontró la remisión origen." });
            }

            var order = shipment.SalesOrderId.HasValue
                ? await db.SalesOrders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == shipment.SalesOrderId.Value)
                : null;

            var lineIds = shipment.Lines
                .Where(x => x.SalesOrderLineId.HasValue)
                .Select(x => x.SalesOrderLineId!.Value)
                .Distinct()
                .ToList();

            var orderLines = await db.SalesOrderLines
                .AsNoTracking()
                .Where(x => lineIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id);

            var lines = shipment.Lines
                .OrderBy(x => x.LineNumber)
                .Select(x =>
                {
                    orderLines.TryGetValue(x.SalesOrderLineId ?? Guid.Empty, out var orderLine);
                    var ratio = orderLine is not null && orderLine.Quantity > 0 ? x.Quantity / orderLine.Quantity : 1m;
                    var discount = orderLine is null ? 0m : decimal.Round(orderLine.DiscountAmount * ratio, 2);
                    var tax = orderLine is null ? 0m : decimal.Round(orderLine.TaxAmount * ratio, 2);
                    var unitPrice = orderLine?.UnitPrice ?? 0m;
                    var lineTotal = decimal.Round((x.Quantity * unitPrice) - discount + tax, 2);

                    return new
                    {
                        x.LineNumber,
                        x.SalesOrderLineId,
                        x.ItemId,
                        x.UnitId,
                        TaxId = orderLine?.TaxId,
                        x.Description,
                        x.Quantity,
                        UnitPrice = unitPrice,
                        DiscountAmount = discount,
                        TaxAmount = tax,
                        LineTotal = lineTotal
                    };
                })
                .ToList();

            return Results.Ok(new
            {
                companyId = shipment.CompanyId,
                branchId = shipment.BranchId,
                customerId = shipment.CustomerId,
                currencyId = order?.CurrencyId,
                salesOrderId = shipment.SalesOrderId,
                salesShipmentId = shipment.Id,
                folio = string.Empty,
                documentDate = DateTime.UtcNow.Date,
                status = "draft",
                exchangeRate = order?.ExchangeRate ?? 1m,
                subtotal = lines.Sum(x => x.Quantity * x.UnitPrice),
                discountAmount = lines.Sum(x => x.DiscountAmount),
                taxAmount = lines.Sum(x => x.TaxAmount),
                total = lines.Sum(x => x.LineTotal),
                notes = $"Factura preparada desde remisión {shipment.Folio}.",
                lines
            });
        });

        group.MapGet("/sales-invoices/{id:guid}/status", async (Guid id, NanchesoftDbContext db) =>
        {
            var row = await db.SalesInvoices.AsNoTracking().Where(x => x.Id == id).Select(x => new { x.Id, x.Status, x.Notes }).FirstOrDefaultAsync();
            if (row is null)
            {
                return Results.NotFound(new { message = "No se encontró la factura." });
            }

            var meta = ParseMeta(row.Notes);
            return Results.Ok(new
            {
                row.Id,
                sourceType = "sales-invoice",
                businessStatus = row.Status,
                cfdiStatus = meta.Status,
                uuid = meta.Uuid,
                stampedAt = meta.StampedAt,
                cancelledAt = meta.CancelledAt,
                message = meta.Message ?? BuildDefaultMessage(meta.Status)
            });
        });

        group.MapGet("/credit-notes/{id:guid}/status", async (Guid id, NanchesoftDbContext db) =>
        {
            var row = await db.CreditNotes.AsNoTracking().Where(x => x.Id == id).Select(x => new { x.Id, x.Status, Text = x.Reason }).FirstOrDefaultAsync();
            if (row is null)
            {
                return Results.NotFound(new { message = "No se encontró la nota de crédito." });
            }

            var meta = ParseMeta(row.Text);
            return Results.Ok(new
            {
                row.Id,
                sourceType = "credit-note",
                businessStatus = row.Status,
                cfdiStatus = meta.Status,
                uuid = meta.Uuid,
                stampedAt = meta.StampedAt,
                cancelledAt = meta.CancelledAt,
                message = meta.Message ?? BuildDefaultMessage(meta.Status)
            });
        });

        group.MapGet("/payroll-runs/{id:guid}/status", async (Guid id, NanchesoftDbContext db) =>
        {
            var run = await db.PayrollRuns
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new { x.Id, x.Status, x.Notes })
                .FirstOrDefaultAsync();

            if (run is null)
            {
                return Results.NotFound(new { message = "No se encontró la corrida de nómina." });
            }

            var notes = await db.PayrollRunLines
                .AsNoTracking()
                .Where(x => x.PayrollRunId == id)
                .Select(x => x.Notes)
                .ToListAsync();

            var aggregate = BuildPayrollAggregate(notes, ParsePayrollRunMeta(run.Notes));
            return Results.Ok(new
            {
                run.Id,
                sourceType = "payroll-run",
                businessStatus = run.Status,
                cfdiStatus = aggregate.Status,
                uuid = aggregate.PrimaryUuid,
                stampedAt = aggregate.StampedAt,
                cancelledAt = aggregate.CancelledAt,
                message = aggregate.Message,
                employeeCount = aggregate.TotalReceipts,
                stampedReceipts = aggregate.StampedReceipts,
                cancelledReceipts = aggregate.CancelledReceipts,
                pendingReceipts = aggregate.PendingReceipts
            });
        });

        group.MapGet("/payroll-runs/{id:guid}/receipt-summary", async (Guid id, NanchesoftDbContext db) =>
        {
            var run = await db.PayrollRuns
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new
                {
                    x.Id,
                    x.Folio,
                    x.RunDate,
                    x.Status,
                    x.EmployeeCount,
                    x.GrossAmount,
                    x.DeductionsAmount,
                    x.NetAmount,
                    PayrollPeriodName = x.PayrollPeriod != null ? x.PayrollPeriod.Name : string.Empty,
                    PaymentDate = x.PayrollPeriod != null ? x.PayrollPeriod.PaymentDate : (DateTime?)null,
                    x.Notes
                })
                .FirstOrDefaultAsync();

            if (run is null)
            {
                return Results.NotFound(new { message = "No se encontró la corrida de nómina." });
            }

            var receiptNotes = await db.PayrollRunLines
                .AsNoTracking()
                .Where(x => x.PayrollRunId == id)
                .Select(x => x.Notes)
                .ToListAsync();

            var aggregate = BuildPayrollAggregate(receiptNotes, ParsePayrollRunMeta(run.Notes));
            return Results.Ok(new
            {
                run.Id,
                run.Folio,
                run.RunDate,
                businessStatus = run.Status,
                cfdiStatus = aggregate.Status,
                run.EmployeeCount,
                run.GrossAmount,
                run.DeductionsAmount,
                run.NetAmount,
                run.PayrollPeriodName,
                run.PaymentDate,
                aggregate.TotalReceipts,
                aggregate.StampedReceipts,
                aggregate.CancelledReceipts,
                aggregate.PendingReceipts,
                aggregate.StampedAt,
                aggregate.CancelledAt,
                aggregate.PrimaryUuid,
                aggregate.Message
            });
        });

        group.MapGet("/payroll-runs/{id:guid}/receipts", async (Guid id, NanchesoftDbContext db) =>
        {
            var runExists = await db.PayrollRuns.AsNoTracking().AnyAsync(x => x.Id == id);
            if (!runExists)
            {
                return Results.NotFound(new { message = "No se encontró la corrida de nómina." });
            }

            var rows = await db.PayrollRunLines
                .AsNoTracking()
                .Where(x => x.PayrollRunId == id)
                .OrderBy(x => x.Employee != null ? x.Employee.EmployeeNumber : string.Empty)
                .ThenBy(x => x.Employee != null ? x.Employee.FirstName : string.Empty)
                .Select(x => new
                {
                    x.Id,
                    x.PayrollRunId,
                    x.DaysPaid,
                    x.GrossAmount,
                    x.DeductionsAmount,
                    x.NetAmount,
                    x.IncidentsAmount,
                    x.Notes,
                    EmployeeNumber = x.Employee != null ? x.Employee.EmployeeNumber : string.Empty,
                    EmployeeFirstName = x.Employee != null ? x.Employee.FirstName : string.Empty,
                    EmployeeMiddleName = x.Employee != null ? x.Employee.MiddleName : string.Empty,
                    EmployeeLastName = x.Employee != null ? x.Employee.LastName : string.Empty,
                    DepartmentName = x.Department != null ? x.Department.Name : string.Empty,
                    PositionName = x.Position != null ? x.Position.Name : string.Empty
                })
                .ToListAsync();

            return Results.Ok(rows.Select(x =>
            {
                var meta = ParsePayrollReceiptMeta(x.Notes);
                var employeeName = string.Join(" ", new[] { x.EmployeeFirstName, x.EmployeeMiddleName, x.EmployeeLastName }.Where(s => !string.IsNullOrWhiteSpace(s)));
                return new
                {
                    id = x.Id,
                    x.PayrollRunId,
                    x.EmployeeNumber,
                    employeeName,
                    x.DepartmentName,
                    x.PositionName,
                    x.DaysPaid,
                    x.GrossAmount,
                    x.DeductionsAmount,
                    x.NetAmount,
                    x.IncidentsAmount,
                    cfdiStatus = meta.Status,
                    uuid = meta.Uuid,
                    stampedAt = meta.StampedAt,
                    cancelledAt = meta.CancelledAt,
                    receiptSeries = meta.Series,
                    receiptFolio = meta.Folio,
                    xmlUrl = $"/api/cfdi/payroll-run-lines/{x.Id}/xml",
                    pdfUrl = $"/api/cfdi/payroll-run-lines/{x.Id}/pdf",
                    canStamp = meta.Status is "pending" or "not-generated",
                    canCancel = meta.Status == "stamped"
                };
            }).ToList());
        });

        group.MapGet("/payroll-run-lines/{id:guid}/status", async (Guid id, NanchesoftDbContext db) =>
        {
            var line = await db.PayrollRunLines
                .AsNoTracking()
                .Include(x => x.Employee)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (line is null)
            {
                return Results.NotFound(new { message = "No se encontró el recibo de nómina." });
            }

            var meta = ParsePayrollReceiptMeta(line.Notes);
            return Results.Ok(new
            {
                id = line.Id,
                sourceType = "payroll-receipt",
                employeeNumber = line.Employee?.EmployeeNumber ?? string.Empty,
                employeeName = line.Employee is null
                    ? string.Empty
                    : string.Join(" ", new[] { line.Employee.FirstName, line.Employee.MiddleName, line.Employee.LastName }.Where(s => !string.IsNullOrWhiteSpace(s))),
                cfdiStatus = meta.Status,
                uuid = meta.Uuid,
                stampedAt = meta.StampedAt,
                cancelledAt = meta.CancelledAt,
                receiptSeries = meta.Series,
                receiptFolio = meta.Folio,
                message = meta.Message ?? BuildPayrollReceiptMessage(meta.Status)
            });
        });

        group.MapGet("/payroll-run-lines/{id:guid}/xml", async (Guid id, NanchesoftDbContext db) =>
        {
            var line = await db.PayrollRunLines
                .AsNoTracking()
                .Include(x => x.Employee)
                .Include(x => x.Department)
                .Include(x => x.Position)
                .Include(x => x.PayrollRun)
                    .ThenInclude(x => x.PayrollPeriod)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (line is null)
            {
                return Results.NotFound(new { message = "No se encontró el recibo CFDI de nómina." });
            }

            var meta = ParsePayrollReceiptMeta(line.Notes);
            if (meta.Status is not ("stamped" or "cancelled"))
            {
                return Results.BadRequest("Primero debes timbrar el recibo CFDI demo para poder ver el XML.");
            }

            var xml = BuildPayrollReceiptXml(line, meta);
            var fileName = $"{(meta.Series ?? "NOM")}-{(meta.Folio ?? line.Id.ToString("N"))}.xml";
            return Results.File(Encoding.UTF8.GetBytes(xml), "application/xml", fileName);
        });

        group.MapGet("/payroll-run-lines/{id:guid}/pdf", async (Guid id, NanchesoftDbContext db) =>
        {
            var line = await db.PayrollRunLines
                .AsNoTracking()
                .Include(x => x.Employee)
                .Include(x => x.Department)
                .Include(x => x.Position)
                .Include(x => x.PayrollRun)
                    .ThenInclude(x => x.PayrollPeriod)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (line is null)
            {
                return Results.NotFound(new { message = "No se encontró el recibo CFDI de nómina." });
            }

            var meta = ParsePayrollReceiptMeta(line.Notes);
            if (meta.Status is not ("stamped" or "cancelled"))
            {
                return Results.BadRequest("Primero debes timbrar el recibo CFDI demo para poder ver el PDF demo.");
            }

            var html = BuildPayrollReceiptHtml(line, meta);
            return Results.Content(html, "text/html; charset=utf-8");
        });

        group.MapPost("/sales-invoices/{id:guid}/stamp", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.SalesInvoices.FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null)
            {
                return Results.NotFound(new { message = "No se encontró la factura para timbrar." });
            }

            if (entity.Status is not ("approved" or "posted"))
            {
                return Results.BadRequest("Solo se pueden timbrar facturas aprobadas o posteadas.");
            }

            var meta = ParseMeta(entity.Notes);
            if (meta.Status == "cancelled")
            {
                return Results.BadRequest("La factura ya fue cancelada CFDI y no puede volver a timbrarse en este modo demo.");
            }

            meta.Status = "stamped";
            meta.Uuid ??= Guid.NewGuid().ToString("N").ToUpperInvariant();
            meta.StampedAt ??= DateTime.UtcNow;
            meta.Message = "Timbrado demo generado desde ventas.";

            entity.Notes = MergeWithMeta(entity.Notes, meta, SalesInvoiceNotesMaxLength);
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = "cfdi-demo";
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true, id = entity.Id, cfdiStatus = meta.Status, uuid = meta.Uuid });
        });

        group.MapPost("/sales-invoices/{id:guid}/cancel", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.SalesInvoices.FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null)
            {
                return Results.NotFound(new { message = "No se encontró la factura para cancelar." });
            }

            var meta = ParseMeta(entity.Notes);
            if (meta.Status != "stamped")
            {
                return Results.BadRequest("Solo se pueden cancelar CFDI previamente timbrados.");
            }

            meta.Status = "cancelled";
            meta.CancelledAt = DateTime.UtcNow;
            meta.Message = "Cancelación demo generada desde ventas.";

            entity.Notes = MergeWithMeta(entity.Notes, meta, SalesInvoiceNotesMaxLength);
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = "cfdi-demo";
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true, id = entity.Id, cfdiStatus = meta.Status });
        });

        group.MapPost("/credit-notes/{id:guid}/stamp", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.CreditNotes.FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null)
            {
                return Results.NotFound(new { message = "No se encontró la nota de crédito para timbrar." });
            }

            if (entity.Status is not ("approved" or "posted"))
            {
                return Results.BadRequest("Solo se pueden timbrar notas de crédito aprobadas o posteadas.");
            }

            var meta = ParseMeta(entity.Reason);
            if (meta.Status == "cancelled")
            {
                return Results.BadRequest("La nota de crédito ya fue cancelada CFDI y no puede volver a timbrarse en este modo demo.");
            }

            meta.Status = "stamped";
            meta.Uuid ??= Guid.NewGuid().ToString("N").ToUpperInvariant();
            meta.StampedAt ??= DateTime.UtcNow;
            meta.Message = "Timbrado demo generado desde ventas.";

            entity.Reason = MergeWithMeta(entity.Reason, meta, CreditNoteReasonMaxLength);
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = "cfdi-demo";
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true, id = entity.Id, cfdiStatus = meta.Status, uuid = meta.Uuid });
        });

        group.MapPost("/credit-notes/{id:guid}/cancel", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.CreditNotes.FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null)
            {
                return Results.NotFound(new { message = "No se encontró la nota de crédito para cancelar." });
            }

            var meta = ParseMeta(entity.Reason);
            if (meta.Status != "stamped")
            {
                return Results.BadRequest("Solo se pueden cancelar CFDI previamente timbrados.");
            }

            meta.Status = "cancelled";
            meta.CancelledAt = DateTime.UtcNow;
            meta.Message = "Cancelación demo generada desde ventas.";

            entity.Reason = MergeWithMeta(entity.Reason, meta, CreditNoteReasonMaxLength);
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = "cfdi-demo";
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true, id = entity.Id, cfdiStatus = meta.Status });
        });

        group.MapPost("/payroll-runs/{id:guid}/stamp", async (Guid id, NanchesoftDbContext db) =>
            await GeneratePayrollReceiptsAsync(id, db));

        group.MapPost("/payroll-runs/{id:guid}/cancel", async (Guid id, NanchesoftDbContext db) =>
            await CancelPayrollReceiptsAsync(id, db));

        group.MapPost("/payroll-runs/{id:guid}/generate-receipts", async (Guid id, NanchesoftDbContext db) =>
            await GeneratePayrollReceiptsAsync(id, db));

        group.MapPost("/payroll-runs/{id:guid}/cancel-receipts", async (Guid id, NanchesoftDbContext db) =>
            await CancelPayrollReceiptsAsync(id, db));

        group.MapPost("/payroll-run-lines/{id:guid}/stamp", async (Guid id, NanchesoftDbContext db) =>
        {
            var line = await db.PayrollRunLines
                .Include(x => x.Employee)
                .Include(x => x.Department)
                .Include(x => x.Position)
                .Include(x => x.PayrollRun)
                    .ThenInclude(x => x.PayrollPeriod)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (line is null || line.PayrollRun is null)
            {
                return Results.NotFound(new { message = "No se encontró el recibo CFDI de nómina." });
            }

            var validation = ValidatePayrollRunForStamp(line.PayrollRun.Status);
            if (validation is not null)
            {
                return validation;
            }

            var meta = ParsePayrollReceiptMeta(line.Notes);
            if (meta.Status == "cancelled")
            {
                return Results.BadRequest("El recibo CFDI demo ya fue cancelado y no puede volver a timbrarse en este modo demo.");
            }

            StampPayrollReceipt(line, meta, DateTime.UtcNow);
            await RefreshPayrollRunAggregateAsync(line.PayrollRun, db);
            await db.SaveChangesAsync();

            return Results.Ok(new { success = true, id = line.Id, cfdiStatus = meta.Status, uuid = meta.Uuid, receiptSeries = meta.Series, receiptFolio = meta.Folio });
        });

        group.MapPost("/payroll-run-lines/{id:guid}/cancel", async (Guid id, NanchesoftDbContext db) =>
        {
            var line = await db.PayrollRunLines
                .Include(x => x.PayrollRun)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (line is null || line.PayrollRun is null)
            {
                return Results.NotFound(new { message = "No se encontró el recibo CFDI de nómina." });
            }

            var meta = ParsePayrollReceiptMeta(line.Notes);
            if (meta.Status != "stamped")
            {
                return Results.BadRequest("Solo se pueden cancelar recibos CFDI previamente timbrados.");
            }

            meta.Status = "cancelled";
            meta.CancelledAt = DateTime.UtcNow;
            meta.Message = "Recibo CFDI demo cancelado.";
            line.Notes = MergeWithJsonMarker(line.Notes, PayrollReceiptMarker, meta, PayrollRunLineNotesMaxLength);
            line.UpdatedAt = DateTime.UtcNow;
            line.UpdatedBy = "cfdi-payroll-demo";

            await RefreshPayrollRunAggregateAsync(line.PayrollRun, db);
            await db.SaveChangesAsync();

            return Results.Ok(new { success = true, id = line.Id, cfdiStatus = meta.Status });
        });

        return app;
    }

    private static async Task<IResult> GeneratePayrollReceiptsAsync(Guid runId, NanchesoftDbContext db)
    {
        var run = await db.PayrollRuns
            .Include(x => x.PayrollPeriod)
            .FirstOrDefaultAsync(x => x.Id == runId);

        if (run is null)
        {
            return Results.NotFound(new { message = "No se encontró la corrida de nómina para timbrar." });
        }

        var validation = ValidatePayrollRunForStamp(run.Status);
        if (validation is not null)
        {
            return validation;
        }

        var lines = await db.PayrollRunLines
            .Where(x => x.PayrollRunId == runId)
            .Include(x => x.Employee)
            .Include(x => x.Department)
            .Include(x => x.Position)
            .ToListAsync();

        if (lines.Count == 0)
        {
            return Results.BadRequest("La corrida de nómina no tiene empleados para generar recibos CFDI demo.");
        }

        var now = DateTime.UtcNow;
        foreach (var line in lines)
        {
            var meta = ParsePayrollReceiptMeta(line.Notes);
            if (meta.Status == "cancelled")
            {
                continue;
            }

            StampPayrollReceipt(line, meta, now);
        }

        await RefreshPayrollRunAggregateAsync(run, db);
        await db.SaveChangesAsync();

        var aggregate = BuildPayrollAggregate(lines.Select(x => x.Notes).ToList(), ParsePayrollRunMeta(run.Notes));
        return Results.Ok(new
        {
            success = true,
            id = run.Id,
            cfdiStatus = aggregate.Status,
            stampedReceipts = aggregate.StampedReceipts,
            cancelledReceipts = aggregate.CancelledReceipts,
            pendingReceipts = aggregate.PendingReceipts,
            uuid = aggregate.PrimaryUuid
        });
    }

    private static async Task<IResult> CancelPayrollReceiptsAsync(Guid runId, NanchesoftDbContext db)
    {
        var run = await db.PayrollRuns.FirstOrDefaultAsync(x => x.Id == runId);
        if (run is null)
        {
            return Results.NotFound(new { message = "No se encontró la corrida de nómina para cancelar." });
        }

        var lines = await db.PayrollRunLines
            .Where(x => x.PayrollRunId == runId)
            .ToListAsync();

        var stampedLines = 0;
        foreach (var line in lines)
        {
            var meta = ParsePayrollReceiptMeta(line.Notes);
            if (meta.Status != "stamped")
            {
                continue;
            }

            meta.Status = "cancelled";
            meta.CancelledAt = DateTime.UtcNow;
            meta.Message = "Recibo CFDI demo cancelado.";
            line.Notes = MergeWithJsonMarker(line.Notes, PayrollReceiptMarker, meta, PayrollRunLineNotesMaxLength);
            line.UpdatedAt = DateTime.UtcNow;
            line.UpdatedBy = "cfdi-payroll-demo";
            stampedLines++;
        }

        if (stampedLines == 0)
        {
            return Results.BadRequest("No hay recibos CFDI timbrados para cancelar en esta corrida.");
        }

        await RefreshPayrollRunAggregateAsync(run, db);
        await db.SaveChangesAsync();

        var aggregate = BuildPayrollAggregate(lines.Select(x => x.Notes).ToList(), ParsePayrollRunMeta(run.Notes));
        return Results.Ok(new
        {
            success = true,
            id = run.Id,
            cfdiStatus = aggregate.Status,
            stampedReceipts = aggregate.StampedReceipts,
            cancelledReceipts = aggregate.CancelledReceipts,
            pendingReceipts = aggregate.PendingReceipts
        });
    }

    private static IResult? ValidatePayrollRunForStamp(string? status)
    {
        if (status is not ("approved" or "posted" or "generated" or "calculated"))
        {
            return Results.BadRequest("Solo se pueden timbrar corridas de nómina aprobadas, generadas o posteadas.");
        }

        return null;
    }

    private static void StampPayrollReceipt(dynamic line, PayrollReceiptMeta meta, DateTime now)
    {
        meta.Status = "stamped";
        meta.Uuid ??= Guid.NewGuid().ToString("N").ToUpperInvariant();
        meta.StampedAt ??= now;
        meta.Series ??= "NOM";
        meta.Folio ??= BuildPayrollReceiptFolio(line);
        meta.Message = "Recibo CFDI demo timbrado.";
        line.Notes = MergeWithJsonMarker((string?)line.Notes, PayrollReceiptMarker, meta, PayrollRunLineNotesMaxLength);
        line.UpdatedAt = now;
        line.UpdatedBy = "cfdi-payroll-demo";
    }

    private static string BuildPayrollReceiptFolio(dynamic line)
    {
        var employeeNumber = (string?)line.Employee?.EmployeeNumber;
        if (!string.IsNullOrWhiteSpace(employeeNumber))
        {
            return employeeNumber.Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase).Replace("/", "-", StringComparison.OrdinalIgnoreCase);
        }

        return $"REC-{line.Id.ToString()[..8].ToUpperInvariant()}";
    }

    private static async Task RefreshPayrollRunAggregateAsync(dynamic run, NanchesoftDbContext db)
    {
        Guid runId = (Guid)run.Id;

        var notes = await db.PayrollRunLines
            .Where(x => x.PayrollRunId == runId)
            .Select(x => x.Notes)
            .ToListAsync();

        var aggregate = BuildPayrollAggregate(notes, ParsePayrollRunMeta((string?)run.Notes));
        var meta = new PayrollRunMeta
        {
            Status = aggregate.Status,
            PrimaryUuid = aggregate.PrimaryUuid,
            StampedAt = aggregate.StampedAt,
            CancelledAt = aggregate.CancelledAt,
            Message = aggregate.Message,
            TotalReceipts = aggregate.TotalReceipts,
            StampedReceipts = aggregate.StampedReceipts,
            CancelledReceipts = aggregate.CancelledReceipts,
            PendingReceipts = aggregate.PendingReceipts
        };

        run.Notes = MergeWithJsonMarker((string?)run.Notes, PayrollRunMarker, meta, PayrollRunNotesMaxLength);
        run.UpdatedAt = DateTime.UtcNow;
        run.UpdatedBy = "cfdi-payroll-demo";
    }

    private static PayrollAggregate BuildPayrollAggregate(IEnumerable<string?> notes, PayrollRunMeta runMeta)
    {
        var metas = notes.Select(ParsePayrollReceiptMeta).ToList();
        var total = metas.Count;
        var stamped = metas.Count(x => x.Status == "stamped");
        var cancelled = metas.Count(x => x.Status == "cancelled");
        var pending = metas.Count(x => x.Status is "pending" or "not-generated" or "failed" or "partial");

        if (pending == 0 && total > 0 && stamped == 0 && cancelled == 0)
        {
            pending = total;
        }

        var status = total == 0
            ? runMeta.Status
            : cancelled == total
                ? "cancelled"
                : stamped == total
                    ? "stamped"
                    : stamped > 0
                        ? "partial"
                        : "pending";

        var message = status switch
        {
            "stamped" => "Todos los recibos CFDI demo de la corrida están timbrados.",
            "cancelled" => "Todos los recibos CFDI demo de la corrida están cancelados.",
            "partial" => "La corrida tiene recibos CFDI demo timbrados y pendientes o cancelados.",
            _ => "La corrida aún no tiene recibos CFDI demo timbrados."
        };

        return new PayrollAggregate
        {
            Status = string.IsNullOrWhiteSpace(status) ? "pending" : status,
            TotalReceipts = total > 0 ? total : runMeta.TotalReceipts,
            StampedReceipts = stamped,
            CancelledReceipts = cancelled,
            PendingReceipts = total > 0 ? Math.Max(total - stamped - cancelled, 0) : runMeta.PendingReceipts,
            PrimaryUuid = metas.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Uuid))?.Uuid ?? runMeta.PrimaryUuid,
            StampedAt = metas.Where(x => x.StampedAt.HasValue).Select(x => x.StampedAt).OrderBy(x => x).FirstOrDefault() ?? runMeta.StampedAt,
            CancelledAt = metas.Where(x => x.CancelledAt.HasValue).Select(x => x.CancelledAt).OrderByDescending(x => x).FirstOrDefault() ?? runMeta.CancelledAt,
            Message = message
        };
    }

    private static async Task<List<CfdiDocumentRow>> BuildDocumentsAsync(NanchesoftDbContext db)
    {
        var invoices = await db.SalesInvoices
            .AsNoTracking()
            .OrderByDescending(x => x.InvoiceDate)
            .Select(x => new
            {
                x.Id,
                x.Folio,
                DocumentDate = x.InvoiceDate,
                x.Total,
                BusinessStatus = x.Status,
                CustomerName = x.Customer != null ? x.Customer.Name : string.Empty,
                Text = x.Notes,
                x.SalesShipmentId
            })
            .ToListAsync();

        var creditNotes = await db.CreditNotes
            .AsNoTracking()
            .OrderByDescending(x => x.CreditNoteDate)
            .Select(x => new
            {
                x.Id,
                x.Folio,
                DocumentDate = x.CreditNoteDate,
                x.Total,
                BusinessStatus = x.Status,
                CustomerName = x.Customer != null ? x.Customer.Name : string.Empty,
                Text = x.Reason
            })
            .ToListAsync();

        var rows = invoices.Select(x =>
        {
            var meta = ParseMeta(x.Text);
            return new CfdiDocumentRow
            {
                Id = x.Id,
                SourceType = "sales-invoice",
                Folio = x.Folio,
                CustomerName = x.CustomerName,
                DocumentDate = x.DocumentDate,
                Total = x.Total,
                BusinessStatus = x.BusinessStatus,
                CfdiStatus = meta.Status,
                Uuid = meta.Uuid,
                StampedAt = meta.StampedAt,
                CancelledAt = meta.CancelledAt,
                SourceRoute = $"/sales/invoices/detail/{x.Id}",
                SalesShipmentId = x.SalesShipmentId,
                Notes = StripMeta(x.Text)
            };
        }).ToList();

        rows.AddRange(creditNotes.Select(x =>
        {
            var meta = ParseMeta(x.Text);
            return new CfdiDocumentRow
            {
                Id = x.Id,
                SourceType = "credit-note",
                Folio = x.Folio,
                CustomerName = x.CustomerName,
                DocumentDate = x.DocumentDate,
                Total = x.Total,
                BusinessStatus = x.BusinessStatus,
                CfdiStatus = meta.Status,
                Uuid = meta.Uuid,
                StampedAt = meta.StampedAt,
                CancelledAt = meta.CancelledAt,
                SourceRoute = $"/sales/credit-notes/detail/{x.Id}",
                SalesShipmentId = null,
                Notes = StripMeta(x.Text)
            };
        }));

        return rows;
    }

    private static CfdiMeta ParseMeta(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new CfdiMeta();
        }

        var markerIndex = value.IndexOf(Marker, StringComparison.Ordinal);
        if (markerIndex < 0)
        {
            return new CfdiMeta();
        }

        var payload = value[(markerIndex + Marker.Length)..].Trim();
        if (string.IsNullOrWhiteSpace(payload))
        {
            return new CfdiMeta();
        }

        if (payload.StartsWith('{'))
        {
            try
            {
                var jsonMeta = JsonSerializer.Deserialize<CfdiMeta>(payload) ?? new CfdiMeta();
                jsonMeta.Message ??= BuildDefaultMessage(jsonMeta.Status);
                return jsonMeta;
            }
            catch
            {
                return new CfdiMeta();
            }
        }

        var parts = payload.Split('|');
        if (parts.Length >= 5 && string.Equals(parts[0], "v1", StringComparison.OrdinalIgnoreCase))
        {
            return new CfdiMeta
            {
                Status = string.IsNullOrWhiteSpace(parts[1]) ? "pending" : parts[1],
                Uuid = string.IsNullOrWhiteSpace(parts[2]) ? null : parts[2],
                StampedAt = ParseCompactDate(parts[3]),
                CancelledAt = ParseCompactDate(parts[4]),
                Message = BuildDefaultMessage(string.IsNullOrWhiteSpace(parts[1]) ? "pending" : parts[1])
            };
        }

        return new CfdiMeta();
    }

    private static PayrollRunMeta ParsePayrollRunMeta(string? value)
    {
        var meta = ParseJsonMarker<PayrollRunMeta>(value, PayrollRunMarker);
        if (string.IsNullOrWhiteSpace(meta.Status) || meta.Status == "pending")
        {
            var legacy = ParseMeta(value);
            if (legacy.Status != "pending" || !string.IsNullOrWhiteSpace(legacy.Uuid) || legacy.StampedAt.HasValue || legacy.CancelledAt.HasValue)
            {
                meta.Status = legacy.Status;
                meta.PrimaryUuid ??= legacy.Uuid;
                meta.StampedAt ??= legacy.StampedAt;
                meta.CancelledAt ??= legacy.CancelledAt;
                meta.Message ??= legacy.Message;
            }
        }

        meta.Message ??= BuildPayrollRunMessage(meta.Status);
        return meta;
    }

    private static PayrollReceiptMeta ParsePayrollReceiptMeta(string? value)
    {
        var meta = ParseJsonMarker<PayrollReceiptMeta>(value, PayrollReceiptMarker);
        meta.Message ??= BuildPayrollReceiptMessage(meta.Status);
        return meta;
    }

    private static T ParseJsonMarker<T>(string? value, string marker) where T : new()
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new T();
        }

        var markerIndex = value.IndexOf(marker, StringComparison.Ordinal);
        if (markerIndex < 0)
        {
            return new T();
        }

        var payload = value[(markerIndex + marker.Length)..].Trim();
        if (string.IsNullOrWhiteSpace(payload) || !payload.StartsWith('{'))
        {
            return new T();
        }

        try
        {
            return JsonSerializer.Deserialize<T>(payload) ?? new T();
        }
        catch
        {
            return new T();
        }
    }

    private static string StripMeta(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var markerIndex = value.IndexOf(Marker, StringComparison.Ordinal);
        return markerIndex < 0 ? value.Trim() : value[..markerIndex].Trim();
    }

    private static string StripMarker(string? value, string marker)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var markerIndex = value.IndexOf(marker, StringComparison.Ordinal);
        return markerIndex < 0 ? value.Trim() : value[..markerIndex].Trim();
    }

    private static string MergeWithMeta(string? originalText, CfdiMeta meta, int maxLength)
    {
        var cleanText = StripMeta(originalText);
        var payload = SerializeCompactMeta(meta);
        var suffix = string.IsNullOrWhiteSpace(cleanText)
            ? $"{Marker}{payload}"
            : $"\n{Marker}{payload}";

        var maxTextLength = Math.Max(0, maxLength - suffix.Length);
        if (cleanText.Length > maxTextLength)
        {
            cleanText = cleanText[..maxTextLength].TrimEnd();
        }

        return string.IsNullOrWhiteSpace(cleanText)
            ? $"{Marker}{payload}"
            : $"{cleanText}\n{Marker}{payload}";
    }

    private static string MergeWithJsonMarker<T>(string? originalText, string marker, T meta, int maxLength)
    {
        var cleanText = StripMarker(originalText, marker);
        var payload = JsonSerializer.Serialize(meta);
        var suffix = string.IsNullOrWhiteSpace(cleanText)
            ? $"{marker}{payload}"
            : $"\n{marker}{payload}";

        var maxTextLength = Math.Max(0, maxLength - suffix.Length);
        if (cleanText.Length > maxTextLength)
        {
            cleanText = cleanText[..maxTextLength].TrimEnd();
        }

        return string.IsNullOrWhiteSpace(cleanText)
            ? $"{marker}{payload}"
            : $"{cleanText}\n{marker}{payload}";
    }

    private static string SerializeCompactMeta(CfdiMeta meta)
    {
        var uuid = meta.Uuid?.Replace("-", string.Empty, StringComparison.Ordinal).ToUpperInvariant() ?? string.Empty;
        return string.Join("|",
            "v1",
            meta.Status,
            uuid,
            FormatCompactDate(meta.StampedAt),
            FormatCompactDate(meta.CancelledAt));
    }

    private static string FormatCompactDate(DateTime? value)
        => value.HasValue ? value.Value.ToUniversalTime().ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture) : string.Empty;

    private static DateTime? ParseCompactDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateTime.TryParseExact(
                value,
                "yyyyMMddHHmmss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private static string BuildDefaultMessage(string? status) => status switch
    {
        "stamped" => "Timbrado demo generado desde ventas.",
        "cancelled" => "Cancelación demo generada desde ventas.",
        "failed" => "El CFDI demo quedó con error.",
        _ => "CFDI demo pendiente de timbrado."
    };

    private static string BuildPayrollRunMessage(string? status) => status switch
    {
        "stamped" => "Todos los recibos CFDI demo de la corrida están timbrados.",
        "cancelled" => "Todos los recibos CFDI demo de la corrida están cancelados.",
        "partial" => "La corrida tiene recibos CFDI demo timbrados y pendientes o cancelados.",
        _ => "La corrida aún no tiene recibos CFDI demo timbrados."
    };

    private static string BuildPayrollReceiptMessage(string? status) => status switch
    {
        "stamped" => "Recibo CFDI demo timbrado.",
        "cancelled" => "Recibo CFDI demo cancelado.",
        _ => "Recibo CFDI demo pendiente de timbrado."
    };

    private static string BuildPayrollReceiptXml(dynamic line, PayrollReceiptMeta meta)
    {
        var employeeName = line.Employee is null
            ? "Empleado demo"
            : string.Join(" ", new[] { line.Employee.FirstName, line.Employee.MiddleName, line.Employee.LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));
        var employeeNumber = line.Employee?.EmployeeNumber ?? "EMP-DEMO";
        var taxId = line.Employee?.TaxId ?? "XAXX010101000";
        var nationalId = line.Employee?.NationalId ?? "XEXX010101HNEXXXA4";
        var period = line.PayrollRun?.PayrollPeriod;
        var paymentDate = (period?.PaymentDate ?? line.PayrollRun?.RunDate ?? DateTime.UtcNow).ToUniversalTime();
        var startDate = (period?.StartDate ?? line.PayrollRun?.RunDate ?? DateTime.UtcNow).ToUniversalTime();
        var endDate = (period?.EndDate ?? line.PayrollRun?.RunDate ?? DateTime.UtcNow).ToUniversalTime();

        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine($"<cfdi:Comprobante xmlns:cfdi=\"http://www.sat.gob.mx/cfd/4\" xmlns:nomina12=\"http://www.sat.gob.mx/nomina12\" Version=\"4.0\" Serie=\"{Xml(meta.Series ?? "NOM")}\" Folio=\"{Xml(meta.Folio ?? employeeNumber)}\" Fecha=\"{Xml((meta.StampedAt ?? DateTime.UtcNow).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture))}\" SubTotal=\"{line.GrossAmount.ToString("0.00", CultureInfo.InvariantCulture)}\" Descuento=\"{line.DeductionsAmount.ToString("0.00", CultureInfo.InvariantCulture)}\" Total=\"{line.NetAmount.ToString("0.00", CultureInfo.InvariantCulture)}\" Moneda=\"MXN\" TipoDeComprobante=\"N\" Exportacion=\"01\">");
        sb.AppendLine($"  <cfdi:Emisor Rfc=\"XAXX010101000\" Nombre=\"Nanchesoft Demo\" RegimenFiscal=\"601\" />");
        sb.AppendLine($"  <cfdi:Receptor Rfc=\"{Xml(taxId)}\" Nombre=\"{Xml(employeeName)}\" DomicilioFiscalReceptor=\"64000\" RegimenFiscalReceptor=\"605\" UsoCFDI=\"CN01\" />");
        sb.AppendLine("  <cfdi:Conceptos>");
        sb.AppendLine($"    <cfdi:Concepto ClaveProdServ=\"84111505\" Cantidad=\"1\" ClaveUnidad=\"ACT\" Descripcion=\"Recibo de nómina {Xml(line.PayrollRun?.Folio ?? string.Empty)}\" ValorUnitario=\"{line.NetAmount.ToString("0.00", CultureInfo.InvariantCulture)}\" Importe=\"{line.NetAmount.ToString("0.00", CultureInfo.InvariantCulture)}\" Descuento=\"0.00\" ObjetoImp=\"01\" />");
        sb.AppendLine("  </cfdi:Conceptos>");
        sb.AppendLine("  <cfdi:Complemento>");
        sb.AppendLine($"    <nomina12:Nomina Version=\"1.2\" TipoNomina=\"O\" FechaPago=\"{paymentDate:yyyy-MM-dd}\" FechaInicialPago=\"{startDate:yyyy-MM-dd}\" FechaFinalPago=\"{endDate:yyyy-MM-dd}\" NumDiasPagados=\"{line.DaysPaid.ToString("0.##", CultureInfo.InvariantCulture)}\" TotalPercepciones=\"{line.GrossAmount.ToString("0.00", CultureInfo.InvariantCulture)}\" TotalDeducciones=\"{line.DeductionsAmount.ToString("0.00", CultureInfo.InvariantCulture)}\">");
        sb.AppendLine("      <nomina12:Emisor RegistroPatronal=\"A1234567890\" />");
        sb.AppendLine($"      <nomina12:Receptor Curp=\"{Xml(nationalId)}\" NumEmpleado=\"{Xml(employeeNumber)}\" Departamento=\"{Xml(line.Department?.Name ?? string.Empty)}\" Puesto=\"{Xml(line.Position?.Name ?? string.Empty)}\" TipoContrato=\"01\" TipoRegimen=\"02\" NumSeguridadSocial=\"00000000000\" FechaInicioRelLaboral=\"{(line.PayrollRun?.RunDate ?? DateTime.UtcNow):yyyy-MM-dd}\" Antigüedad=\"P1Y\" TipoJornada=\"01\" PeriodicidadPago=\"04\" SalarioBaseCotApor=\"{line.NetAmount.ToString("0.00", CultureInfo.InvariantCulture)}\" SalarioDiarioIntegrado=\"{line.NetAmount.ToString("0.00", CultureInfo.InvariantCulture)}\" ClaveEntFed=\"NL\" />");
        sb.AppendLine("    </nomina12:Nomina>");
        sb.AppendLine("  </cfdi:Complemento>");
        sb.AppendLine("</cfdi:Comprobante>");
        return sb.ToString();
    }

    private static string BuildPayrollReceiptHtml(dynamic line, PayrollReceiptMeta meta)
    {
        var employeeName = line.Employee is null
            ? "Empleado demo"
            : string.Join(" ", new[] { line.Employee.FirstName, line.Employee.MiddleName, line.Employee.LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));

        var sb = new StringBuilder();
        sb.AppendLine("<!doctype html>");
        sb.AppendLine("<html lang='es'>");
        sb.AppendLine("<head>");
        sb.AppendLine("    <meta charset='utf-8' />");
        sb.AppendLine("    <title>Recibo CFDI nómina demo</title>");
        sb.AppendLine("    <style>");
        sb.AppendLine("        body { font-family: Arial, sans-serif; margin: 24px; color: #1f2937; }");
        sb.AppendLine("        .header { display:flex; justify-content:space-between; align-items:flex-start; margin-bottom: 24px; }");
        sb.AppendLine("        .card { border:1px solid #d1d5db; border-radius: 12px; padding: 16px; margin-bottom: 16px; }");
        sb.AppendLine("        .grid { display:grid; grid-template-columns: repeat(2,minmax(0,1fr)); gap: 12px; }");
        sb.AppendLine("        .label { color:#6b7280; font-size:12px; text-transform:uppercase; }");
        sb.AppendLine("        .value { font-size:16px; font-weight:600; }");
        sb.AppendLine("        table { width:100%; border-collapse: collapse; margin-top: 12px; }");
        sb.AppendLine("        th, td { border-bottom:1px solid #e5e7eb; padding: 10px 8px; text-align:left; }");
        sb.AppendLine("        th { color:#6b7280; font-size:12px; text-transform:uppercase; }");
        sb.AppendLine("    </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("    <div class='header'>");
        sb.AppendLine("        <div>");
        sb.AppendLine("            <div class='label'>CFDI nómina demo</div>");
        sb.AppendLine("            <h1 style='margin: 6px 0 0 0;'>Recibo de nómina</h1>");
        sb.AppendLine($"            <div style='margin-top:8px; color:#4b5563;'>{Html(employeeName)} · {Html(line.Employee?.EmployeeNumber ?? string.Empty)}</div>");
        sb.AppendLine("        </div>");
        sb.AppendLine("        <div class='card' style='min-width:260px;'>");
        sb.AppendLine("            <div class='label'>Serie y folio</div>");
        sb.AppendLine($"            <div class='value'>{Html(meta.Series ?? "NOM")}-{Html(meta.Folio ?? string.Empty)}</div>");
        sb.AppendLine("            <div class='label' style='margin-top:8px;'>UUID</div>");
        sb.AppendLine($"            <div class='value' style='font-size:13px; word-break:break-all;'>{Html(meta.Uuid ?? string.Empty)}</div>");
        sb.AppendLine("        </div>");
        sb.AppendLine("    </div>");
        sb.AppendLine();
        sb.AppendLine("    <div class='card'>");
        sb.AppendLine("        <div class='grid'>");
        sb.AppendLine($"            <div><div class='label'>Departamento</div><div class='value'>{Html(line.Department?.Name ?? string.Empty)}</div></div>");
        sb.AppendLine($"            <div><div class='label'>Puesto</div><div class='value'>{Html(line.Position?.Name ?? string.Empty)}</div></div>");
        sb.AppendLine($"            <div><div class='label'>Corrida</div><div class='value'>{Html(line.PayrollRun?.Folio ?? string.Empty)}</div></div>");
        sb.AppendLine($"            <div><div class='label'>Estado CFDI</div><div class='value'>{Html(meta.Status)}</div></div>");
        sb.AppendLine("        </div>");
        sb.AppendLine("    </div>");
        sb.AppendLine();
        sb.AppendLine("    <div class='card'>");
        sb.AppendLine("        <table>");
        sb.AppendLine("            <thead>");
        sb.AppendLine("                <tr>");
        sb.AppendLine("                    <th>Concepto</th>");
        sb.AppendLine("                    <th>Importe</th>");
        sb.AppendLine("                </tr>");
        sb.AppendLine("            </thead>");
        sb.AppendLine("            <tbody>");
        sb.AppendLine($"                <tr><td>Percepciones</td><td>{line.GrossAmount:N2}</td></tr>");
        sb.AppendLine($"                <tr><td>Deducciones</td><td>{line.DeductionsAmount:N2}</td></tr>");
        sb.AppendLine($"                <tr><td>Incidencias</td><td>{line.IncidentsAmount:N2}</td></tr>");
        sb.AppendLine($"                <tr><td><strong>Neto</strong></td><td><strong>{line.NetAmount:N2}</strong></td></tr>");
        sb.AppendLine("            </tbody>");
        sb.AppendLine("        </table>");
        sb.AppendLine("    </div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        return sb.ToString();
    }

    private static string Xml(string? value) => System.Security.SecurityElement.Escape(value ?? string.Empty) ?? string.Empty;
    private static string Html(string? value) => (value ?? string.Empty)
        .Replace("&", "&amp;", StringComparison.Ordinal)
        .Replace("<", "&lt;", StringComparison.Ordinal)
        .Replace(">", "&gt;", StringComparison.Ordinal)
        .Replace("\"", "&quot;", StringComparison.Ordinal);

    private sealed class CfdiMeta
    {
        public string Status { get; set; } = "pending";
        public string? Uuid { get; set; }
        public DateTime? StampedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string? Message { get; set; }
    }

    private sealed class PayrollRunMeta
    {
        public string Status { get; set; } = "pending";
        public string? PrimaryUuid { get; set; }
        public DateTime? StampedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string? Message { get; set; }
        public int TotalReceipts { get; set; }
        public int StampedReceipts { get; set; }
        public int CancelledReceipts { get; set; }
        public int PendingReceipts { get; set; }
    }

    private sealed class PayrollReceiptMeta
    {
        public string Status { get; set; } = "pending";
        public string? Uuid { get; set; }
        public DateTime? StampedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string? Series { get; set; }
        public string? Folio { get; set; }
        public string? Message { get; set; }
    }

    private sealed class PayrollAggregate
    {
        public string Status { get; set; } = "pending";
        public int TotalReceipts { get; set; }
        public int StampedReceipts { get; set; }
        public int CancelledReceipts { get; set; }
        public int PendingReceipts { get; set; }
        public string? PrimaryUuid { get; set; }
        public DateTime? StampedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    private sealed class CfdiDocumentRow
    {
        public Guid Id { get; set; }
        public string SourceType { get; set; } = string.Empty;
        public string Folio { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public DateTime DocumentDate { get; set; }
        public decimal Total { get; set; }
        public string BusinessStatus { get; set; } = string.Empty;
        public string CfdiStatus { get; set; } = string.Empty;
        public string? Uuid { get; set; }
        public DateTime? StampedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string SourceRoute { get; set; } = string.Empty;
        public Guid? SalesShipmentId { get; set; }
        public string Notes { get; set; } = string.Empty;
    }
}
