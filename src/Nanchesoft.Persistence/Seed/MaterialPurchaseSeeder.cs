using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Persistence.Seed;

public static class MaterialPurchaseSeeder
{
    public static async Task EnsureAsync(NanchesoftDbContext db)
    {
        await ApplySchemaAlterationsAsync(db);
        await EnsureSeriesAsync(db);
    }

    // Idempotent schema evolution: CREATE new tables, ALTER existing ones
    private static async Task ApplySchemaAlterationsAsync(NanchesoftDbContext db)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync();

        // Step 1: Ensure schemas exist
        var schemaSql = @"
CREATE SCHEMA IF NOT EXISTS purchase;
CREATE SCHEMA IF NOT EXISTS inventory;
CREATE SCHEMA IF NOT EXISTS org;
";
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = schemaSql;
            await cmd.ExecuteNonQueryAsync();
        }

        // Step 2: Create new tables (idempotent)
        var createTablesSql = @"
CREATE TABLE IF NOT EXISTS purchase.purchase_payments (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NOT NULL,
    company_id uuid NOT NULL,
    branch_id uuid NOT NULL,
    purchase_receipt_id uuid NOT NULL,
    supplier_id uuid NOT NULL,
    bank_account_id uuid NULL,
    payment_date timestamp with time zone NOT NULL,
    folio text NOT NULL DEFAULT '',
    payment_method text NOT NULL DEFAULT 'transfer',
    amount numeric(18,2) NOT NULL DEFAULT 0,
    reference text NOT NULL DEFAULT '',
    notes text NOT NULL DEFAULT '',
    status text NOT NULL DEFAULT 'posted',
    cancelled_at timestamp with time zone NULL,
    cancelled_by text NULL,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    created_by text NOT NULL DEFAULT '',
    updated_at timestamp with time zone NULL,
    updated_by text NULL
);

CREATE TABLE IF NOT EXISTS purchase.purchase_receipt_diffs (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NOT NULL,
    company_id uuid NOT NULL,
    purchase_receipt_id uuid NOT NULL,
    purchase_order_id uuid NULL,
    authorized_at timestamp with time zone NOT NULL DEFAULT now(),
    authorized_by text NOT NULL DEFAULT '',
    notes text NOT NULL DEFAULT '',
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    created_by text NOT NULL DEFAULT ''
);

CREATE TABLE IF NOT EXISTS purchase.purchase_receipt_diff_lines (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    purchase_receipt_diff_id uuid NOT NULL,
    line_number int NOT NULL DEFAULT 0,
    material_item_id uuid NULL,
    material_code text NOT NULL DEFAULT '',
    material_name text NOT NULL DEFAULT '',
    diff_type text NOT NULL DEFAULT 'quantity_diff',
    ordered_quantity numeric(18,4) NOT NULL DEFAULT 0,
    received_quantity numeric(18,4) NOT NULL DEFAULT 0,
    ordered_unit_price numeric(18,4) NOT NULL DEFAULT 0,
    received_unit_price numeric(18,4) NOT NULL DEFAULT 0,
    notes text NOT NULL DEFAULT ''
);

CREATE TABLE IF NOT EXISTS inventory.material_stock_balances (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NOT NULL,
    company_id uuid NOT NULL,
    warehouse_id uuid NOT NULL,
    material_item_id uuid NOT NULL,
    quantity_on_hand numeric(18,4) NOT NULL DEFAULT 0,
    quantity_reserved numeric(18,4) NOT NULL DEFAULT 0,
    quantity_available numeric(18,4) NOT NULL DEFAULT 0,
    average_cost numeric(18,4) NOT NULL DEFAULT 0,
    last_cost numeric(18,4) NOT NULL DEFAULT 0,
    last_movement_at timestamp with time zone NOT NULL DEFAULT now(),
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    created_by text NOT NULL DEFAULT '',
    updated_at timestamp with time zone NULL,
    updated_by text NULL,
    UNIQUE(company_id, warehouse_id, material_item_id)
);

CREATE TABLE IF NOT EXISTS inventory.material_inventory_movements (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NOT NULL,
    company_id uuid NOT NULL,
    warehouse_id uuid NOT NULL,
    material_item_id uuid NOT NULL,
    supplier_id uuid NULL,
    movement_type text NOT NULL DEFAULT 'entry',
    document_type text NOT NULL DEFAULT 'review_receipt',
    document_id uuid NULL,
    document_folio text NOT NULL DEFAULT '',
    movement_date timestamp with time zone NOT NULL DEFAULT now(),
    quantity_in numeric(18,4) NOT NULL DEFAULT 0,
    quantity_out numeric(18,4) NOT NULL DEFAULT 0,
    balance_after numeric(18,4) NOT NULL DEFAULT 0,
    unit_cost numeric(18,4) NOT NULL DEFAULT 0,
    total_cost numeric(18,2) NOT NULL DEFAULT 0,
    notes text NOT NULL DEFAULT '',
    user_name text NOT NULL DEFAULT '',
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    created_by text NOT NULL DEFAULT ''
);
";
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = createTablesSql;
            await cmd.ExecuteNonQueryAsync();
        }

        // Step 3: ALTER existing tables (add columns to tables that already existed)
        var alterSql = @"
-- purchase.purchase_orders new columns
ALTER TABLE purchase.purchase_orders ADD COLUMN IF NOT EXISTS order_type text NOT NULL DEFAULT 'materials';
ALTER TABLE purchase.purchase_orders ADD COLUMN IF NOT EXISTS supplier_delivery_date timestamp with time zone NULL;
ALTER TABLE purchase.purchase_orders ADD COLUMN IF NOT EXISTS buyer_name text NOT NULL DEFAULT '';
ALTER TABLE purchase.purchase_orders ADD COLUMN IF NOT EXISTS warehouse_id uuid NULL;
ALTER TABLE purchase.purchase_orders ADD COLUMN IF NOT EXISTS received_total numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE purchase.purchase_orders ADD COLUMN IF NOT EXISTS approved_by text NULL;
ALTER TABLE purchase.purchase_orders ADD COLUMN IF NOT EXISTS closed_by text NULL;
ALTER TABLE purchase.purchase_orders ADD COLUMN IF NOT EXISTS cancelled_at timestamp with time zone NULL;
ALTER TABLE purchase.purchase_orders ADD COLUMN IF NOT EXISTS cancelled_by text NULL;

-- purchase.purchase_order_lines new columns
ALTER TABLE purchase.purchase_order_lines ADD COLUMN IF NOT EXISTS material_item_id uuid NULL;
ALTER TABLE purchase.purchase_order_lines ADD COLUMN IF NOT EXISTS notes text NOT NULL DEFAULT '';

-- purchase.purchase_receipts new columns
ALTER TABLE purchase.purchase_receipts ADD COLUMN IF NOT EXISTS receipt_type text NOT NULL DEFAULT 'review';
ALTER TABLE purchase.purchase_receipts ADD COLUMN IF NOT EXISTS supplier_document_number text NOT NULL DEFAULT '';
ALTER TABLE purchase.purchase_receipts ADD COLUMN IF NOT EXISTS supplier_document_date timestamp with time zone NULL;
ALTER TABLE purchase.purchase_receipts ADD COLUMN IF NOT EXISTS subtotal numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE purchase.purchase_receipts ADD COLUMN IF NOT EXISTS tax_amount numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE purchase.purchase_receipts ADD COLUMN IF NOT EXISTS total numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE purchase.purchase_receipts ADD COLUMN IF NOT EXISTS reviewed_at timestamp with time zone NULL;
ALTER TABLE purchase.purchase_receipts ADD COLUMN IF NOT EXISTS reviewed_by text NULL;
ALTER TABLE purchase.purchase_receipts ADD COLUMN IF NOT EXISTS authorized_at timestamp with time zone NULL;
ALTER TABLE purchase.purchase_receipts ADD COLUMN IF NOT EXISTS authorized_by text NULL;
ALTER TABLE purchase.purchase_receipts ADD COLUMN IF NOT EXISTS rejected_at timestamp with time zone NULL;
ALTER TABLE purchase.purchase_receipts ADD COLUMN IF NOT EXISTS rejected_by text NULL;
ALTER TABLE purchase.purchase_receipts ADD COLUMN IF NOT EXISTS rejection_reason text NULL;
ALTER TABLE purchase.purchase_receipts ADD COLUMN IF NOT EXISTS has_differences boolean NOT NULL DEFAULT false;
ALTER TABLE purchase.purchase_receipts ADD COLUMN IF NOT EXISTS differences_authorized boolean NOT NULL DEFAULT false;
ALTER TABLE purchase.purchase_receipts ADD COLUMN IF NOT EXISTS differences_authorized_at timestamp with time zone NULL;
ALTER TABLE purchase.purchase_receipts ADD COLUMN IF NOT EXISTS differences_authorized_by text NULL;
ALTER TABLE purchase.purchase_receipts ADD COLUMN IF NOT EXISTS payment_status text NOT NULL DEFAULT 'pending';
ALTER TABLE purchase.purchase_receipts ADD COLUMN IF NOT EXISTS paid_amount numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE purchase.purchase_receipts ADD COLUMN IF NOT EXISTS converted_to_invoice_id uuid NULL;
ALTER TABLE purchase.purchase_receipts ADD COLUMN IF NOT EXISTS converted_at timestamp with time zone NULL;
ALTER TABLE purchase.purchase_receipts ADD COLUMN IF NOT EXISTS converted_by text NULL;

-- purchase.purchase_receipt_lines new columns
ALTER TABLE purchase.purchase_receipt_lines ADD COLUMN IF NOT EXISTS material_item_id uuid NULL;
ALTER TABLE purchase.purchase_receipt_lines ADD COLUMN IF NOT EXISTS tax_id uuid NULL;
ALTER TABLE purchase.purchase_receipt_lines ADD COLUMN IF NOT EXISTS unit_price numeric(18,4) NOT NULL DEFAULT 0;
ALTER TABLE purchase.purchase_receipt_lines ADD COLUMN IF NOT EXISTS discount_amount numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE purchase.purchase_receipt_lines ADD COLUMN IF NOT EXISTS tax_amount numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE purchase.purchase_receipt_lines ADD COLUMN IF NOT EXISTS line_total numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE purchase.purchase_receipt_lines ADD COLUMN IF NOT EXISTS ordered_quantity numeric(18,4) NOT NULL DEFAULT 0;
ALTER TABLE purchase.purchase_receipt_lines ADD COLUMN IF NOT EXISTS ordered_unit_price numeric(18,4) NOT NULL DEFAULT 0;
ALTER TABLE purchase.purchase_receipt_lines ADD COLUMN IF NOT EXISTS notes text NOT NULL DEFAULT '';

-- org.suppliers new columns
ALTER TABLE org.suppliers ADD COLUMN IF NOT EXISTS short_name text NOT NULL DEFAULT '';
ALTER TABLE org.suppliers ADD COLUMN IF NOT EXISTS classification text NOT NULL DEFAULT '';
ALTER TABLE org.suppliers ADD COLUMN IF NOT EXISTS fiscal_regime text NOT NULL DEFAULT '';
ALTER TABLE org.suppliers ADD COLUMN IF NOT EXISTS cfdi_use text NOT NULL DEFAULT '';
ALTER TABLE org.suppliers ADD COLUMN IF NOT EXISTS address text NOT NULL DEFAULT '';
ALTER TABLE org.suppliers ADD COLUMN IF NOT EXISTS postal_code text NOT NULL DEFAULT '';
ALTER TABLE org.suppliers ADD COLUMN IF NOT EXISTS colony text NOT NULL DEFAULT '';
ALTER TABLE org.suppliers ADD COLUMN IF NOT EXISTS city text NOT NULL DEFAULT '';
ALTER TABLE org.suppliers ADD COLUMN IF NOT EXISTS state text NOT NULL DEFAULT '';
ALTER TABLE org.suppliers ADD COLUMN IF NOT EXISTS country text NOT NULL DEFAULT 'México';
ALTER TABLE org.suppliers ADD COLUMN IF NOT EXISTS phone2 text NOT NULL DEFAULT '';
ALTER TABLE org.suppliers ADD COLUMN IF NOT EXISTS fax text NOT NULL DEFAULT '';
ALTER TABLE org.suppliers ADD COLUMN IF NOT EXISTS sales_contact text NOT NULL DEFAULT '';
ALTER TABLE org.suppliers ADD COLUMN IF NOT EXISTS collection_contact text NOT NULL DEFAULT '';
ALTER TABLE org.suppliers ADD COLUMN IF NOT EXISTS credit_limit numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE org.suppliers ADD COLUMN IF NOT EXISTS current_balance numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE org.suppliers ADD COLUMN IF NOT EXISTS accounting_account text NOT NULL DEFAULT '';
ALTER TABLE org.suppliers ADD COLUMN IF NOT EXISTS discount_prompt_payment numeric(5,2) NOT NULL DEFAULT 0;
ALTER TABLE org.suppliers ADD COLUMN IF NOT EXISTS discount1 numeric(5,2) NOT NULL DEFAULT 0;
ALTER TABLE org.suppliers ADD COLUMN IF NOT EXISTS discount2 numeric(5,2) NOT NULL DEFAULT 0;
ALTER TABLE org.suppliers ADD COLUMN IF NOT EXISTS discount3 numeric(5,2) NOT NULL DEFAULT 0;
ALTER TABLE org.suppliers ADD COLUMN IF NOT EXISTS discount4 numeric(5,2) NOT NULL DEFAULT 0;
ALTER TABLE org.suppliers ADD COLUMN IF NOT EXISTS preferred_payment_method text NOT NULL DEFAULT 'transfer';
ALTER TABLE org.suppliers ADD COLUMN IF NOT EXISTS bank_clabe text NOT NULL DEFAULT '';
ALTER TABLE org.suppliers ADD COLUMN IF NOT EXISTS bank_name text NOT NULL DEFAULT '';
ALTER TABLE org.suppliers ADD COLUMN IF NOT EXISTS bank_account text NOT NULL DEFAULT '';
ALTER TABLE org.suppliers ADD COLUMN IF NOT EXISTS notes text NOT NULL DEFAULT '';

-- Indexes for performance
CREATE INDEX IF NOT EXISTS idx_purchase_orders_order_type ON purchase.purchase_orders(order_type);
CREATE INDEX IF NOT EXISTS idx_purchase_orders_status ON purchase.purchase_orders(status);
CREATE INDEX IF NOT EXISTS idx_purchase_receipts_receipt_type ON purchase.purchase_receipts(receipt_type);
CREATE INDEX IF NOT EXISTS idx_purchase_receipts_status ON purchase.purchase_receipts(status);
CREATE INDEX IF NOT EXISTS idx_purchase_receipts_payment_status ON purchase.purchase_receipts(payment_status);
CREATE INDEX IF NOT EXISTS idx_material_stock_balances_material ON inventory.material_stock_balances(material_item_id, warehouse_id);
CREATE INDEX IF NOT EXISTS idx_material_inventory_movements_material ON inventory.material_inventory_movements(material_item_id, movement_date);
";
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = alterSql;
            await cmd.ExecuteNonQueryAsync();
        }
    }

    private static async Task EnsureSeriesAsync(NanchesoftDbContext db)
    {
        var company = await db.Companies.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        if (company is null) return;

        var seriesDefs = new[]
        {
            ("OC-MAT",  "Orden de Compra Materiales",   "OC-MAT",  "purchase_order_materials"),
            ("OC-SERV", "Orden de Compra Servicios",    "OC-SERV", "purchase_order_services"),
            ("ENT-REV", "Entrada por Revisión",         "ENT-REV", "purchase_review_receipt"),
            ("COMP-FAC","Compra con Factura",           "COMP-FAC","purchase_invoice_receipt"),
            ("DEV-MAT", "Devolución Materiales",        "DEV-MAT", "purchase_return_materials"),
            ("PAG-PROV","Pago a Proveedor",             "PAG-PROV","purchase_payment"),
        };

        foreach (var (code, name, prefix, docType) in seriesDefs)
        {
            var exists = await db.DocumentSeries
                .AnyAsync(s => s.CompanyId == company.Id && s.Code == code);
            if (!exists)
            {
                db.DocumentSeries.Add(new DocumentSeries
                {
                    TenantId = company.TenantId,
                    CompanyId = company.Id,
                    Code = code,
                    Name = name,
                    Prefix = prefix,
                    DocumentType = docType,
                    CurrentNumber = 0,
                    NumberLength = 5,
                    IsDefault = false,
                    CreatedBy = "seeder"
                });
            }
        }

        await db.SaveChangesAsync();
    }
}
