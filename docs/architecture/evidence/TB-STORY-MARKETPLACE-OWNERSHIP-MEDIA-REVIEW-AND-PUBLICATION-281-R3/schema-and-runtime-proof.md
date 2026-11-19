# Schema and runtime proof (TB-281-R3)

## Schema

- Parent 281/R1 migrations immutable.
- Additive Story migration `20261119020000_Tb281R3TenantScopedCommandOccurrences`: PK `command_occurrences` → `(tenant_id, occurrence_key)`.
- Pre-apply dump verified; applied once by MigrationRunner; post pendingCount=0 for Story.

## Runtime

- Host refreshed from `artifacts/publish-primary` with `TOOBA_SKIP_DEVELOPMENT_BOOTSTRAP=1`.
- PID/dll/health recorded in RECEIPTS.md.
- ZERO live Story/Media business mutation.
