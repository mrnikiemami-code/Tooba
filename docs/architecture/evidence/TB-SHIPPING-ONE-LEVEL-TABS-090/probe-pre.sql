\pset pager off
\echo == fulfillment migration history (tail) ==
SELECT "MigrationId" FROM fulfillment.__ef_migrations_history ORDER BY 1 DESC LIMIT 12;

\echo == shipping_services columns ==
SELECT column_name, data_type FROM information_schema.columns
WHERE table_schema='fulfillment' AND table_name='shipping_services' ORDER BY ordinal_position;

\echo == shipping_service_options columns ==
SELECT column_name, data_type FROM information_schema.columns
WHERE table_schema='fulfillment' AND table_name='shipping_service_options' ORDER BY ordinal_position;

\echo == services ==
SELECT * FROM fulfillment.shipping_services ORDER BY sort_order, code;

\echo == options ==
SELECT * FROM fulfillment.shipping_service_options ORDER BY 1;

\echo == counts ==
SELECT
  (SELECT count(*) FROM fulfillment.shipping_services) AS services,
  (SELECT count(*) FROM fulfillment.shipping_service_options) AS options,
  (SELECT count(*) FROM fulfillment.shipping_service_translations) AS service_translations,
  (SELECT count(*) FROM fulfillment.shipping_service_option_translations) AS option_translations;

\echo == one-level tables present? ==
SELECT table_schema || '.' || table_name FROM information_schema.tables
WHERE table_name IN ('shipping_methods','shipping_method_translations');
