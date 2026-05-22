-- Nanchesoft IA — Persistencia de conversaciones y mensajes
-- Idempotente: crea schema, tablas e índices si no existen.

CREATE SCHEMA IF NOT EXISTS ai;

CREATE TABLE IF NOT EXISTS ai.ai_conversations (
    id uuid PRIMARY KEY,
    is_active boolean NOT NULL DEFAULT true,
    created_at timestamptz NOT NULL DEFAULT now(),
    created_by varchar(120) NULL,
    updated_at timestamptz NULL,
    updated_by varchar(120) NULL,
    tenant_id uuid NOT NULL,
    company_id uuid NULL,
    branch_id uuid NULL,
    user_id uuid NOT NULL,
    title varchar(240) NOT NULL,
    module varchar(40) NOT NULL DEFAULT 'hr_payroll',
    status varchar(20) NOT NULL DEFAULT 'active',
    last_intent varchar(80) NULL,
    last_activity_at timestamptz NOT NULL DEFAULT now(),
    message_count integer NOT NULL DEFAULT 0
);

CREATE INDEX IF NOT EXISTS ix_ai_conversations_tenant_user_active
    ON ai.ai_conversations (tenant_id, user_id, is_active);

CREATE INDEX IF NOT EXISTS ix_ai_conversations_tenant_company_user_activity
    ON ai.ai_conversations (tenant_id, company_id, user_id, last_activity_at DESC);

CREATE TABLE IF NOT EXISTS ai.ai_messages (
    id uuid PRIMARY KEY,
    is_active boolean NOT NULL DEFAULT true,
    created_at timestamptz NOT NULL DEFAULT now(),
    created_by varchar(120) NULL,
    updated_at timestamptz NULL,
    updated_by varchar(120) NULL,
    tenant_id uuid NOT NULL,
    conversation_id uuid NOT NULL REFERENCES ai.ai_conversations(id) ON DELETE CASCADE,
    role varchar(20) NOT NULL,
    content text NOT NULL,
    intent varchar(80) NULL,
    endpoint varchar(200) NULL,
    data_json jsonb NULL,
    sequence_number integer NOT NULL DEFAULT 0
);

CREATE INDEX IF NOT EXISTS ix_ai_messages_conversation_sequence
    ON ai.ai_messages (conversation_id, sequence_number);

CREATE INDEX IF NOT EXISTS ix_ai_messages_tenant_conversation_created
    ON ai.ai_messages (tenant_id, conversation_id, created_at);
