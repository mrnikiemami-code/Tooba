\pset pager off
\echo '--- variants of the demo product ---'
SELECT v.variant_id, v.catalog_code_seam, v.is_default, v.status, v.combination_fingerprint
FROM catalog.variants v JOIN catalog.products p ON p.product_id=v.product_id
WHERE p.slug_seam='schema-mobile-demo-phone' ORDER BY v.catalog_code_seam;
\echo '--- variant attribute values ---'
SELECT v.catalog_code_seam, d.code AS attribute_code, av.canonical_value
FROM catalog.variant_attribute_values av
JOIN catalog.variants v ON v.variant_id=av.variant_id
JOIN catalog.products p ON p.product_id=v.product_id
JOIN catalog.attribute_definitions d ON d.definition_id=av.definition_id
WHERE p.slug_seam='schema-mobile-demo-phone' ORDER BY 1,2;
\echo '--- product variant axes ---'
SELECT d.code AS axis_code, pa.display_order
FROM catalog.product_variant_axes pa
JOIN catalog.products p ON p.product_id=pa.product_id
JOIN catalog.attribute_definitions d ON d.definition_id=pa.definition_id
WHERE p.slug_seam='schema-mobile-demo-phone' ORDER BY 2;
\echo '--- effective axis bindings for the primary category ---'
SELECT d.code AS attribute_code, b.is_variant_axis, d.is_active, d.value_kind
FROM catalog.category_attribute_bindings b
JOIN catalog.attribute_definitions d ON d.definition_id=b.definition_id
WHERE b.category_id=(SELECT pc.category_id FROM catalog.product_categories pc JOIN catalog.products p ON p.product_id=pc.product_id WHERE p.slug_seam='schema-mobile-demo-phone' AND pc.role=0 LIMIT 1)
ORDER BY d.code;
