# Runtime smoke

Postgres `postgres-db` / `tooba_alpha` admin/123456. Catalog EF `20260909130000` applied by MigrationRunner. Offer history was stale (`return_policy_choice` already exists); remaining T013 SQL applied + `__ef_migrations_history` rows. DB not wiped.

Host `:5088` health 200 after stopping PID 1340 DLL lock.

## Product kg DecimalPlaces=2 Step=null

`01a05387-fbd0-7000-acd3-4382ce92c773` unit `kg`, places 2, step null.

## Cart 1.25

POST `/v1/storefront/cart` then add offer `01a03826-9936-7000-b499-ff26a6123a8c` qty 1.25.
Cart `01a083d3-9147-7000-bf44-9487dda17459` line `01a083d3-91c3-7000-ae69-08082e453ed0` quantity 1.250000. `itemCount` 1.25, `unitCode` kg.

## Reservation 1.25

`inventory.reservations` `01a083d3-91d5-7000-a835-46fcf5b40abd` quantity 1.250000 Held.

## Pack 0.50

`fulfillment.items` accepts `quantity_packed = 0.500000` (transaction rolled back). Domain pack 0.50 of 1.25 covered in Host.Tests.

## Rounding

GET `/v1/admin/settings/quantity-rounding` → Nearest / نزدیک‌ترین مقدار.
