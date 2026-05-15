CREATE SCHEMA IF NOT EXISTS sales;

CREATE TABLE IF NOT EXISTS sales.service_catalog_items
(
    "Id" uuid NOT NULL,
    "IsActive" boolean NOT NULL DEFAULT TRUE,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
    "CreatedBy" text NULL,
    "UpdatedAt" timestamp with time zone NULL,
    "UpdatedBy" text NULL,
    "TenantId" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "Code" character varying(40) NOT NULL,
    "Name" character varying(160) NOT NULL,
    "Description" character varying(500) NOT NULL DEFAULT '',
    "BillingUnit" character varying(30) NOT NULL DEFAULT 'HORA',
    "DefaultRate" numeric(18,2) NOT NULL DEFAULT 0,
    "Notes" character varying(600) NULL,
    CONSTRAINT "PK_service_catalog_items" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_service_catalog_items_CompanyId_Code"
    ON sales.service_catalog_items ("CompanyId", "Code");

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_service_catalog_items_tenants') THEN
        ALTER TABLE sales.service_catalog_items
            ADD CONSTRAINT "FK_service_catalog_items_tenants"
            FOREIGN KEY ("TenantId") REFERENCES core.tenants ("Id") ON DELETE RESTRICT;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_service_catalog_items_companies') THEN
        ALTER TABLE sales.service_catalog_items
            ADD CONSTRAINT "FK_service_catalog_items_companies"
            FOREIGN KEY ("CompanyId") REFERENCES core.companies ("Id") ON DELETE RESTRICT;
    END IF;
END $$;

CREATE TABLE IF NOT EXISTS sales.customer_service_rates
(
    "Id" uuid NOT NULL,
    "IsActive" boolean NOT NULL DEFAULT TRUE,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
    "CreatedBy" text NULL,
    "UpdatedAt" timestamp with time zone NULL,
    "UpdatedBy" text NULL,
    "TenantId" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CustomerId" uuid NOT NULL,
    "ServiceCatalogItemId" uuid NOT NULL,
    "CurrencyId" uuid NULL,
    "Rate" numeric(18,2) NOT NULL DEFAULT 0,
    "EffectiveFrom" timestamp with time zone NOT NULL,
    "EffectiveTo" timestamp with time zone NULL,
    "Notes" character varying(600) NULL,
    CONSTRAINT "PK_customer_service_rates" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_customer_service_rates_CompanyId_CustomerId_ServiceCatalogItemId_EffectiveFrom"
    ON sales.customer_service_rates ("CompanyId", "CustomerId", "ServiceCatalogItemId", "EffectiveFrom");

CREATE INDEX IF NOT EXISTS "IX_customer_service_rates_CustomerId_ServiceCatalogItemId_IsActive"
    ON sales.customer_service_rates ("CustomerId", "ServiceCatalogItemId", "IsActive");

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_customer_service_rates_tenants') THEN
        ALTER TABLE sales.customer_service_rates
            ADD CONSTRAINT "FK_customer_service_rates_tenants"
            FOREIGN KEY ("TenantId") REFERENCES core.tenants ("Id") ON DELETE RESTRICT;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_customer_service_rates_companies') THEN
        ALTER TABLE sales.customer_service_rates
            ADD CONSTRAINT "FK_customer_service_rates_companies"
            FOREIGN KEY ("CompanyId") REFERENCES core.companies ("Id") ON DELETE RESTRICT;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_customer_service_rates_customers') THEN
        ALTER TABLE sales.customer_service_rates
            ADD CONSTRAINT "FK_customer_service_rates_customers"
            FOREIGN KEY ("CustomerId") REFERENCES org.customers ("Id") ON DELETE RESTRICT;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_customer_service_rates_service_catalog_items') THEN
        ALTER TABLE sales.customer_service_rates
            ADD CONSTRAINT "FK_customer_service_rates_service_catalog_items"
            FOREIGN KEY ("ServiceCatalogItemId") REFERENCES sales.service_catalog_items ("Id") ON DELETE RESTRICT;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_customer_service_rates_currencies') THEN
        ALTER TABLE sales.customer_service_rates
            ADD CONSTRAINT "FK_customer_service_rates_currencies"
            FOREIGN KEY ("CurrencyId") REFERENCES catalog.currencies ("Id") ON DELETE RESTRICT;
    END IF;
END $$;

ALTER TABLE sales.service_notes ADD COLUMN IF NOT EXISTS "ServiceCatalogItemId" uuid NULL;
ALTER TABLE sales.service_notes ADD COLUMN IF NOT EXISTS "ServiceCodeSnapshot" character varying(40) NOT NULL DEFAULT '';
ALTER TABLE sales.service_notes ADD COLUMN IF NOT EXISTS "ServiceNameSnapshot" character varying(160) NOT NULL DEFAULT '';
ALTER TABLE sales.service_notes ADD COLUMN IF NOT EXISTS "StartTimeText" character varying(20) NULL;
ALTER TABLE sales.service_notes ADD COLUMN IF NOT EXISTS "EndTimeText" character varying(20) NULL;
ALTER TABLE sales.service_notes ADD COLUMN IF NOT EXISTS "BreakMinutes" integer NOT NULL DEFAULT 0;

CREATE INDEX IF NOT EXISTS "IX_service_notes_CompanyId_NoteDate"
    ON sales.service_notes ("CompanyId", "NoteDate");

CREATE UNIQUE INDEX IF NOT EXISTS "IX_service_notes_CompanyId_Folio"
    ON sales.service_notes ("CompanyId", "Folio");

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_service_notes_service_catalog_items') THEN
        ALTER TABLE sales.service_notes
            ADD CONSTRAINT "FK_service_notes_service_catalog_items"
            FOREIGN KEY ("ServiceCatalogItemId") REFERENCES sales.service_catalog_items ("Id") ON DELETE RESTRICT;
    END IF;
END $$;
