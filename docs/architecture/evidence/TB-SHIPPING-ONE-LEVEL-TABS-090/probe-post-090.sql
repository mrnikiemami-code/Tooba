\pset pager off
\echo == post-090 one-level catalog (committed backfill) ==
SELECT method_code, legacy_service_code, legacy_option_code, is_active, sort_order
FROM fulfillment.shipping_methods ORDER BY sort_order, method_code;

\echo == post-090 method translations ==
SELECT m.method_code, t.language_id, t.name
FROM fulfillment.shipping_method_translations t
JOIN fulfillment.shipping_methods m ON m.shipping_method_id = t.shipping_method_id
ORDER BY m.sort_order, m.method_code, t.language_id;

\echo == migration history tail ==
SELECT migration_id FROM fulfillment.__ef_migrations_history ORDER BY 1 DESC LIMIT 3;

\echo == legacy catalog unchanged ==
SELECT
  (SELECT count(*) FROM fulfillment.shipping_services) AS legacy_services,
  (SELECT count(*) FROM fulfillment.shipping_service_options) AS legacy_options,
  (SELECT count(*) FROM fulfillment.shipping_service_translations) AS legacy_service_translations,
  (SELECT count(*) FROM fulfillment.shipping_service_option_translations) AS legacy_option_translations;

\echo == historical commerce rows unchanged ==
SELECT
  (SELECT count(*) FROM fulfillment.fulfillments) AS fulfillments,
  (SELECT count(*) FROM fulfillment.shipments) AS shipments,
  (SELECT count(*) FROM "order".cart_shipping_drafts) AS drafts;
