\pset pager off
SELECT 'methods' AS m, count(*)::text AS v FROM fulfillment.shipping_methods
UNION ALL SELECT 'translations', count(*)::text FROM fulfillment.shipping_method_translations
UNION ALL SELECT 'inactive_methods', string_agg(method_code, ',' ORDER BY method_code) FROM fulfillment.shipping_methods WHERE is_active=false
UNION ALL SELECT 'carts', count(*)::text FROM cart.carts
UNION ALL SELECT 'cart_lines', count(*)::text FROM cart.cart_lines
UNION ALL SELECT 'drafts', count(*)::text FROM "order".cart_shipping_drafts
UNION ALL SELECT 'seller_orders', count(*)::text FROM "order".seller_orders;
