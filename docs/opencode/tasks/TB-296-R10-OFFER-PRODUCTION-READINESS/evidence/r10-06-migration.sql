DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'offer') THEN
        CREATE SCHEMA offer;
    END IF;
END $EF$;
CREATE TABLE IF NOT EXISTS offer.__ef_migrations_history (
    migration_id character varying(150) NOT NULL,
    product_version character varying(32) NOT NULL,
    CONSTRAINT pk___ef_migrations_history PRIMARY KEY (migration_id)
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'offer') THEN
        CREATE SCHEMA offer;
    END IF;
END $EF$;

CREATE TABLE offer.offers (
    offer_id uuid NOT NULL,
    catalog_variant_id uuid NOT NULL,
    seller_party_id uuid NOT NULL,
    seller_sku character varying(64),
    status character varying(32) NOT NULL,
    channel character varying(32) NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    CONSTRAINT pk_offers PRIMARY KEY (offer_id)
);

CREATE TABLE offer.outbox_messages (
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

CREATE UNIQUE INDEX ix_offers_seller_party_id_catalog_variant_id_channel ON offer.offers (seller_party_id, catalog_variant_id, channel) WHERE status <> 'Archived';

CREATE UNIQUE INDEX ix_offers_seller_party_id_seller_sku ON offer.offers (seller_party_id, seller_sku) WHERE seller_sku IS NOT NULL;

CREATE INDEX ix_outbox_messages_pending ON offer.outbox_messages (occurred_at) WHERE processed_at IS NULL AND dead_lettered_at IS NULL;

INSERT INTO offer.__ef_migrations_history (migration_id, product_version)
VALUES ('20260823082919_InitialOffer', '8.0.11');

COMMIT;

START TRANSACTION;

ALTER TABLE offer.offers
    ADD COLUMN IF NOT EXISTS return_policy_choice character varying(32) NOT NULL DEFAULT 'Default';
ALTER TABLE offer.offers
    ADD COLUMN IF NOT EXISTS custom_return_window_days integer NULL;

INSERT INTO offer.__ef_migrations_history (migration_id, product_version)
VALUES ('20260907120000_AddOfferReturnPolicy', '8.0.11');

COMMIT;

START TRANSACTION;

ALTER TABLE offer.offers ADD minimum_order_quantity numeric(18,6);

ALTER TABLE offer.offers ADD maximum_order_quantity numeric(18,6);

INSERT INTO offer.__ef_migrations_history (migration_id, product_version)
VALUES ('20260909130100_AddOfferQuantityLimits', '8.0.11');

COMMIT;

START TRANSACTION;

ALTER TABLE offer.offers ADD fulfillment_responsibility character varying(16);

ALTER TABLE offer.offers ADD fulfillment_shipping_type_id uuid;

INSERT INTO offer.__ef_migrations_history (migration_id, product_version)
VALUES ('20261027120000_AddOfferFulfillmentAssignment', '8.0.11');

COMMIT;

START TRANSACTION;

ALTER TABLE offer.offers ADD creation_intent_fingerprint character varying(64);

INSERT INTO offer.__ef_migrations_history (migration_id, product_version)
VALUES ('20261123120000_AddOfferCreationIntentFingerprint', '8.0.11');

COMMIT;

START TRANSACTION;

CREATE TABLE offer.offer_history (
    offer_history_entry_id uuid NOT NULL,
    offer_id uuid NOT NULL,
    occurred_at timestamp with time zone NOT NULL,
    change_kind character varying(64) NOT NULL,
    actor_user_id uuid,
    summary character varying(512) NOT NULL,
    old_value character varying(1024),
    new_value character varying(1024),
    CONSTRAINT pk_offer_history PRIMARY KEY (offer_history_entry_id)
);

CREATE INDEX ix_offer_history_offer_id_occurred_at ON offer.offer_history (offer_id, occurred_at DESC);

INSERT INTO offer.__ef_migrations_history (migration_id, product_version)
VALUES ('20261126120000_R10OfferHistory', '8.0.11');

COMMIT;

