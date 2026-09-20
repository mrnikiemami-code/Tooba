# Data migration

- Additive CREATE TABLE IF NOT EXISTS order.checkout_processes + indexes
- No backfill of historical checkouts required
- Down drops table; no destructive rewrite of checkouts/orders
- Compatibility: existing data unaffected
