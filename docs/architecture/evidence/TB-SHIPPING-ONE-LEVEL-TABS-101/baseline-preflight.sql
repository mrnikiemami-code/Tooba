\pset pager off
SELECT now() AS captured_at;
SELECT sm.method_code, sm.is_active, sm.legacy_service_code, sm.legacy_option_code, tr.language_id::text, tr.name
FROM fulfillment.shipping_methods sm LEFT JOIN fulfillment.shipping_method_translations tr ON tr.shipping_method_id=sm.shipping_method_id
ORDER BY sm.method_code, tr.language_id;
SELECT s.code, s.is_active, o.code AS option_code, o.is_active AS option_active
FROM fulfillment.shipping_services s LEFT JOIN fulfillment.shipping_service_options o ON o.shipping_service_id=s.shipping_service_id
ORDER BY s.code, o.code;
SELECT l.code, l.culture, l.is_default FROM localization.languages l ORDER BY l.culture;
