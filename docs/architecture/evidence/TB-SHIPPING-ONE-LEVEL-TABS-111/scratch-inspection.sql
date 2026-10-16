\pset pager off
\echo == migration history applied per module schema (correct table name __ef_migrations_history) ==
SELECT 'catalog' AS m, count(*) AS applied FROM catalog.__ef_migrations_history
UNION ALL SELECT 'offer', count(*) FROM offer.__ef_migrations_history
UNION ALL SELECT 'pricing', count(*) FROM pricing.__ef_migrations_history
UNION ALL SELECT 'inventory', count(*) FROM inventory.__ef_migrations_history
UNION ALL SELECT 'tax', count(*) FROM tax.__ef_migrations_history
UNION ALL SELECT 'party', count(*) FROM party.__ef_migrations_history
UNION ALL SELECT 'promotion', count(*) FROM promotion.__ef_migrations_history
UNION ALL SELECT 'localization', count(*) FROM localization.__ef_migrations_history
UNION ALL SELECT 'support', count(*) FROM support.__ef_migrations_history
ORDER BY m;
\echo == catalog product (slug_seam, status, kind) ==
SELECT slug_seam, status, kind FROM catalog.products ORDER BY slug_seam;
\echo == catalog variants (catalog_code_seam, is_default, status) ==
SELECT catalog_code_seam, is_default, status, combination_fingerprint
FROM catalog.variants ORDER BY catalog_code_seam;
\echo == published product has >=1 default variant? ==
SELECT p.slug_seam, p.status AS product_status,
       count(v.variant_id) AS variants,
       count(*) FILTER (WHERE v.is_default) AS defaults
FROM catalog.products p LEFT JOIN catalog.variants v ON v.product_id = p.product_id
GROUP BY p.slug_seam, p.status ORDER BY p.slug_seam;
\echo == offers / prices / stock ==
SELECT 'offers' AS m, count(*)::text AS v FROM offer.offers
UNION ALL SELECT 'active_offers', count(*)::text FROM offer.offers WHERE status = 'Active'
UNION ALL SELECT 'prices', count(*)::text FROM pricing.prices
UNION ALL SELECT 'active_base_prices', count(*)::text FROM pricing.prices WHERE status = 'Active' AND qualifier_kind = 'Base'
UNION ALL SELECT 'stock_positions', count(*)::text FROM inventory.stock_positions
UNION ALL SELECT 'stock_positive_on_hand', count(*)::text FROM inventory.stock_positions WHERE on_hand > 0
UNION ALL SELECT 'seller_parties', count(*)::text FROM party.parties
UNION ALL SELECT 'locations', count(*)::text FROM inventory.locations
UNION ALL SELECT 'tax_rules', count(*)::text FROM tax.rules;
\echo == per-variant sellable chain (published product only) ==
SELECT v.catalog_code_seam,
       v.is_default,
       (SELECT count(*) FROM offer.offers o WHERE o.catalog_variant_id = v.variant_id) AS offers,
       (SELECT count(*) FROM offer.offers o WHERE o.catalog_variant_id = v.variant_id AND o.status = 'Active') AS active_offers,
       (SELECT count(*) FROM pricing.prices p JOIN offer.offers o2 ON o2.offer_id = p.offer_id
          WHERE o2.catalog_variant_id = v.variant_id AND p.status = 'Active' AND p.qualifier_kind = 'Base') AS active_base_prices,
       (SELECT coalesce(sum(s.on_hand - s.reserved), 0) FROM inventory.stock_positions s
          WHERE s.catalog_variant_id = v.variant_id) AS available
FROM catalog.variants v
JOIN catalog.products p ON p.product_id = v.product_id
WHERE p.slug_seam = 'schema-mobile-demo-phone'
ORDER BY v.catalog_code_seam;
\echo == carts / orders / shipping (must be empty on scratch) ==
SELECT 'carts' AS m, count(*)::text AS v FROM cart.carts
UNION ALL SELECT 'seller_orders', count(*)::text FROM "order".seller_orders
UNION ALL SELECT 'shipping_methods', count(*)::text FROM fulfillment.shipping_methods;
