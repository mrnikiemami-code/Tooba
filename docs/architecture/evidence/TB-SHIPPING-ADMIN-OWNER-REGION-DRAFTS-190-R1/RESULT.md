# TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190-R1 — RESULT (worker)

Protocol: BRIDGE-WAKE-V1 · Channel: tooba-main · Worker: tooba-worker-01
Parent: TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190
Mode: BOUNDED_DRAFT_VALIDATION_PREFLIGHT_AND_RELATIONAL_REPLACE_REPAIR
Expected-Base-SHA: 4b5da2b018b64aa14a227c0942d3f6ad14c1b323
Status: BLOCKED (single, precise architectural conflict; repairs implemented and proven)
Hard timebox: 45 minutes (reached)

## 1. Input shape (R190-01) — repaired and proven

`CreateShippingAuthoringDraftCommandValidator` / `ReplaceShippingAuthoringDraftRegionsCommandValidator` now
separate the unconditional `NotNull` required rule from the dependent size/shape rules. The old trailing
`.When(x => x.Request?.RegionGroups is not null)` also skipped `NotNull`, so an omitted/null JSON collection
reached the handler.

- `RegionGroups` required rule is unconditional except for the parent `Request` guard.
- `[]` stays a valid incomplete draft.
- Size/shape rules run only when the collection is present.
- Both handlers additionally refuse an explicit `null` collection with the stable
  `shipping.authoring.region_payload_invalid` code instead of normalising it to empty or letting an NRE escape
  (defense in depth for direct, non-HTTP invocation).

HTTP regression (real endpoints, real pipeline, real validation behaviour, EF InMemory store):
`A_missing_region_group_collection_is_rejected_before_dispatch_and_persists_nothing`
- POST omitted `regionGroups` → 400 `validation.failed` + `shipping.validation.region_groups_required`
- POST `"regionGroups": null` → same
- POST `[]` → 201 (incomplete draft, revision 1, zero groups)
- PUT omitted / null → same required failure, stored revision and tree untouched
- dispatch counter proves no handler ran for any rejected request; store stays empty for rejected POSTs.

Direct validator facts: null parent request is body-invalid (no throw); null `Areas`, null area element and
`Guid.Empty` group id keep the existing stable shape codes.

## 2. Whole-tree preflight (R190-02) — repaired and proven

`ShippingAuthoringDraft.PreflightRegionGroups` validates the complete proposal with no mutation: shape,
retained-ID membership, repeated retained IDs, unknown/foreign IDs, node validity and draft-wide node
uniqueness. Only then does `ApplyRegionGroups` touch retained groups, orders, selections, revision or
timestamps.

`A_bad_last_identity_rejects_without_touching_the_same_aggregate_or_tracker`:
- first retained group carries a real valid change (different area), the LAST identity is unknown → rejected
- same aggregate instance, identical group/area snapshot, same revision and `UpdatedAt`
- every `ChangeTracker` entry is `Unchanged`, and a subsequent `SaveChangesAsync()` in the SAME context
  publishes nothing (verified through a fresh readback)
- repeated retained ID is rejected the same way
- a valid replacement afterwards succeeds and keeps the retained group identity

## 3. Ordered relational replacement (R190-03) — repaired and proven on real PostgreSQL

Persistence strategy: PostgreSQL 17, real Npgsql, real module migrations, real `ShippingDbContext` and
`ShippingAuthoringDraftDirectory`. Because `ux_authoring_region_groups_draft_order` is an IMMEDIATE unique
index, a valid swap has no legal first ordinary UPDATE. The repair stages a temporary non-conflicting order
(zero-based final order + `StagedOrderBase`), then writes the final zero-based order, both inside ONE
Shipping-owned transaction with one commit and rollback on every failure. Selected-node moves reparent the
SAME `authoring_selected_areas` row (UPDATE, not delete+insert), so `(draft_id, node_kind, node_id)`
uniqueness is never transiently violated. No schema change was required, so no corrective migration was
added; TB-190 and older migrations are untouched.

Executed relational facts (4/4 pass, `TOOBA_TB190R1_POSTGRES`, disposable container, loopback 55432):
- `Reorder_delete_shift_insert_before_and_node_move_preserve_identity_and_final_uniqueness`
  swap A@0,B@1,C@2 → C@0,B@1,A@2; delete-first shift 1→0,2→1; insert-before retained groups; node move
  between groups keeps the same selected-area row id and its new group id; final orders exactly 0..n-1;
  no staged order or duplicate node survives.
- `A_same_revision_race_leaves_exactly_one_winner_and_no_partial_group_or_area_writes`
  two independent contexts at revision 1: exactly one success, loser gets
  `shipping.authoring.draft_revision_conflict`, no loser group/area rows, no staged orders, loser tracker empty.
- `A_failure_after_the_first_stage_rolls_the_whole_replacement_back`
  a test-owned interceptor fails the SECOND SaveChanges (after the staged write): previous tree, revision 1
  and area ownership intact on a new-context readback, then a valid replacement from that revision succeeds.
- `The_real_handler_creates_replaces_and_refuses_a_stale_revision_through_npgsql`
  real create/replace handlers through Npgsql; stale retry returns the canonical conflict and stores nothing.

Isolation: dedicated container `tooba-shipping-tb190r1` (`postgres:17-alpine`),
`127.0.0.1:55432`, database `shipping_tb190r1`, task user. The test fixture fails closed if the connection
string is absent or mentions any shared database. Shared :5432 / tooba_alpha untouched. Credentials are test
locals and are never logged by production code. Container removed after the run.

## 4. Focused commands and counts

```
dotnet build …/Tooba.Shipping.Tests.csproj                                      -> 0 errors
dotnet test … --filter ShippingAuthoringDraftRepairTests                        -> 8/8 pass
dotnet test … --filter ShippingAuthoringDraftEndpointTests                      -> 4/4 pass
dotnet test … --filter ShippingAuthoringDraftRelationalTests (PostgreSQL)       -> 4/4 pass
dotnet test … (whole Tooba.Shipping.Tests)                                      -> 538 pass / 1 fail / 539
```

## 5. BLOCKING CONFLICT (why this is not PASS)

The only failing test is the pre-existing
`OperationalQuoteCompatibilityTests.The_operational_slice_adds_no_migration_and_no_money_movement`. It scans
the whole Shipping module for the literal `BeginTransaction`/`DbTransaction` and asserts the set is empty.
The task explicitly authorises (as a narrow exception to TB-190) a single Shipping-owned transaction for the
staged replacement; that authorised code is the scanner's only offender.

This task forbids refreshing baselines or softening guards to accept a repair, so the guard was NOT edited and
no baseline was refreshed. Two Architect options:
1. authorise a bounded update of that legacy scanner to permit the single module-owned replacement
   transaction, or
2. authorise the alternative deferred-uniqueness corrective migration (replace the immediate unique index
   with an equivalent `DEFERRABLE INITIALLY DEFERRED` unique constraint) so the single-SaveChanges preference
   can be restored without any explicit transaction.

Everything else requested by R1 is implemented and proven. No frontend, Storefront, Host, Contracts signature,
route, permission, activation, rate or language surface was touched. No new error codes or resources were added.
