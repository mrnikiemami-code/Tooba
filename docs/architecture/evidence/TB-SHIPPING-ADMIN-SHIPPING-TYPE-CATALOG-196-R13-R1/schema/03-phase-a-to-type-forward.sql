START TRANSACTION;

CREATE TABLE fulfillment.shipping_types (
    shipping_type_id uuid NOT NULL,
    code character varying(64) NOT NULL,
    sort_order integer NOT NULL,
    is_active boolean NOT NULL,
    revision bigint NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    CONSTRAINT pk_shipping_types PRIMARY KEY (shipping_type_id),
    CONSTRAINT ck_shipping_types_code_canonical CHECK (char_length(code) BETWEEN 1 AND 64 AND code = btrim(code) AND code ~ '^[a-z][a-z0-9_]*$'),
    CONSTRAINT ck_shipping_types_sort_order_range CHECK (sort_order >= 0 AND sort_order <= 1000000),
    CONSTRAINT ck_shipping_types_revision_positive CHECK (revision > 0)
);

CREATE TABLE fulfillment.shipping_type_translations (
    shipping_type_translation_id uuid NOT NULL,
    shipping_type_id uuid NOT NULL,
    language_id uuid NOT NULL,
    name character varying(128) NOT NULL,
    description character varying(512),
    CONSTRAINT pk_shipping_type_translations PRIMARY KEY (shipping_type_translation_id),
    CONSTRAINT ck_shipping_type_translations_name_nonblank CHECK (char_length(name) BETWEEN 1 AND 128 AND name = btrim(name)),
    CONSTRAINT ck_shipping_type_translations_description_nonblank CHECK (description IS NULL OR (char_length(description) BETWEEN 1 AND 512 AND description = btrim(description))),
    CONSTRAINT fk_shipping_type_translations_shipping_types_shipping_type_id FOREIGN KEY (shipping_type_id) REFERENCES fulfillment.shipping_types (shipping_type_id) ON DELETE CASCADE
);

CREATE UNIQUE INDEX ix_shipping_types_code ON fulfillment.shipping_types (code);

CREATE INDEX ix_shipping_types_active_sort ON fulfillment.shipping_types (is_active, sort_order);

CREATE UNIQUE INDEX ix_shipping_type_translations_type_language ON fulfillment.shipping_type_translations (shipping_type_id, language_id);

INSERT INTO fulfillment.__ef_migrations_history (migration_id, product_version)
VALUES ('20261022101415_ShippingTypeCatalog', '8.0.11');

COMMIT;
