\pset pager off
\echo '--- demo product status ---'
SELECT slug_seam, status, kind FROM catalog.products WHERE slug_seam = 'schema-mobile-demo-phone';
\echo '--- variants of demo product (default/status/fingerprint) ---'
SELECT v.catalog_code_seam, v.is_default, v.status, v.combination_fingerprint
FROM catalog.variants v
JOIN catalog.products p ON p.product_id = v.product_id
WHERE p.slug_seam = 'schema-mobile-demo-phone'
ORDER BY v.catalog_code_seam;
\echo '--- variant attribute values ---'
SELECT v.catalog_code_seam, d.code AS attribute_code, av.canonical_value
FROM catalog.variant_attribute_values av
JOIN catalog.variants v ON v.variant_id = av.variant_id
JOIN catalog.products p ON p.product_id = v.product_id
JOIN catalog.attribute_definitions d ON d.definition_id = av.definition_id
WHERE p.slug_seam = 'schema-mobile-demo-phone'
ORDER BY v.catalog_code_seam, d.code;
\echo '--- declared variant axes ---'
SELECT a.display_order, d.code AS axis_code
FROM catalog.product_variant_axes a
JOIN catalog.products p ON p.product_id = a.product_id
JOIN catalog.attribute_definitions d ON d.definition_id = a.definition_id
WHERE p.slug_seam = 'schema-mobile-demo-phone'
ORDER BY a.display_order;
\echo '--- default count on demo product (must be exactly 1) ---'
SELECT count(*) FILTER (WHERE v.is_default) AS defaults,
       count(*) AS total_variants
FROM catalog.variants v
JOIN catalog.products p ON p.product_id = v.product_id
WHERE p.slug_seam = 'schema-mobile-demo-phone';
\echo '--- catalog totals ---'
SELECT 'products' AS m, count(*)::text AS v FROM catalog.products
UNION ALL SELECT 'published_products', count(*)::text FROM catalog.products WHERE status = 'Published'
UNION ALL SELECT 'variants', count(*)::text FROM catalog.variants
UNION ALL SELECT 'product_history_entries', count(*)::text FROM catalog.product_history_entries;
\echo '--- offer/price/party/inventory table presence ---'
SELECT to_regclass('offer.offers') AS offers_table, to_regclass('offer.prices') AS prices_table,
       to_regclass('party.parties') AS parties_table,
       to_regclass('inventory.stock_items') AS stock_items_table;
\echo '--- schemas present ---'
SELECT nspname FROM pg_namespace
WHERE nspname NOT LIKE 'pg_%' AND nspname <> 'information_schema'
ORDER BY nspname;
