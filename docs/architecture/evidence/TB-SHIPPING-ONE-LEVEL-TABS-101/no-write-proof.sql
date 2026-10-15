\pset pager off
SELECT 'cart.carts' AS t, max(created_at) AS max_created, max(updated_at) AS max_updated FROM cart.carts
UNION ALL SELECT 'order.cart_shipping_drafts', NULL, max(updated_at) FROM "order".cart_shipping_drafts
UNION ALL SELECT 'fulfillment.shipping_methods', max(created_at), max(updated_at) FROM fulfillment.shipping_methods;
