\pset pager off
SELECT now() AS captured_at;
SELECT count(*) AS methods, (SELECT count(*) FROM fulfillment.shipping_method_translations) AS translations FROM fulfillment.shipping_methods;
SELECT sm.method_code, sm.is_active, tr.language_id::text, tr.name FROM fulfillment.shipping_methods sm
LEFT JOIN fulfillment.shipping_method_translations tr ON tr.shipping_method_id=sm.shipping_method_id
WHERE sm.method_code IN ('post:standard','post:express') ORDER BY sm.method_code, tr.language_id;
SELECT (SELECT count(*) FROM cart.carts) AS carts, (SELECT count(*) FROM cart.cart_lines) AS cart_lines, (SELECT count(*) FROM order.cart_shipping_drafts) AS drafts;
SELECT count(*) AS seller_orders FROM "order".seller_orders;
