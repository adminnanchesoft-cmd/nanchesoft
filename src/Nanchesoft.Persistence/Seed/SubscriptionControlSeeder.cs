using Microsoft.EntityFrameworkCore;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Persistence.Seed;

public static class SubscriptionControlSeeder
{
    public static async Task EnsureAsync(NanchesoftDbContext dbContext)
    {
        const string sql = """
CREATE SCHEMA IF NOT EXISTS subscription;

CREATE TABLE IF NOT EXISTS subscription.subscription_charges
(
    id uuid PRIMARY KEY,
    is_active boolean NOT NULL DEFAULT true,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    created_by text NULL,
    updated_at timestamp with time zone NULL,
    updated_by text NULL,
    tenant_id uuid NOT NULL,
    plan_id uuid NOT NULL,
    charge_month text NOT NULL,
    billing_year integer NOT NULL,
    billing_month integer NOT NULL,
    tenant_code_snapshot text NOT NULL DEFAULT '',
    tenant_name_snapshot text NOT NULL DEFAULT '',
    plan_code_snapshot text NOT NULL DEFAULT '',
    plan_name_snapshot text NOT NULL DEFAULT '',
    charge_date timestamp with time zone NOT NULL,
    due_date timestamp with time zone NOT NULL,
    plan_price_monthly numeric(18,2) NOT NULL DEFAULT 0,
    discount_amount numeric(18,2) NOT NULL DEFAULT 0,
    surcharge_amount numeric(18,2) NOT NULL DEFAULT 0,
    total_amount numeric(18,2) NOT NULL DEFAULT 0,
    paid_amount numeric(18,2) NOT NULL DEFAULT 0,
    compensation_amount numeric(18,2) NOT NULL DEFAULT 0,
    balance_amount numeric(18,2) NOT NULL DEFAULT 0,
    paid_at timestamp with time zone NULL,
    payment_method text NOT NULL DEFAULT '',
    reference text NOT NULL DEFAULT '',
    status text NOT NULL DEFAULT 'pending',
    notes text NOT NULL DEFAULT ''
);

ALTER TABLE subscription.subscription_charges ADD COLUMN IF NOT EXISTS id uuid;
ALTER TABLE subscription.subscription_charges ADD COLUMN IF NOT EXISTS is_active boolean NOT NULL DEFAULT true;
ALTER TABLE subscription.subscription_charges ADD COLUMN IF NOT EXISTS created_at timestamp with time zone NOT NULL DEFAULT now();
ALTER TABLE subscription.subscription_charges ADD COLUMN IF NOT EXISTS created_by text NULL;
ALTER TABLE subscription.subscription_charges ADD COLUMN IF NOT EXISTS updated_at timestamp with time zone NULL;
ALTER TABLE subscription.subscription_charges ADD COLUMN IF NOT EXISTS updated_by text NULL;
ALTER TABLE subscription.subscription_charges ADD COLUMN IF NOT EXISTS tenant_id uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
ALTER TABLE subscription.subscription_charges ADD COLUMN IF NOT EXISTS plan_id uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
ALTER TABLE subscription.subscription_charges ADD COLUMN IF NOT EXISTS charge_month text NOT NULL DEFAULT '';
ALTER TABLE subscription.subscription_charges ADD COLUMN IF NOT EXISTS billing_year integer NOT NULL DEFAULT 0;
ALTER TABLE subscription.subscription_charges ADD COLUMN IF NOT EXISTS billing_month integer NOT NULL DEFAULT 0;
ALTER TABLE subscription.subscription_charges ADD COLUMN IF NOT EXISTS tenant_code_snapshot text NOT NULL DEFAULT '';
ALTER TABLE subscription.subscription_charges ADD COLUMN IF NOT EXISTS tenant_name_snapshot text NOT NULL DEFAULT '';
ALTER TABLE subscription.subscription_charges ADD COLUMN IF NOT EXISTS plan_code_snapshot text NOT NULL DEFAULT '';
ALTER TABLE subscription.subscription_charges ADD COLUMN IF NOT EXISTS plan_name_snapshot text NOT NULL DEFAULT '';
ALTER TABLE subscription.subscription_charges ADD COLUMN IF NOT EXISTS charge_date timestamp with time zone NOT NULL DEFAULT now();
ALTER TABLE subscription.subscription_charges ADD COLUMN IF NOT EXISTS due_date timestamp with time zone NOT NULL DEFAULT now();
ALTER TABLE subscription.subscription_charges ADD COLUMN IF NOT EXISTS plan_price_monthly numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE subscription.subscription_charges ADD COLUMN IF NOT EXISTS discount_amount numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE subscription.subscription_charges ADD COLUMN IF NOT EXISTS surcharge_amount numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE subscription.subscription_charges ADD COLUMN IF NOT EXISTS total_amount numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE subscription.subscription_charges ADD COLUMN IF NOT EXISTS paid_amount numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE subscription.subscription_charges ADD COLUMN IF NOT EXISTS compensation_amount numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE subscription.subscription_charges ADD COLUMN IF NOT EXISTS balance_amount numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE subscription.subscription_charges ADD COLUMN IF NOT EXISTS paid_at timestamp with time zone NULL;
ALTER TABLE subscription.subscription_charges ADD COLUMN IF NOT EXISTS payment_method text NOT NULL DEFAULT '';
ALTER TABLE subscription.subscription_charges ADD COLUMN IF NOT EXISTS reference text NOT NULL DEFAULT '';
ALTER TABLE subscription.subscription_charges ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'pending';
ALTER TABLE subscription.subscription_charges ADD COLUMN IF NOT EXISTS notes text NOT NULL DEFAULT '';

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.table_constraints
        WHERE table_schema = 'subscription'
          AND table_name = 'subscription_charges'
          AND constraint_type = 'PRIMARY KEY') THEN
        ALTER TABLE subscription.subscription_charges ADD PRIMARY KEY (id);
    END IF;
END $$;

CREATE UNIQUE INDEX IF NOT EXISTS ux_subscription_charges_tenant_month
    ON subscription.subscription_charges(tenant_id, billing_year, billing_month);

CREATE INDEX IF NOT EXISTS ix_subscription_charges_status
    ON subscription.subscription_charges(status);

CREATE INDEX IF NOT EXISTS ix_subscription_charges_due_date
    ON subscription.subscription_charges(due_date);
""";

        await dbContext.Database.ExecuteSqlRawAsync(sql);
    }
}
