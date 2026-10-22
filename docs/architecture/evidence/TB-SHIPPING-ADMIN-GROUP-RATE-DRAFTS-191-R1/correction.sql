START TRANSACTION;

ALTER TABLE shipping.authoring_region_groups DROP CONSTRAINT ck_authoring_region_groups_currency_canonical;

ALTER TABLE shipping.authoring_rate_rows DROP CONSTRAINT ck_authoring_rate_rows_amount_within_max;

ALTER TABLE shipping.authoring_rate_rows DROP CONSTRAINT ck_authoring_rate_rows_count_interval;

ALTER TABLE shipping.authoring_rate_rows DROP CONSTRAINT ck_authoring_rate_rows_merchandise_interval;

ALTER TABLE shipping.authoring_rate_rows DROP CONSTRAINT ck_authoring_rate_rows_weight_interval;

ALTER TABLE shipping.authoring_region_groups ADD CONSTRAINT ck_authoring_region_groups_currency_canonical CHECK (currency_code IS NULL OR (octet_length(currency_code) = 3 AND currency_code COLLATE "C" ~ '^[A-Z]{3}$'));

ALTER TABLE shipping.authoring_rate_rows ADD CONSTRAINT ck_authoring_rate_rows_amount_within_max CHECK (freight_amount <= 1000000000 AND packaging_amount <= 1000000000 AND (min_merchandise_amount IS NULL OR min_merchandise_amount <= 1000000000) AND (max_merchandise_amount IS NULL OR max_merchandise_amount <= 1000000000));

ALTER TABLE shipping.authoring_rate_rows ADD CONSTRAINT ck_authoring_rate_rows_count_interval CHECK ((min_item_count IS NULL AND max_item_count IS NULL) OR (min_item_count IS NOT NULL AND min_item_count >= 1 AND min_item_count <= 1000000 AND (max_item_count IS NULL OR (max_item_count > min_item_count AND max_item_count <= 1000000))));

ALTER TABLE shipping.authoring_rate_rows ADD CONSTRAINT ck_authoring_rate_rows_merchandise_interval CHECK ((min_merchandise_amount IS NULL AND max_merchandise_amount IS NULL) OR (min_merchandise_amount IS NOT NULL AND min_merchandise_amount >= 0 AND min_merchandise_amount <= 1000000000 AND (max_merchandise_amount IS NULL OR (max_merchandise_amount > min_merchandise_amount AND max_merchandise_amount <= 1000000000))));

ALTER TABLE shipping.authoring_rate_rows ADD CONSTRAINT ck_authoring_rate_rows_weight_interval CHECK ((min_weight_grams IS NULL AND max_weight_grams IS NULL) OR (min_weight_grams IS NOT NULL AND min_weight_grams >= 0 AND min_weight_grams <= 1000000000 AND (max_weight_grams IS NULL OR (max_weight_grams > min_weight_grams AND max_weight_grams <= 1000000000))));

INSERT INTO shipping.__ef_migrations_history (migration_id, product_version)
VALUES ('20261022075711_AuthoringRateRowConstraintParity', '8.0.11');

COMMIT;

