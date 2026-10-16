\pset pager off
SELECT 'methods' AS m, count(*)::text AS v FROM fulfillment.shipping_methods
UNION ALL SELECT 'translations', count(*)::text FROM fulfillment.shipping_method_translations
UNION ALL SELECT 'carts', count(*)::text FROM cart.carts
UNION ALL SELECT 'cart_lines', count(*)::text FROM cart.cart_lines
UNION ALL SELECT 'drafts', count(*)::text FROM "order".cart_shipping_drafts
UNION ALL SELECT 'seller_orders', count(*)::text FROM "order".seller_orders
UNION ALL SELECT 'products', count(*)::text FROM catalog.products
UNION ALL SELECT 'offers', count(*)::text FROM offer.offers;
