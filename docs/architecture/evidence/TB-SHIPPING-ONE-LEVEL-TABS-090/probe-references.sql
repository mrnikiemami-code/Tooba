\pset pager off
\echo == service translations ==
SELECT s.code, t.language_id, t.name
FROM fulfillment.shipping_services s
JOIN fulfillment.shipping_service_translations t ON t.shipping_service_id = s.shipping_service_id
ORDER BY s.sort_order, s.code, t.language_id;

\echo == option translations ==
SELECT s.code AS service_code, o.code AS option_code, o.is_active AS option_active, o.sort_order AS option_sort,
       ot.language_id, ot.name
FROM fulfillment.shipping_service_options o
JOIN fulfillment.shipping_services s ON s.shipping_service_id = o.shipping_service_id
LEFT JOIN fulfillment.shipping_service_option_translations ot ON ot.shipping_service_option_id = o.shipping_service_option_id
ORDER BY s.sort_order, o.sort_order, o.code, ot.language_id;

\echo == languages referenced ==
SELECT DISTINCT language_id FROM fulfillment.shipping_service_translations
UNION SELECT DISTINCT language_id FROM fulfillment.shipping_service_option_translations;

\echo == cart drafts referencing shipping method codes ==
SELECT shipping_method_code, count(*) FROM "order".cart_shipping_drafts GROUP BY 1 ORDER BY 1;

\echo == fulfillment shipments/consolidated codes ==
SELECT shipping_method_code, count(*) FROM fulfillment.shipments GROUP BY 1 ORDER BY 1;

\echo == legacy codes referenced by fulfillment units ==
SELECT shipping_method_code, count(*) FROM fulfillment.fulfillments GROUP BY 1 ORDER BY 1;
