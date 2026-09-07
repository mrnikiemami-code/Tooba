# Runtime E2E

Host restarted after build unlock. Probes:

- http://127.0.0.1:5088 → HTTP 200
- http://127.0.0.1:3000/admin/orders → HTTP 200

Apply pending migrations on Host startup for `fulfillment.items.quantity_packed` and `order.order_lines` return snapshot columns.

Domain/composer unit tests cover pack/partial/unpack/cancel/split/return snapshot matrix.
