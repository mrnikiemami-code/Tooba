# 01 — Seeding policy

Task: TB-P06-T024-R2

## Guard

| Rule | Implementation |
|------|----------------|
| Development only | `AccessControlDevelopmentSeed.EnsureAsync` runs from Host startup only when `IHostEnvironment.IsDevelopment()` |
| Never Production | No Production registration; Production Host skips seed |
| Idempotent | Deterministic emails, role code, category FA names, product slugs, order idempotency keys; re-run reuses existing rows |
| Canonical owners | Catalog / Offer / Inventory / Cart / Checkout / Order / Party / Identity / AccessControl contracts |
| No auth bypass | Membership + ACC role assignment + SpiceDB/InMemory member + capability tuple sync — same paths as runtime |
| No fake disconnected IDs | Snapshot publishes real persisted IDs via `AccessControlDemoSnapshot` + `GET /v1/admin/access-control/demo-preview` |

## Entry points

- `src/backend/Host/Tooba.Host/AccessControl/AccessControlDevelopmentSeed.cs`
- Wired from `Program.cs` after storefront demo catalog bootstrap (try/catch so Host stays up if seed fails)

## Deterministic keys

| Kind | Value |
|------|-------|
| Employee email | `seller-employee-mobile@tooba.local` |
| Role code | `mobile-order-op` |
| Categories | `دمو کنترل دسترسی` → `موبایل` / `کتاب` |
| Product slugs | `acc-demo-mobile-phone`, `acc-demo-books-novel` |
| Order idempotency | `acc-demo-seed-mobile-v1`, `acc-demo-seed-books-v1`, `acc-demo-seed-mixed-v1` |
| Inventory locations | `WH-ACC-MOB`, `WH-ACC-BOOK` |
