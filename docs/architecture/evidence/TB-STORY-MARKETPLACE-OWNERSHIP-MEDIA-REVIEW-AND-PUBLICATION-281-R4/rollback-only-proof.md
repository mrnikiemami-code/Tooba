# TB-281-R4 PostgreSQL rollback-only proof

Harness: Host tests on canonical `tooba_alpha` via `ToobaNpgsql.ConfigureModuleContext`. No second DB, no EnsureCreated DDL, no seed.

## C1 — Items + publication + isolation

`StoryMarketplace281R4RelationalTests.C1_persisted_items_publication_visibility_and_seller_isolation_rollback_only`

- Explicit `StoryItems.Add` for Items collection persistence
- Publishable positive; future/expired/unreviewed/empty negative
- Seller isolation + admin shop-name filter/paging/count in same transaction
- finally rollback; connection probe `committedRows=0` for test-owned ids

## C2 — tenant-scoped occurrence + stale

`C2_tenant_scoped_occurrence_duplicate_and_stale_version_zero_write`

- Two tenants same OccurrenceKey allowed by composite PK
- Same-tenant duplicate via raw SQL INSERT → 23505
- Stale expectedVersion → typed refusal, zero extra occurrence/mutation rows
- finally rollback; committedRows=0

## C3 — Media ambient + save-fail

`C3_media_ambient_tx_refused_and_save_fail_interceptor_on_relational`

- Ambient transaction → typed refusal before store
- Save-fail interceptor path on relational Media context with recording store
- finally rollback; committedRows=0

## Limits

- Rollback-only; no committed two-writer stress harness
- Provider limits disclosed in test output when applicable
