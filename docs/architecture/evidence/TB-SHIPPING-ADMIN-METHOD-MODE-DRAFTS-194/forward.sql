START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM shipping.__ef_migrations_history WHERE "migration_id" = '20261022093803_AuthoringMethodSettings') THEN
    CREATE TABLE shipping.authoring_method_settings (
        draft_id uuid NOT NULL,
        freight_payment_mode integer NOT NULL,
        calculation_mode integer NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_authoring_method_settings PRIMARY KEY (draft_id),
        CONSTRAINT ck_authoring_method_settings_calculation_mode CHECK (calculation_mode IN (0, 1, 2)),
        CONSTRAINT ck_authoring_method_settings_freight_mode CHECK (freight_payment_mode IN (0, 1)),
        CONSTRAINT fk_authoring_method_settings_draft FOREIGN KEY (draft_id) REFERENCES shipping.authoring_drafts (draft_id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM shipping.__ef_migrations_history WHERE "migration_id" = '20261022093803_AuthoringMethodSettings') THEN
    INSERT INTO shipping.__ef_migrations_history (migration_id, product_version)
    VALUES ('20261022093803_AuthoringMethodSettings', '8.0.11');
    END IF;
END $EF$;
COMMIT;

