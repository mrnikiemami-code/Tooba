# TB-SHIPPING-ADMIN-ATOMIC-EDITOR-DOCUMENT-199 — RESULT

Task: TB-SHIPPING-ADMIN-ATOMIC-EDITOR-DOCUMENT-199
Parent: TB-SHIPPING-ADMIN-EDITOR-INTEGRATION-CONTRACT-198
Mode: BOUNDED_EXISTING_DRAFT_ATOMIC_EDITOR_DOCUMENT_IMPLEMENTATION
Base SHA (verified `HEAD == origin/main`): `8211c5e43eb9cf5a8a265c99f78c5c68f6758af9`
Channel: tooba-main · Worker: tooba-worker-01
Production-Functional-Acceptance: PENDING · Stop-State: STOP_FOR_ARCHITECT

## Delivered capability (existing draft only)

Four additive routes, ONE query + ONE command handler shared by both audiences:

```
GET  /v1/admin/shipping-services/authoring-drafts/{draftId:guid}/editor-document
PUT  /v1/admin/shipping-services/authoring-drafts/{draftId:guid}/editor-document
GET  /v1/seller/shipping-services/authoring-drafts/{draftId:guid}/editor-document
PUT  /v1/seller/shipping-services/authoring-drafts/{draftId:guid}/editor-document
```

- Real owner gate BEFORE `ISender` and BEFORE write-body parsing (`fulfillment.view` for GET,
  `fulfillment.manage` for PUT, independently). Full owner+draft filter; unknown/foreign draft is the same
  safe `shipping.authoring.draft_not_found`; SingleStore seller is the canonical early 404 before any seller
  resolution. No owner/tenant/seller/edition/currency authority is read from the transport.
- GET returns ONE consistent own authored root snapshot: groups (identity/order/currency), areas, rate rows,
  method translations, group titles, weekly schedule, method settings, stored type id with truthful
  resolution, revision and timestamps, plus the authoritative language registry/default.

## Wire presence semantics (as implemented)

| Field | Absent / null | `[]` | Supplied object |
| --- | --- | --- | --- |
| `regionGroups` | shape failure (`validation.failed`) | valid (whole ordered collection) | whole replacement |
| `methodTranslations` | shape failure | no change | selected-language patch |
| `weeklySchedule` | PRESERVE | n/a | replace (7 ISO days + timezone) |
| `methodSettings` | PRESERVE (unconfigured stays unconfigured) | n/a | replace both independent axes |
| `typeAssignment` | PRESERVE (stored id only observed) | n/a | `shippingTypeId:null` CLEAR, `Guid.Empty` invalid, non-null needs exactly one ACTIVE fact |

Group-level: `groupId` null/absent = new server id; `areas` required; `rows` required (`[]` valid);
`titles` required patch list (`[]` no change, explicit null title clears). New groups carry nested rows and
titles in the SAME PUT.

## Domain/plan strategy

- `PreflightEditorDocument` builds detached prospective groups (`ShippingAuthoringRegionGroup.Create`) for
  brand-new groups so nested rates and titles of the same PUT are validated BEFORE any tracked mutation.
- Shared mutation cores (`ApplyRegionGroupsCore`, `ApplyGroupRateRowsCore`, `ApplyPresentationCore`,
  `ApplyWeeklyScheduleCore`, `ApplyMethodSettingsCore`, `ApplyTypeAssignmentCore`) do not advance the root.
  `ApplyEditorDocument` invokes them and performs exactly ONE `Revision = checked(Revision + 1)`.
- Honest no-op: unchanged identity/order/areas/rows/text/schedule/settings/type ⇒ `plan.HasChanges == false`
  ⇒ no write, `Revision`/`UpdatedAt` preserved. Stale token still wins over an identical payload.
- Currency binding is never cleared by `[]`; a different trusted currency is the existing mismatch rejection.
- `RegionsDiffer` now compares the persisted tree by its `Order` (0..n-1) instead of the incidental
  materialisation position, so an identical document loaded from the database is a truthful no-op.

## Atomic persistence (one commit, one transaction begin site)

- The reviewed staged-save boundary is extracted into ONE private typed helper
  `SaveAuthoringReplacementAsync(draft, regionPlan?, editorPlan?, ct)` inside
  `ShippingAuthoringDraftDirectory`, called only by `ReplaceRegionGroupsAsync` and
  `ReplaceEditorDocumentAsync`. Exactly ONE `BeginTransactionAsync` site remains; staged order (phase 1) then
  final zero-based order, ONE commit, rollback + tracker cleanup on failure/cancellation.
- `ShippingTransactionBoundary` guard and its compatibility facts were updated only for that exact member /
  caller universe (no filename exemption, no broadened allowlist, no removed negative).

## Executed proof (real endpoint delegates + real PostgreSQL 17)

Executed counts (this run): `ShippingAuthoringEditorDocumentDomainTests` 4, `ShippingAuthoringEditorDocumentEndpointTests` 4,
`ShippingAuthoringEditorDocumentStorageTests` 4 on the disposable `127.0.0.1:55432 / shipping_tb190r1 / tb190r1`
instance (parsed, fail-closed target with live `current_database()`/`current_user` check), then the WHOLE
Shipping suite ONCE with the live DB: **645 passed, 0 failed, 0 skipped**.

- Endpoint: composite admin PUT (new group with nested row+title, reorder, method text, settings, schedule,
  type) advances the root exactly once; identical PUT is a no-op preserving revision AND `UpdatedAt`; stale
  PUT is `409 shipping.authoring.draft_revision_conflict`; omitted optional wrappers preserve stored
  schedule/settings/type/text; reversed interval, unavailable language, duplicate node, empty type id are
  400 with exact codes and no write; malformed/absent-collection payloads never reach a handler (per-request
  handler deltas asserted); unknown draft and another store's draft are both 404 for GET and PUT; seller route
  (Marketplace, no tenant) shares the same use case and advances one revision.
- Storage (new context proof): ONE composite PUT creates a new group with a rate row AND title, reorders the
  retained group and changes sections — exact whole winning tree, stable retained ids, revision exactly old+1;
  identical document at an advanced clock keeps revision and `UpdatedAt`; late-invalid child after valid
  earlier edits leaves the same context tracker empty, `SaveChanges()==0`, persisted tree/timestamp unchanged;
  unknown group id and unknown row id rejected before any write; valid retry at the ORIGINAL revision succeeds
  from the same context and advances once.
- Storage cross-capability race on the SAME root revision (bounded first-`SavingChanges` two-arrival barrier
  after both read and before writes): both captured `OriginalValues[Revision]==1`, exactly one success and one
  canonical 409, new revision 2, winner-specific complete tree with no hybrid, actual loser tracker empty and
  `SaveChanges()==0`.
- Storage rollback: failing the SECOND staged `SaveChanges` and caller cancellation (real caller CTS) at the
  same boundary both leave a fresh-context unchanged tree at revision 1 with no staged residue (`group.Order`
  set still `{0}`), clean writing tracker, and a retry at the original revision succeeds (revision 2).
- Post-commit I/O: language/currency context, option/geography validation and type facts are all acquired
  BEFORE the write; responses are composed from the saved root plus those pre-read facts (no fallible re-query
  after commit — the reused TB-197 handler-composition proof plus the ordering above).
- Guard/registry: route/request/handler/validator set equality updated to 53 shipped routes / 35 reachable
  requests (31 validator-required, 4 without), module structure/error/transaction guards retained.

Build: `Tooba.Shipping.Tests` and its Shipping references build with 0 errors. A whole-solution build was not
used as proof because the running `Tooba.Host` process holds output DLL locks (environmental, unrelated).

## Changed files

- `src/backend/Modules/Shipping/Tooba.Shipping.Contracts/Authoring/ShippingAuthoringEditorDocumentContracts.cs` (new)
- `src/backend/Modules/Shipping/Tooba.Shipping.Domain/Authoring/ShippingAuthoringDraft.cs`
- `src/backend/Modules/Shipping/Tooba.Shipping.Application/Authoring/Ports/IShippingAuthoringDraftDirectory.cs`
- `src/backend/Modules/Shipping/Tooba.Shipping.Application/Authoring/Models/ShippingAuthoringEditorDocumentModels.cs` (new)
- `src/backend/Modules/Shipping/Tooba.Shipping.Application/Authoring/Queries/GetShippingAuthoringEditorDocumentQuery.cs` (new)
- `src/backend/Modules/Shipping/Tooba.Shipping.Application/Authoring/Queries/GetShippingAuthoringTypeQueries.cs`
- `src/backend/Modules/Shipping/Tooba.Shipping.Application/Authoring/Commands/ReplaceShippingAuthoringEditorDocumentCommand.cs` (new)
- `src/backend/Modules/Shipping/Tooba.Shipping.Application/Authoring/Validation/ShippingAuthoringDraftValidators.cs`
- `src/backend/Modules/Shipping/Tooba.Shipping.Infrastructure/Authoring/ShippingAuthoringDraftDirectory.cs`
- `src/backend/Modules/Shipping/Tooba.Shipping.Endpoints/Admin/ShippingAuthoringDraftEndpoints.cs`
- `src/backend/Modules/Shipping/Tooba.Shipping.Endpoints/Seller/ShippingSellerAuthoringDraftEndpoints.cs`
- `src/backend/Modules/Shipping/Tooba.Shipping.Tests/Authoring/ShippingAuthoringEditorDocumentDomainTests.cs` (new)
- `src/backend/Modules/Shipping/Tooba.Shipping.Tests/Authoring/ShippingAuthoringEditorDocumentEndpointTests.cs` (new)
- `src/backend/Modules/Shipping/Tooba.Shipping.Tests/Authoring/ShippingAuthoringEditorDocumentStorageTests.cs` (new)
- `src/backend/Modules/Shipping/Tooba.Shipping.Tests/Authoring/ShippingAuthoringContextEndpointTests.cs`
- `src/backend/Modules/Shipping/Tooba.Shipping.Tests/Authoring/ShippingAuthoringOwnerEndpointTests.cs`
- `src/backend/Modules/Shipping/Tooba.Shipping.Tests/Certification/ShippingRequestClassificationTests.cs`
- `src/backend/Modules/Shipping/Tooba.Shipping.Tests/Compatibility/ShippingTransactionBoundary.cs`
- `docs/architecture/shipping/shipping-request-classification.json`
- `docs/architecture/shipping/SHIPPING-ADMIN-RULES-DECISION.md` (implementation facts appended)

## Deviations / remaining gaps

- One domain defensive fix was required by the storage no-op proof: `RegionsDiffer` compares persisted groups
  by `Order` (not EF materialisation position). No behavior/business rule changed.
- Not executed: a dedicated "catalog throws on any post-write lookup" fault-injection fact. The no-post-commit
  I/O property is argued by construction (all foreign facts are read before the single write, response built
  from saved root + pre-read facts) and by the reused TB-197 composition proof, not by a new throwing fixture.
- UI, full-method creation workflow, live merchant registry acceptance and runtime activation remain out of
  scope and unclaimed (Production-Functional-Acceptance: PENDING).

## Cleanup

Task container `tb199pg` removed; `127.0.0.1:55432` verified free (`port55432_free=True`). No shared
database, port 5432, image download or unrelated service touched. No credentials logged.

Shared-Database-Data-Flag-Changes: NONE
Automatic-Next-Task: NONE
