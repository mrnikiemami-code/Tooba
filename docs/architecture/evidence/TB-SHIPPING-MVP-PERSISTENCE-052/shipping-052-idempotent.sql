DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'shipping') THEN
        CREATE SCHEMA shipping;
    END IF;
END $EF$;
CREATE TABLE IF NOT EXISTS shipping.__ef_migrations_history (
    migration_id character varying(150) NOT NULL,
    product_version character varying(32) NOT NULL,
    CONSTRAINT pk___ef_migrations_history PRIMARY KEY (migration_id)
);

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM shipping.__ef_migrations_history WHERE "migration_id" = '20261014144742_InitialShipping') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'shipping') THEN
            CREATE SCHEMA shipping;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM shipping.__ef_migrations_history WHERE "migration_id" = '20261014144742_InitialShipping') THEN
    CREATE TABLE shipping.coverage_grants (
        coverage_grant_id uuid NOT NULL,
        service_code character varying(64) NOT NULL,
        country_id uuid NOT NULL,
        administrative_area_id uuid,
        city_id uuid,
        service_area_id uuid,
        is_active boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_coverage_grants PRIMARY KEY (coverage_grant_id),
        CONSTRAINT ck_coverage_grants_scope_coherent CHECK (service_area_id IS NULL OR city_id IS NOT NULL),
        CONSTRAINT ck_coverage_grants_service_code_canonical CHECK (char_length(service_code) > 0 AND service_code = btrim(service_code))
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM shipping.__ef_migrations_history WHERE "migration_id" = '20261014144742_InitialShipping') THEN
    CREATE TABLE shipping.outbox_messages (
        id uuid NOT NULL,
        occurred_at timestamp with time zone NOT NULL,
        event_type character varying(256) NOT NULL,
        payload text NOT NULL,
        correlation_id character varying(128),
        version integer NOT NULL,
        tenant_id character varying(128),
        deployment_id character varying(128) NOT NULL,
        edition character varying(32) NOT NULL,
        processed_at timestamp with time zone,
        attempt_count integer NOT NULL,
        next_attempt_at timestamp with time zone,
        dead_lettered_at timestamp with time zone,
        last_error character varying(256),
        locked_until timestamp with time zone,
        CONSTRAINT pk_outbox_messages PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM shipping.__ef_migrations_history WHERE "migration_id" = '20261014144742_InitialShipping') THEN
    CREATE TABLE shipping.rate_policies (
        rate_policy_id uuid NOT NULL,
        service_code character varying(64) NOT NULL,
        country_id uuid NOT NULL,
        administrative_area_id uuid,
        city_id uuid,
        service_area_id uuid,
        charge_amount numeric(18,2) NOT NULL,
        currency_code character varying(3) NOT NULL,
        preparation_days integer NOT NULL,
        lead_days integer NOT NULL,
        free_above_threshold numeric(18,2),
        is_active boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_rate_policies PRIMARY KEY (rate_policy_id),
        CONSTRAINT ck_rate_policies_charge_amount_non_negative CHECK (charge_amount >= 0),
        CONSTRAINT ck_rate_policies_charge_amount_within_max CHECK (charge_amount <= 1000000000),
        CONSTRAINT ck_rate_policies_currency_code_canonical CHECK (char_length(currency_code) = 3 AND currency_code = upper(currency_code)),
        CONSTRAINT ck_rate_policies_free_above_threshold_non_negative CHECK (free_above_threshold IS NULL OR free_above_threshold >= 0),
        CONSTRAINT ck_rate_policies_lead_days_range CHECK (lead_days >= 0 AND lead_days <= 365),
        CONSTRAINT ck_rate_policies_preparation_days_range CHECK (preparation_days >= 0 AND preparation_days <= 365),
        CONSTRAINT ck_rate_policies_scope_coherent CHECK (service_area_id IS NULL OR city_id IS NOT NULL),
        CONSTRAINT ck_rate_policies_service_code_canonical CHECK (char_length(service_code) > 0 AND service_code = btrim(service_code))
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM shipping.__ef_migrations_history WHERE "migration_id" = '20261014144742_InitialShipping') THEN
    CREATE INDEX ix_coverage_grants_service_country_active ON shipping.coverage_grants (service_code, country_id, is_active);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM shipping.__ef_migrations_history WHERE "migration_id" = '20261014144742_InitialShipping') THEN
    CREATE UNIQUE INDEX ux_coverage_grants_service_scope ON shipping.coverage_grants (service_code, country_id, administrative_area_id, city_id, service_area_id) NULLS NOT DISTINCT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM shipping.__ef_migrations_history WHERE "migration_id" = '20261014144742_InitialShipping') THEN
    CREATE INDEX ix_outbox_messages_pending ON shipping.outbox_messages (occurred_at) WHERE processed_at IS NULL AND dead_lettered_at IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM shipping.__ef_migrations_history WHERE "migration_id" = '20261014144742_InitialShipping') THEN
    CREATE INDEX ix_rate_policies_service_active ON shipping.rate_policies (service_code, is_active);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM shipping.__ef_migrations_history WHERE "migration_id" = '20261014144742_InitialShipping') THEN
    CREATE UNIQUE INDEX ux_rate_policies_service_scope ON shipping.rate_policies (service_code, country_id, administrative_area_id, city_id, service_area_id) NULLS NOT DISTINCT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM shipping.__ef_migrations_history WHERE "migration_id" = '20261014144742_InitialShipping') THEN
    INSERT INTO shipping.__ef_migrations_history (migration_id, product_version)
    VALUES ('20261014144742_InitialShipping', '8.0.11');
    END IF;
END $EF$;
COMMIT;

