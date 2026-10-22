DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'fulfillment') THEN
        CREATE SCHEMA fulfillment;
    END IF;
END $EF$;


CREATE TABLE fulfillment.consolidated_package_members (
    consolidated_package_member_id uuid NOT NULL,
    consolidated_package_id uuid NOT NULL,
    shipment_id uuid NOT NULL,
    seller_party_id uuid NOT NULL,
    fulfillment_id uuid NOT NULL,
    joined_at timestamp with time zone NOT NULL,
    released_at timestamp with time zone,
    CONSTRAINT pk_consolidated_package_members PRIMARY KEY (consolidated_package_member_id)
);


CREATE TABLE fulfillment.consolidated_packages (
    consolidated_package_id uuid NOT NULL,
    package_number character varying(32) NOT NULL,
    checkout_id uuid NOT NULL,
    status character varying(32) NOT NULL,
    shipping_method_code character varying(64) NOT NULL,
    shipping_method_label character varying(128) NOT NULL,
    tracking_reference character varying(128),
    note character varying(512),
    created_by uuid,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    dispatched_at timestamp with time zone,
    delivered_at timestamp with time zone,
    cancelled_at timestamp with time zone,
    CONSTRAINT pk_consolidated_packages PRIMARY KEY (consolidated_package_id)
);


CREATE TABLE fulfillment.fulfillments (
    fulfillment_id uuid NOT NULL,
    seller_order_id uuid NOT NULL,
    checkout_id uuid NOT NULL,
    seller_party_id uuid NOT NULL,
    placed_by_user_id uuid NOT NULL,
    status character varying(32) NOT NULL,
    recipient_name character varying(256) NOT NULL,
    contact_mobile character varying(32) NOT NULL,
    province_name character varying(128) NOT NULL,
    city_name character varying(128) NOT NULL,
    postal_address character varying(512) NOT NULL,
    postal_code character varying(32) NOT NULL,
    shipping_method_code character varying(64) NOT NULL,
    shipping_method_label character varying(128) NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    CONSTRAINT pk_fulfillments PRIMARY KEY (fulfillment_id)
);


CREATE TABLE fulfillment.items (
    fulfillment_item_id uuid NOT NULL,
    fulfillment_id uuid NOT NULL,
    order_line_id uuid NOT NULL,
    quantity_ordered numeric(18,6) NOT NULL,
    quantity_processing numeric(18,6) NOT NULL,
    quantity_packed numeric(18,6) NOT NULL,
    quantity_shipped numeric(18,6) NOT NULL,
    reservation_id uuid,
    shipping_group_id uuid,
    reservation_consumed boolean NOT NULL,
    CONSTRAINT pk_items PRIMARY KEY (fulfillment_item_id)
);


CREATE TABLE fulfillment.outbox_messages (
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


CREATE TABLE fulfillment.payment_inbox (
    event_id uuid NOT NULL,
    payment_id uuid NOT NULL,
    processed_at timestamp with time zone NOT NULL,
    CONSTRAINT pk_payment_inbox PRIMARY KEY (event_id)
);


CREATE TABLE fulfillment.shipment_items (
    shipment_item_id uuid NOT NULL,
    shipment_id uuid NOT NULL,
    order_line_id uuid NOT NULL,
    quantity numeric(18,6) NOT NULL,
    CONSTRAINT pk_shipment_items PRIMARY KEY (shipment_item_id)
);


CREATE TABLE fulfillment.shipments (
    shipment_id uuid NOT NULL,
    fulfillment_id uuid NOT NULL,
    shipping_group_id uuid,
    status character varying(32) NOT NULL,
    carrier_display_name character varying(128) NOT NULL,
    shipping_method_code character varying(64) NOT NULL,
    shipping_method_label character varying(128) NOT NULL,
    provider_metadata_json text,
    provider_metadata_version integer NOT NULL,
    tracking_reference character varying(128),
    previous_tracking_reference character varying(128),
    dispatched_at timestamp with time zone,
    delivered_at timestamp with time zone,
    created_at timestamp with time zone NOT NULL,
    CONSTRAINT pk_shipments PRIMARY KEY (shipment_id)
);


CREATE TABLE fulfillment.shipping_groups (
    shipping_group_id uuid NOT NULL,
    fulfillment_id uuid NOT NULL,
    seller_order_id uuid NOT NULL,
    seller_party_id uuid NOT NULL,
    method_code character varying(64) NOT NULL,
    service_code character varying(64),
    option_code character varying(64),
    method_label character varying(128) NOT NULL,
    origin_context character varying(256),
    minimum_delivery_date date NOT NULL,
    requested_delivery_date date,
    requested_delivery_time_window character varying(64),
    lead_days integer NOT NULL,
    CONSTRAINT pk_shipping_groups PRIMARY KEY (shipping_group_id)
);


CREATE TABLE fulfillment.shipping_methods (
    shipping_method_id uuid NOT NULL,
    method_code character varying(32) NOT NULL,
    provider_kind character varying(64) NOT NULL,
    legacy_service_code character varying(64) NOT NULL,
    legacy_option_code character varying(64),
    sort_order integer NOT NULL,
    is_active boolean NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    CONSTRAINT pk_shipping_methods PRIMARY KEY (shipping_method_id),
    CONSTRAINT ck_shipping_methods_code_canonical CHECK (length(method_code) BETWEEN 1 AND 32 AND method_code = lower(method_code) AND method_code !~ '[^a-z0-9_:]'),
    CONSTRAINT ck_shipping_methods_option_code_canonical CHECK (legacy_option_code IS NULL OR (length(btrim(legacy_option_code)) BETWEEN 1 AND 64 AND legacy_option_code = lower(legacy_option_code)))
);


CREATE TABLE fulfillment.shipping_service_option_translations (
    translation_id uuid NOT NULL,
    shipping_service_option_id uuid NOT NULL,
    language_id uuid NOT NULL,
    name character varying(128) NOT NULL,
    CONSTRAINT pk_shipping_service_option_translations PRIMARY KEY (translation_id)
);


CREATE TABLE fulfillment.shipping_service_options (
    shipping_service_option_id uuid NOT NULL,
    shipping_service_id uuid NOT NULL,
    code character varying(64) NOT NULL,
    sort_order integer NOT NULL,
    is_active boolean NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    CONSTRAINT pk_shipping_service_options PRIMARY KEY (shipping_service_option_id)
);


CREATE TABLE fulfillment.shipping_service_translations (
    translation_id uuid NOT NULL,
    shipping_service_id uuid NOT NULL,
    language_id uuid NOT NULL,
    name character varying(128) NOT NULL,
    description character varying(512),
    CONSTRAINT pk_shipping_service_translations PRIMARY KEY (translation_id)
);


CREATE TABLE fulfillment.shipping_services (
    shipping_service_id uuid NOT NULL,
    code character varying(64) NOT NULL,
    provider_kind character varying(64) NOT NULL,
    icon_key character varying(32) NOT NULL,
    color_key character varying(32) NOT NULL,
    sort_order integer NOT NULL,
    is_active boolean NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    CONSTRAINT pk_shipping_services PRIMARY KEY (shipping_service_id)
);


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
    CONSTRAINT ck_shipping_types_revision_positive CHECK (revision > 0),
    CONSTRAINT ck_shipping_types_sort_order_range CHECK (sort_order >= 0 AND sort_order <= 1000000)
);


CREATE TABLE fulfillment.shipping_method_translations (
    translation_id uuid NOT NULL,
    shipping_method_id uuid NOT NULL,
    language_id uuid NOT NULL,
    name character varying(128) NOT NULL,
    description character varying(512),
    CONSTRAINT pk_shipping_method_translations PRIMARY KEY (translation_id),
    CONSTRAINT fk_shipping_method_translations_method FOREIGN KEY (shipping_method_id) REFERENCES fulfillment.shipping_methods (shipping_method_id) ON DELETE CASCADE
);


CREATE TABLE fulfillment.shipping_type_translations (
    shipping_type_translation_id uuid NOT NULL,
    shipping_type_id uuid NOT NULL,
    language_id uuid NOT NULL,
    name character varying(128) NOT NULL,
    description character varying(512),
    CONSTRAINT pk_shipping_type_translations PRIMARY KEY (shipping_type_translation_id),
    CONSTRAINT ck_shipping_type_translations_description_nonblank CHECK (description IS NULL OR (char_length(description) BETWEEN 1 AND 512 AND description = btrim(description))),
    CONSTRAINT ck_shipping_type_translations_name_nonblank CHECK (char_length(name) BETWEEN 1 AND 128 AND name = btrim(name)),
    CONSTRAINT fk_shipping_type_translations_shipping_types_shipping_type_id FOREIGN KEY (shipping_type_id) REFERENCES fulfillment.shipping_types (shipping_type_id) ON DELETE CASCADE
);


CREATE INDEX ix_consolidated_package_members_package_id ON fulfillment.consolidated_package_members (consolidated_package_id);


CREATE UNIQUE INDEX ix_consolidated_package_members_shipment_active ON fulfillment.consolidated_package_members (shipment_id) WHERE released_at IS NULL;


CREATE INDEX ix_consolidated_packages_checkout_id ON fulfillment.consolidated_packages (checkout_id);


CREATE UNIQUE INDEX ix_consolidated_packages_package_number ON fulfillment.consolidated_packages (package_number);


CREATE INDEX ix_fulfillments_checkout_id ON fulfillment.fulfillments (checkout_id);


CREATE UNIQUE INDEX ix_fulfillments_seller_order_id ON fulfillment.fulfillments (seller_order_id);


CREATE INDEX ix_fulfillments_seller_party_id ON fulfillment.fulfillments (seller_party_id);


CREATE INDEX ix_items_fulfillment_id ON fulfillment.items (fulfillment_id);


CREATE INDEX ix_items_shipping_group_id ON fulfillment.items (shipping_group_id) WHERE shipping_group_id IS NOT NULL;


CREATE INDEX ix_outbox_messages_pending ON fulfillment.outbox_messages (occurred_at) WHERE processed_at IS NULL AND dead_lettered_at IS NULL;


CREATE INDEX ix_shipment_items_shipment_id ON fulfillment.shipment_items (shipment_id);


CREATE INDEX ix_shipments_fulfillment_id ON fulfillment.shipments (fulfillment_id);


CREATE INDEX ix_shipments_shipping_group_id ON fulfillment.shipments (shipping_group_id) WHERE shipping_group_id IS NOT NULL;


CREATE UNIQUE INDEX ix_shipments_tracking_reference ON fulfillment.shipments (tracking_reference) WHERE tracking_reference IS NOT NULL;


CREATE UNIQUE INDEX ix_shipping_groups_fulfillment_group ON fulfillment.shipping_groups (fulfillment_id, shipping_group_id);


CREATE INDEX ix_shipping_groups_fulfillment_id ON fulfillment.shipping_groups (fulfillment_id);


CREATE UNIQUE INDEX ix_shipping_method_translations_method_lang ON fulfillment.shipping_method_translations (shipping_method_id, language_id);


CREATE INDEX ix_shipping_methods_legacy_service_code ON fulfillment.shipping_methods (legacy_service_code);


CREATE UNIQUE INDEX ix_shipping_methods_method_code ON fulfillment.shipping_methods (method_code);


CREATE UNIQUE INDEX ux_shipping_methods_legacy_binding ON fulfillment.shipping_methods (legacy_service_code, legacy_option_code) WHERE legacy_option_code IS NOT NULL;


CREATE UNIQUE INDEX ix_shipping_service_option_translations_option_lang ON fulfillment.shipping_service_option_translations (shipping_service_option_id, language_id);


CREATE UNIQUE INDEX ix_shipping_service_options_service_code ON fulfillment.shipping_service_options (shipping_service_id, code);


CREATE INDEX ix_shipping_service_options_service_id ON fulfillment.shipping_service_options (shipping_service_id);


CREATE UNIQUE INDEX ix_shipping_service_translations_service_lang ON fulfillment.shipping_service_translations (shipping_service_id, language_id);


CREATE UNIQUE INDEX ix_shipping_services_code ON fulfillment.shipping_services (code);


CREATE UNIQUE INDEX ix_shipping_type_translations_type_language ON fulfillment.shipping_type_translations (shipping_type_id, language_id);


CREATE INDEX ix_shipping_types_active_sort ON fulfillment.shipping_types (is_active, sort_order);


CREATE UNIQUE INDEX ix_shipping_types_code ON fulfillment.shipping_types (code);


