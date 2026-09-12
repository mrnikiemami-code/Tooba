# TB-P10-T004-R12 — Deployment readiness

- `Tooba.MigrationRunner` includes Catalog + Payment (and the rest of the module list). R10 files:
  - `20260912080000_AddStoreHoldPolicySettings`
  - `20260912080000_AddUnpaidTimeoutAndMethodHolds`
  are ordinary EF migrations (idempotent via `__ef_migrations_history`).
- Host boot with `Tooba:CatalogDemo:RunLegacyBootstraps=false` does **not** migrate on startup; production apply path is MigrationRunner, not a task fixture.
- Defaults: platform JSON hold hours; store/method tables empty ⇒ inherit. Existing Orders/Payments remain readable (`unpaid_timeout_at` nullable).
- Historical recovery is an explicit Admin operation (`recover_inventory_reservation`), not a startup mutation.
- No destructive rewrite migrations in R2–R11 payment/order/hold set.
