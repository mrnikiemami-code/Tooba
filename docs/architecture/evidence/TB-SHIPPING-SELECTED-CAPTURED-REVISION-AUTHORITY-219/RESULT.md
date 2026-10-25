# TB-SHIPPING-SELECTED-CAPTURED-REVISION-AUTHORITY-219 — RESULT

Mode: BOUNDED_SHIPPING_OWNED_SELECTED_CAPTURE_POINTER_AND_CONTRACTS_READ
Base: `4972c6c8af39c000bceaebe4f0846540ec37f107` (fetch verified, `HEAD == origin/main` before work)
Parent: TB-SHIPPING-CAPTURED-GROUP-PROJECTION-COMPOSER-218-R1 (`dfdeae8afb…`, review in
`218-R1/ARCHITECT-REVIEW.md`)

## Verdict

`BOUNDED_CAPABILITY_EXECUTED / NOT WIRED / NOT LIVE`.

Shipping now owns ONE versioned selected-capture pointer per **full owner + canonical service/option**, plus
a Contracts-only selected-revision reader. The pointer references an **already existing** TB-214 immutable
snapshot; nothing is captured by this slice, no payload is copied, and no legacy rate/packaging/schedule row
is written. Selection is **publication intent only** — not readiness, eligibility, customer permission or a
quotable-method claim.

Task text is persisted verbatim in `TASK.md` in this folder.

## What was added (one cohesive Shipping capability)

Domain — `Tooba.Shipping.Domain/Authoring/ShippingSelectedCapturedRevision.cs`
- `ShippingSelectedCaptureState { Cleared, Selected }`; identity = `SelectionId`, owner kind/tenant/seller,
  `BindingServiceKey`(required, canonical), `BindingOptionKey?(canonical)`, `SelectedSnapshotId?`,
  `SelectionVersion`, `UpdatedAt`.
- `CanonicalKey(value)` = trim + `ToLowerInvariant`, blank ⇒ `null` (single canonicalization policy).
- `CreateSelected(...)` ⇒ version **1**; `Select(...)` ⇒ no-op when the target is already selected, else
  version+1; `Clear(...)` ⇒ keeps the row, nulls the target, version+1 once. `NextVersion` rejects `<=0`
  and `long.MaxValue`; versions never reset to 0 (no ABA) and never decrease.

Persistence — `ShippingDbContext` (+ snapshot + ONE additive migration `20261025130021_SelectedCapturedRevision`)
- Table `shipping.selected_captured_revisions`; PK `selection_id`; `selection_version` is a real
  `IsConcurrencyToken()`; unique index `ux_selected_captured_revisions_owner_binding` over
  `(owner_kind, owner_tenant_id, owner_seller_party_id, binding_service_key, binding_option_key)` with
  `AreNullsDistinct(false)` so nullable owner/option columns cannot bypass one-pointer uniqueness.
- CHECKs: owner coherence per kind, `selection_version > 0`, canonical service key
  (`= lower(btrim(...))` and non-empty), canonical-or-null option key, non-empty selected snapshot id.
- No FK/navigation into another schema, no payload column, no legacy flattening.

Application port — `Tooba.Shipping.Application/Authoring/Ports/IShippingSelectedCapturedRevisionDirectory.cs`
- Internal write/read port: `SelectAsync(owner, service, option, draftId, sourceRevision,
  expectedSelectionVersion, ct)`, `ClearAsync(owner, service, option, expectedSelectionVersion, ct)` returning
  typed `ShippingSelectedCaptureSelection`. No public/HTTP/MediatR write contract, no browser-supplied owner.

Infrastructure adapter — `Tooba.Shipping.Infrastructure/Authoring/ShippingSelectedCapturedRevisionDirectory.cs`
- Registered **once scoped** in `ShippingModule.AddServices` and shared by the internal port and the
  Contracts reader (same scoped instance ⇒ one selection authority).
- Target resolution uses the EXISTING exact `GetCapturedRevisionAsync` + `ReadPayload()`; a missing/foreign/
  corrupt capture or a payload whose service/option differs from the request is refused (`snapshot_unavailable`
  / `binding_mismatch`) **before** any write. It never calls `CaptureRevisionAsync`.
- Binding persisted is **derived from the verified payload**, never from the caller's declaration.
- Concurrency: `expectedSelectionVersion == 0` is the only way to create (otherwise `selected_capture.conflict`);
  an existing row requires the exact version (stale ⇒ conflict, no last-write-wins, no auto-retry); same target
  at the current version is a truthful no-op preserving version **and** `UpdatedAt`; clear preserves the row and
  advances once when it actually changed, already-cleared is a no-op, absent+expected0 returns absent without
  inserting.
- Write safety: `RequireCleanTracker()` refuses unrelated pending caller work **before** writing (never flush/
  clear); one `SaveChanges` is the only atomic boundary (no manual transaction, 214 guards untouched); only the
  exact `23505` on `ux_selected_captured_revisions_owner_binding`/`selected_captured_revisions`/schema `shipping`
  maps to conflict, and `DbUpdateConcurrencyException` maps to conflict; the attempted pointer entry is detached
  on every failure path while the caller's exception/token identity propagate unchanged.

Contracts — `Tooba.Shipping.Contracts/Rates/ShippingSelectedCapturedRevisionContracts.cs`
- `IShippingSelectedCapturedRevisionLookup.ReadAsync(request, ct)` with typed request (owner kind + exact
  tenant/seller + service/option) — **no draft/latest/active field**. Result: `Verdict`
  (`Selected/Cleared/Absent/OwnerScopeUnavailable/SnapshotUnavailable/InvalidRequest`), stable reason codes,
  `State`, `SelectionVersion`, and for `Selected` exact `SnapshotId/DraftId/SourceRevision/FormatVersion/
  ServiceCode/OptionCode/ShippingTypeId` + stored translations (`ShippingSelectedCapturedTranslation`).
  Contracts-owned shape only — no Domain/JSON payload leak.
- Reads `AsNoTracking`, resolve the snapshot by exact id **and** the same owner, and re-verify the stored
  binding against the payload; corrupt/foreign/mismatched ⇒ `snapshot_unavailable`. No fallback to mutable
  draft, newest snapshot, config or another owner. Provider exception / caller cancellation propagate.

Owner-scope reuse — `Tooba.Shipping.Infrastructure/Authoring/ShippingCapturedOwnerScopeResolver.cs`
- The 216 owner/edition/active-tenant coherence mapping was extracted into ONE Shipping-internal helper now
  reused by 216 and 219 (no duplicate validator, no blanket Seller denial, owner identity stays **data**, not
  actor permission). `ShippingCapturedRevisionQuoteLookup.cs` only had its private helper replaced; its
  behaviour is unchanged (9/9 of its class re-executed green).

Error codes — `ShippingErrorCodes`: `shipping.selected_capture.conflict`,
`shipping.selected_capture.snapshot_unavailable`, `shipping.selected_capture.binding_mismatch`,
`shipping.selected_capture.invalid` (additive).

## Executed proof

Target: task-owned disposable `127.0.0.1:15432` / `shipping_tb214` / `tb214`
(`postgres:17`, one container created for this task, identity verified as `current_database()|current_user|
host(inet_server_addr())` before any DDL, container removed afterwards; older 55432 family and shared 5432
never touched). `TOOBA_TB214_POSTGRES` supplied only to the test process.

Class `Tooba.Shipping.Tests/Authoring/ShippingSelectedCapturedRevisionStorageTests.cs` — **9/9 passed**
(real PostgreSQL, normal migration chain applied to the disposable DB only):
1. Pointer table + `NULLS NOT DISTINCT` unique owner/binding index (no destination column) + all five CHECKs.
2. First selection ⇒ version 1 with exact snapshot/draft/revision provenance; a later draft advance leaves the
   selected read **exactly** the old immutable capture.
3. Select newer ⇒ v2; deliberate rollback to the older existing capture ⇒ v3 (rollback by selection, version
   never decreases).
4. Same target at current version ⇒ no-op preserving version **and** `UpdatedAt`; stale expected version ⇒
   `selected_capture.conflict` with nothing written; absent pointer creatable only from version 0.
5. Clear keeps the row/version (v1→v2, target null), a pre-clear version cannot resurrect the target, clearing
   again is a no-op, an explicit new selection at v2 ⇒ v3, and clear-absent at expected 0 returns absent without
   inserting.
6. Foreign-owner capture and never-captured revision ⇒ `snapshot_unavailable` with no snapshot created; a
   caller-declared binding the payload does not have ⇒ `binding_mismatch` and no pointer; `"  POST "`/`"Post"`
   are the SAME pointer (one row, `post`); independent Store owners keep separate pointers and each reads only
   its own selected snapshot.
7. Two independent contexts at the same expected version but different targets ⇒ exactly one winner, loser
   `selected_capture.conflict`, one row advanced once (1→2), no hybrid.
8. Sentinel fault at the write boundary ⇒ same exception instance, no pending entry, later `SaveChanges` = 0, no
   row; real caller cancellation ⇒ same token identity, no entry, later `SaveChanges` = 0, no row; unrelated
   dirty caller entity ⇒ refused **first**, its `Modified` state and tracking preserved, no pointer.
9. Contracts reader: absent binding ⇒ `Absent` (no latest fallback), blank binding ⇒ `InvalidRequest`, tenant/
   kind outside the trusted context ⇒ `OwnerScopeUnavailable` before any read, cleared ⇒ `Cleared` with no
   provenance, disposed context and a cancelled caller token propagate unchanged.

Build: `Tooba.Shipping.Tests` — 0 errors. Compatibility: the extracted owner resolver left
`ShippingCapturedQuoteContractsEdgeTests` at **9/9 passed** (the only other affected class; no wildcard/full
shipping/Host/Order/solution suite was replayed).

## Not done / limits (honest)

- **NOT WIRED, NOT LIVE.** No public HTTP route, MediatR command, Order consumer, projection/selection/commit
  hookup or admin publication UI. The 218/218-R1 composer still receives an explicit trusted server plan; it does
  **not** yet consume this reader.
- Selection is intent only: no Active/CanCheckout flag, no Store-only restriction, no seller=fulfillment
  inference, no quotability/readiness claim. Incomplete/Free/Carrier captures may be selected as data and the
  existing quote/refusal remains authoritative.
- Remaining missing authority for the next slice: real fulfillment owner/type/group-membership and physical
  measurement sources, plus the explicit admin publication action.
- `scopedDI` for the new registrations was verified by registration inspection (same scoped adapter for both
  interfaces), not by an executed container resolution.
- The migration was applied **only** to the disposable DB. It is **NOT** shared-applied (like the 214 snapshot
  migration). No Host/FE restart, no user draft/capture write, no shared DB change, no flag flip.
- Timebox: actual ≈29 min vs HARD30 / implementation-stop20 / final10 (over the implementation stop; recorded,
  not a process PASS). One correction round was used (test scoping to the task tenant + provider-failure probe).
  A Docker Desktop engine restart mid-run was re-verified before the final green run.

---

## Correction appendix — TB-219-R1 (2026-10-25, append-only)

Architect review (ARCHITECT-REVIEW.md) source-confirmed two write-correctness defects in this deliverable;
the architecture, table/index/CHECK shape, Contracts surface and owner resolver are retained.

1. SelectAsync/ClearAsync decided expected-version and the same-target / already-cleared no-ops from a
   tracking query, so a CLEAN STALE identity-map instance could report success from an OLD token (and a
   current token could be falsely rejected) after another context advanced the row.
2. ShippingSelectedCapturedRevision.Select/Clear assigned the target BEFORE computing NextVersion, so a
   rejected overflow changed the tracked row without advancing the version; no cleanup ran and a later
   SaveChanges could publish it.

Both are repaired in TB-219-R1 with executed RED (3 failed) then GREEN (12/12) real-PostgreSQL facts, and the
statement above that a rejected pointer "writes nothing" is only now true. See
../TB-SHIPPING-SELECTED-CAPTURED-REVISION-AUTHORITY-219-R1/RESULT.md. Nothing else in this report changes;
219 remains **NOT WIRED / NOT LIVE**, selection stays publication intent only, and the 214/219 migrations
remain NOT shared-applied.
