SELECT 'one_level' AS src, sm.method_code, tr.language_id::text, tr.name
FROM fulfillment.shipping_methods sm
JOIN fulfillment.shipping_method_translations tr ON tr.shipping_method_id = sm.shipping_method_id
WHERE sm.method_code IN ('post:standard','post:express') ORDER BY sm.method_code, tr.language_id;
SELECT 'legacy_service' AS src, s.code, t.language_id::text, t.name
FROM fulfillment.shipping_services s JOIN fulfillment.shipping_service_translations t ON t.shipping_service_id = s.shipping_service_id
WHERE s.code IN ('post','snapp_courier') ORDER BY s.code, t.language_id;
SELECT 'legacy_option' AS src, s.code || ':' || o.code AS method_code, t.language_id::text, t.name
FROM fulfillment.shipping_service_options o
JOIN fulfillment.shipping_services s ON s.shipping_service_id = o.shipping_service_id
JOIN fulfillment.shipping_service_option_translations t ON t.shipping_service_option_id = o.shipping_service_option_id
WHERE s.code = 'post' ORDER BY method_code, t.language_id;
