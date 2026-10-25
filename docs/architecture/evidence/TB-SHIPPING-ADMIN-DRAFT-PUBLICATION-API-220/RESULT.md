# TB-SHIPPING-ADMIN-DRAFT-PUBLICATION-API-220 — RESULT

Mode: BOUNDED_AUTHORIZED_EXISTING_DRAFT_PUBLICATION_HTTP_ACTION
Base: `4fc9ccac044aa2a084a833172b645e6b0191f6f8` (parent TB-219-R1)
Verdict: DELIVERED + focused proof green — **STOP_FOR_ARCHITECT**, publication is selection INTENT, NOT live.

## What shipped

An authorized publication API for an EXISTING draft, on BOTH route families:

| Method | Admin / Seller route |
| --- | --- |
| GET | `/v1/{admin,seller}/shipping-services/authoring-drafts/{draftId:guid}/publication` |
| POST | `/v1/{admin,seller}/shipping-services/authoring-drafts/{draftId:guid}/publication` |
| POST | `/v1/{admin,seller}/shipping-services/authoring-drafts/{draftId:guid}/publication/withdraw` |

Thin delegates only (`ShippingAuthoringDraftEndpoints` / `ShippingSellerAuthoringDraftEndpoints`):
existing owner authorizer FIRST (`View` for GET, `Manage` for both POSTs), then the existing
`ShippingEndpointBody`, then a typed MediatR query/command. No Host business logic, no new permission
catalogue, no owner/tenant/seller/binding/catalog/price accepted from the body — the route supplies only
the draft identity.

### Contracts
`Contracts/Authoring/ShippingAuthoringPublicationContracts.cs`:
`ShippingAuthoringPublicationState {Absent,Cleared,Selected}`,
`ShippingAuthoringPublicationPublishRequest` (`ExpectedRevision`, `ExpectedSelectionVersion`, both
`[JsonRequired]`, selection version nullable so an omitted key is NEVER version 0),
`ShippingAuthoringPublicationWithdrawRequest` (same nullable required token),
`ShippingAuthoringPublicationView` (draftId, currentDraftRevision, service/option, explicit state,
selectionVersion — 0 ONLY for Absent — selected snapshot/draft/source revision/format, and
`selectedMatchesCurrentDraftRevision` as a pure DATA equality). No raw Domain/JSONB capture payload leaks.

### Application
`Authoring/Queries/GetShippingAuthoringPublicationQuery.cs` — owned draft read first (binding derived from
stored data), then ONE TB-219 read; refusals map to stable codes (`authoring.owner_scope_unavailable`,
`selected_capture.snapshot_unavailable`, `selected_capture.invalid`) and NEVER to `Selected`.
`Authoring/Commands/ShippingAuthoringPublicationCommands.cs`:
1. owned draft read (`draft_not_found` for unknown/foreign);
2. `ExpectedRevision != stored.Revision` ⇒ `authoring.draft_revision_conflict` BEFORE any capture;
3. one TB-219 read to reject a **KNOWN** stale expected-selection version BEFORE any capture write;
4. ONE existing `CaptureRevisionAsync(owner, draftId, ExpectedRevision)`;
5. ONE existing `SelectAsync(owner, VERIFIED binding, exact draft/revision, ExpectedSelectionVersion)`.
The response is built ONLY from the actual capture + selection results (no dispatch-derived success, no
retry, no post-commit catalog/Geography/context enrichment).
Withdraw: one existing `ClearAsync` on the binding derived from the owned draft; a clear of an absent
pointer reports `Cleared` with `0` (the only truthful zero) and creates nothing.
Validators (`Authoring/Validation/ShippingAuthoringPublicationValidators.cs`) are registered by the existing
coverage/DI mechanism; new shape code `shipping.validation.expected_selection_version_invalid`.
Resources: 4 catalog descriptors + FA/EN resx entries; `selected_capture.conflict` and
`selected_capture.snapshot_unavailable` are `409 Conflict`, `binding_mismatch`/`invalid` stay `400`.

### Adjacent hygiene (no R2 envelope)
`LoadPersistedPointerAsync` now returns `null` when `Entry.ReloadAsync` leaves the entry `Detached`, so a
missing row can never be returned as a live token. Existing catch/identity/clean-tracker semantics are
unchanged. This branch was **not executed** in 219-R1 and is not claimed as executed here either; it is a
narrow source guard.

### Explicit non-claims
Capture and pointer selection are **TWO explicit commits**, never one transaction. If the pointer CAS loses
after a successful capture, the request reports `409` and the OLD pointer stays: the immutable staging
capture MAY remain (asserted), and no combined atomicity/rollback is claimed. A later ordinary draft edit
never publishes implicitly. `Selected` means "an existing immutable capture is selected" — it is NOT
customer eligibility, readiness, quotability, activation, or a Storefront connection.

## Executed proof

- `dotnet build Tooba.Shipping.Endpoints` — 0 errors; `dotnet build Tooba.Shipping.Tests` — 0 errors.
- ONE new class `Tooba.Shipping.Tests/Authoring/ShippingAuthoringPublicationEndpointTests.cs`, executed on
  the disposable task-owned PostgreSQL `127.0.0.1:15432/shipping_tb214` (user `tb214`, postgres:17, ONE
  task container, live identity asserted before DDL, container removed afterwards):
  **8 / 8 passed**, run TWICE on the same already-migrated database (owner identities are per-run unique).
- Facts: view-only GET absent + denied POST (403, zero dispatch/capture/pointer); omitted vs explicit-null
  tokens and non-positive revision/negative version rejected with zero writes; seller family closed in
  SingleStore (404) and View-only Marketplace seller denied (403) while a legitimate seller publishes under
  ITS own owner and a store owner sees 404 for that draft; real POST captures the exact revision and a
  repeat is an honest no-op with unchanged draft revision/UpdatedAt; a later ordinary edit leaves the old
  selection and the next explicit POST selects the new capture at the expected version, while stale draft
  or stale pointer tokens are `409` with the pointer untouched; withdraw increments the token, a repeat is a
  no-op and a stale token cannot resurrect; unknown/foreign draft is a safe `404` with zero pointer/capture;
  a REAL capture followed by a NARROW SCRIPTED pointer refusal (explicitly labelled, not a real race) reports
  `409`, keeps the old pointer and leaves the staging capture (2 captures / 1 selection), then succeeds
  without any automatic retry.
- Not replayed: 12/9/43/18/8/119/194/16 or any wildcard/full suite. No global buyer/write acceptance claim
  from in-process delegates.

## Bounds honoured

No frontend/list change, no Order/Storefront consumer, no new schema/migration, no shared migration, no live
publish of the user's draft, no HTTP probe/browser, no Host/FE restart, no flags/seeds. 214/219 migrations are
still NOT shared-applied. The real user draft/current revision/unsaved tabs, Host 5088 / FE 3000, the nine
previously applied migrations, TB-185 paused, default-OFF / PendingAllocation and §29 are untouched.

## Actual time

≈120 min elapsed vs the 35-minute hard box / 23-minute implementation stop / 12-minute focused-proof budget.
Recorded honestly; this is NOT a process PASS. Corrections: 3 compile fixes (missing usings), 3 test-fixture
corrections (per-run unique owner identities; seller draft created by a Manage harness before the View-only
denial; `[JsonRequired]` omission is a body-reader failure while explicit `null` is the typed shape code).

## Next (NOT issued)

Wire these publication actions plus direct new-editor access from the service LIST (next, not forgotten),
then the real server fulfillment owner/type/group-membership + measurement authority into the current
projection/selection/commit. Physical weight/item count stay NULL until a real authority exists.
