# Prepared schema-only change — approval required

Target: existing local PostgreSQL127.0.0.1:5432, database tooba_alpha, connection alias tenant-alpha, configured tenant store-alpha. Fresh provenance must match before mutation; credentials stay private.
No Host execution/restart, no FE restart, no seed/backfill/business API write.

## Allowed migration universe

Shipping:
-20261022055446_AuthoringDraftPersistence: create drafts, groups, selected areas and keys/indexes/checks.
-20261022065333_AuthoringGroupRateRows: nullable currency column, rate rows and checks/index.
-20261022075711_AuthoringRateRowConstraintParity: replace five own currency/rate checks with canonical stricter definitions.
-20261022081351_AuthoringPresentationTranslations: create draft translations and group titles.
-20261022085811_AuthoringWeeklySchedule: create root week header and day rows.
-20261022093803_AuthoringMethodSettings: create independent setting pair table.
-20261022195929_AuthoringDraftTypeAssignment: nullable root shipping_type_id plus nonempty-id check.

Fulfillment:
-20261022101415_ShippingTypeCatalog: create own type and translation tables/constraints/indexes.
-20261022182208_AddFulfillmentOutboxPendingIndex: transactional nonunique partial index on fulfillment.outbox_messages(occurred_at), processed_at IS NULL AND dead_lettered_at IS NULL. May block writes while created; no same-name adoption allowed.

## Exact execution mechanism and fail-closed prerequisites

Use the existing current-source Tooba.MigrationRunner, not the Host and not a made-up module flag.
CLI supports `plan --tenant store-alpha` then `apply --tenant store-alpha`; application configuration is loaded from the runner's AppContext.BaseDirectory and environment. Resolve exact current configured target privately, never put credentials in command text/evidence.
Runner apply iterates ALL registered module descriptors. It is permitted only if:
1. Read-only complete current plan across ALL descriptors is successful and the total pending ids equal EXACTLY the nine above, with no missing/unlisted pending id, unknown module, changed schema or divergent history.
2. Target identity/edition/tenant is verified; exact fulfillment index is still absent, expected new object identities do not collide. Stop on drift, never adopt by name or edit history.
3. A usable local database backup is captured and verified before DDL, with secret-safe retained path and no backup bytes in git. This is recovery protection, not an automatic restore authorization.
4. Existing live Host is not running a concurrent migration; runner uses its existing database advisory lock. Bounded lock/statement waits; no force-kill of shared sessions.
5. Use current checked-in migrations and existing SDK/packages; if runner build needed, isolated output. No package upgrade, no migration edit, no global apply to all tenants.

Only then apply the nine via the existing tenant-scoped CLI. If complete pending set differs, STOP with the extra ids before any apply; permission does not extend to them.
Afterward read history/schema to verify Shipping13/Fulfillment12 with expected identities and no remaining pending on those modules; counts alone are not sufficient. On failure, capture exact completed history and error, do not retry/downgrade/restore blindly.
Keep Host5088/FE3000 untouched. Report exact schema changes, backup location, elapsed and source parity. Live read/write acceptance remains PENDING until later runtime refresh.
