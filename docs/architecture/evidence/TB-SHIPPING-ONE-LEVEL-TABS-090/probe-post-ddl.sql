\pset pager off
\echo == one-level history rows ==
SELECT migration_id FROM fulfillment.__ef_migrations_history WHERE migration_id LIKE '%ShippingOneLevel%';

\echo == new tables ==
SELECT table_schema || '.' || table_name FROM information_schema.tables
WHERE table_schema='fulfillment' AND table_name IN ('shipping_methods','shipping_method_translations') ORDER BY 1;

\echo == shipping_methods columns ==
SELECT column_name, data_type, character_maximum_length, is_nullable
FROM information_schema.columns
WHERE table_schema='fulfillment' AND table_name='shipping_methods' ORDER BY ordinal_position;

\echo == indexes ==
SELECT indexname FROM pg_indexes WHERE schemaname='fulfillment'
  AND indexname IN ('ix_shipping_methods_method_code','ux_shipping_methods_legacy_binding','ix_shipping_methods_legacy_service_code','ix_shipping_method_translations_method_lang')
ORDER BY 1;

\echo == checks ==
SELECT conname FROM pg_constraint WHERE contype='c'
  AND conname IN ('ck_shipping_methods_code_canonical','ck_shipping_methods_option_code_canonical') ORDER BY 1;

\echo == fk ==
SELECT conname FROM pg_constraint WHERE contype='f' AND conname='fk_shipping_method_translations_method';

\echo == row counts (new vs legacy) ==
SELECT
  (SELECT count(*) FROM fulfillment.shipping_methods) AS methods,
  (SELECT count(*) FROM fulfillment.shipping_method_translations) AS method_translations,
  (SELECT count(*) FROM fulfillment.shipping_services) AS legacy_services,
  (SELECT count(*) FROM fulfillment.shipping_service_options) AS legacy_options;
