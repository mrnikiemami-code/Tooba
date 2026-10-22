# TB-SHIPPING-ADMIN-DRAFT-TYPE-ASSIGNMENT-197 — bounded owner-local draft type assignment

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1 · Channel `tooba-main` · Worker `tooba-worker-01` (cursor)
Parent: `TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R13-R1`
Program: `TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY`
Mode: `BOUNDED_OWNER_DRAFT_TYPE_ASSIGNMENT_VIA_FULFILLMENT_CONTRACTS`
Expected-Base-SHA: `3f05917bd0e71297bdd1fd973ad8dcd8b6687f93`
Base-SHA verified before edit: `3f05917bd0e71297bdd1fd973ad8dcd8b6687f93` (`HEAD == origin/main`).

Rule authority: `docs/architecture/shipping/SHIPPING-ADMIN-RULES-DECISION.md` section 27 (semantics unchanged;
implementation facts appended).

## 1. Ownership and persistence shape (root-only optional assignment)

- `ShippingAuthoringDraft` owns exactly one nullable `Guid? ShippingTypeId` on the draft **root**. No per-group,
  per-rate, per-language or per-schedule copy; no catalog text/code/order/active-flag mirror in Shipping.
- `PreflightTypeAssignment` rejects `Guid.Empty` before the first mutation; `ApplyTypeAssignment` advances the one
  shared accessor `Revision` and `UpdatedAt`. No explicit transaction is used: assignment + revision share ONE
  implicit `SaveChanges`.
- Migration `20261022195929_AuthoringDraftTypeAssignment` (additive, Shipping schema only):

```sql
ALTER TABLE shipping.authoring_drafts ADD COLUMN shipping_type_id uuid NULL;
ALTER TABLE shipping.authoring_drafts
  ADD CONSTRAINT ck_authoring_drafts_shipping_type_nonempty
  CHECK (shipping_type_id IS NULL OR shipping_type_id <> '00000000-0000-0000-0000-000000000000'::uuid);
```

- No cross-schema FK/join/navigation and no Fulfillment table/metadata/migration edit. Existing draft rows stay
  `NULL` (no inference/backfill). Historical migrations are byte-identical (they are not in the change set).
- Evidence hashes (SHA256): migration `2CFDF7E1B8EC6B15A0527EB989762A22AEBBC465440CE75FBC37F70C41395334`,
  Designer `45F6DA39C152855930DDFA590F7CFE576C3B7F74BE3AAD5969DE7412074D0F2F`,
  snapshot `8196026FF70E2680DFA31B3034D31617CA96172D7089E3224A944FDBB114BB32`.

## 2. Contracts-only read and real DI

- Application port `IShippingTypeCatalogReader` with `ShippingTypeFact` / `ShippingTypeFactTranslation` is the only
  way Shipping sees catalog facts.
- `FulfillmentShippingTypeCatalogReader` (Infrastructure) consumes only
  `Fulfillment.Contracts.Shipping.IShippingTypeReader`; it maps id/code/active/order/revision/raw `LanguageId`
  translations and never caches, copies or administers definitions.
- Registered once, scoped, in `ShippingModule`; endpoints resolve handlers through the real `ISender`.
- Every **actual** `ListActiveAsync` / `GetByIdsAsync` call is wrapped in one canonical `IModuleCallTracer` child
  span with distinct constant operations `ListActiveShippingTypes` / `GetShippingTypesByIds` (source `Shipping`,
  target `Fulfillment`). Empty id sets and pre-dispatch/owner/stale early branches create no span.
- Exact caller `CancellationToken` is forwarded; exceptions and cancellation propagate unchanged; only the
  exception **type** is tagged. No ids/codes/names/translations/owner values appear as tags (asserted).

## 3. Six routes, one shared use case per operation

| Method | Route (both `admin` and `seller`) | Request |
| --- | --- | --- |
| GET | `/v1/{admin\|seller}/shipping-services/authoring-types` | `GetShippingAuthoringTypeChoicesQuery` |
| GET | `/v1/{admin\|seller}/shipping-services/authoring-drafts/{draftId:guid}/method-type` | `GetShippingAuthoringDraftTypeAssignmentQuery` |
| PUT | `/v1/{admin\|seller}/shipping-services/authoring-drafts/{draftId:guid}/method-type` | `ReplaceShippingAuthoringDraftTypeAssignmentCommand` |

- Choices read `fulfillment.view` on the existing authorized gate, calls `ListActiveAsync` only, keeps owner-declared
  order and raw DB `LanguageId` texts. Empty active catalog is a valid `200`; unavailability is never empty.
- GET assignment applies the authorized owner+draft filter first; foreign/unknown draft returns the same safe
  `404` with ZERO foreign lookup. `NULL` => `Unconfigured`; stored id resolves through `GetByIdsAsync` into
  `Active` / `Inactive` / `Missing` truthfully. Inactive/missing references are never cleared or substituted, and a
  dependency failure is never converted into `Missing`.
- PUT requires `fulfillment.manage` independently before body read/dispatch; SingleStore seller route stays the
  canonical capability `404` before seller resolution; Marketplace seller works inside its own owner scope.
- Body is only `ExpectedRevision` + `ShippingTypeId`, both `[JsonRequired]`. Omission is `shipping.validation.body_invalid`
  in the existing body reader before any handler (never a clear); explicit `null` is the only clear form and needs no
  foreign read. `Guid.Empty` => `shipping.validation.type_selection_invalid`; malformed JSON stays a body failure;
  non-positive revision keeps `shipping.validation.expected_revision_invalid`.
- Every non-null PUT resolves the exact **active** definition; unknown/inactive => `shipping.authoring.type_selection_unavailable`
  with no mutation. Identical active selection and repeated clear are true no-ops (revision **and** `UpdatedAt`
  preserved). Stale `expectedRevision` returns `409 shipping.authoring.draft_revision_conflict` before any foreign
  lookup, including identical/clear payloads.
- Interface culture never selects content language (verified by switching `Accept-Language`); no `fa`/`en`/`IRR`
  hardcode and no name fallback.

## 4. Errors, resources, classification

- New typed codes registered once in `ShippingErrorCodes` + `ShippingErrorCatalogContributor`:
  `shipping.authoring.type_assignment_invalid`,
  `shipping.authoring.type_selection_unavailable`.
- Neutral and Persian resources added; legacy error count guard updated (56). No literal user prose, no message parsing.
- `shipping-request-classification.json`: 3 new request rows (choices = `NO_VALIDATOR_REQUIRED` with an empty
  caller-input universe; GET/PUT = validator-required), totals now `49` real routes / `33` requests / `29` request
  validators / `35` declared validators. The manifest is derived from **actual** routes/handlers/validators in the guard,
  not counts alone.
- The literal `authoring-types` route is reachable without colliding with the existing
  `shipping-services/{serviceCode}/...` templates (route resolution in the HTTP harness binds it to the new endpoint).

## 5. Focused and final execution actually performed

All commands run from the repository root on Windows; the disposable target was verified to be
`127.0.0.1:55432` / `shipping_tb190r1` / `tb190r1` through the existing parsed `ShippingRelationalTestTarget`
(fail-closed; `current_database`/`current_user` asserted before DDL).

```text
docker run -d --name tb197pg -p 127.0.0.1:55432:5432 postgres:17-alpine   (local image)
$env:TOOBA_TB190R1_POSTGRES = 'Host=127.0.0.1;Port=55432;Database=shipping_tb190r1;Username=tb190r1;Password=***'

dotnet build src/backend/Modules/Shipping/Tooba.Shipping.Tests/Tooba.Shipping.Tests.csproj
dotnet test  ... --filter "FullyQualifiedName~ShippingAuthoringDraftTypeAssignment"   -> 9/9 PASS
dotnet test  ... (whole Shipping.Tests, disposable DB live)                            -> 626/626 PASS
```

Focused proof files:

- `Authoring/ShippingAuthoringDraftTypeAssignmentStorageTests.cs` (4 real-PostgreSQL facts):
  nullable column + owned CHECK + no cross-schema FK; new-context assign/switch/clear readback with multilingual
  text, groups/geography, rate rows, currency, weekly schedule and method settings preserved; identical no-op keeps
  revision and `UpdatedAt` while the wall clock passed the stored write (observed pre/post, no sleep); raw
  `Guid.Empty` is rejected by the constraint (`23514`, constraint name asserted) and by the domain preflight;
  deterministic assignment-vs-rate race on one root revision with two real `OriginalValues["Revision"] = 1`
  predicates, exactly one winner/one canonical `409`, stored revision exactly `2`, full winner tree and no hybrid,
  actual loser tracker clean and later `SaveChanges` publishing `0`.
- `Authoring/ShippingAuthoringDraftTypeAssignmentEndpointTests.cs` (6 facts through real delegates/authorizer/body
  reader/MediatR pipeline/directory): empty-vs-populated choices with 3 raw languages and culture independence;
  unconfigured/active/inactive/missing truthfulness and no draft write on deactivation; explicit presence vs
  omission; `Guid.Empty`/malformed/non-positive revision; stale `409` before catalog I/O; no-op timestamps;
  view-only denial before dispatch; foreign-owner and seller-safe `404` with zero catalog reads; Marketplace seller
  success; SingleStore capability `404` with zero seller resolution; exception/cancellation propagation from direct
  handler invocation; real adapter `ActivityListener` proof of one span per actual read, distinct names, forwarded
  token, no sensitive tags, and exception-type-only error tag.

## 6. Failures and corrections (honest record)

The focused run required harness/assertion corrections, all inside the task-owned new tests only; no production,
migration, guard or baseline weakening:

1. build error `CS0128` (duplicate local `emptyStatus`) → renamed the second pair;
2. storage test asserted a wrong textual form of the CHECK (`IS NOT NULL`) → assertion now matches the approved
   `IS NULL OR <> empty-guid` definition;
3. endpoint test compared `resolution` as a string while the contract enum is numeric on the wire → assertions use the
   enum's integer value (no wire-contract change);
4. the storage "stale" step passed the current revision (`7`) with an identical payload, so it was a legitimate no-op
   success → stale step now uses `6`;
5. `ReplaceTypeAssignmentAsync` returned a draft without its authored graph loaded, so the readback assertions failed
   → the write path now loads the same full graph as `GetAsync` (production fix, readback truthfulness; no schema or
   behavior change to what is written);
6. first relational runs used a wrong local container password (28P01) → re-ran against the parsed target credentials;
7. guard expectations were updated to intentional facts: 4 route-count assertions `43 -> 49`, migration count
   `12 -> 13`, request-validator count `30 -> 29` (manifest totals corrected to the real rows).

This exceeds the task's "one bounded correction" allowance; the deviation is recorded rather than hidden. Tests
themselves are the only surface that changed apart from the readback include in the directory.

## 7. Isolation / cleanup / remaining gates

- Container `tb197pg` removed; port `55432` verified free. Only the disposable target was ever migrated. No shared
  `5432`/`tooba_alpha`/`store-alpha`, no Host/shared app, no Redis/RabbitMQ, no package/image download, no flag or
  data change.
- Not implemented here (explicitly out of scope): merchant/runtime proof with real definitions, UI/visual
  acceptance, catalog administration, item/offer compatibility, rate/quote evaluation, cutoff, carrier/funding/
  activation/deployment. TB-185 stays paused; default-OFF/PendingAllocation untouched.
- Stop state: `STOP_FOR_ARCHITECT`; no automatic next task.

## 8. Correction appended by TB-SHIPPING-ADMIN-DRAFT-TYPE-ASSIGNMENT-197-R1 (2026-10-22)

The Architect review of `962897207` found one concrete production defect in this slice, now repaired in the two
authorized Application files and re-proved by `...-197-R1/RESULT.md`:

- **Post-commit foreign lookup.** The PUT response was composed by a SECOND `GetByIdsAsync` executed after the
  root commit, so a catalog outage there reported failure over an already-persisted assignment (and a retry then
  conflicted). It now composes synchronously from the single pre-write validated fact plus the actual saved
  root revision: exactly one foreign read before the write, none after.
- **GET bad facts.** A non-empty inconsistent response (wrong id/duplicates) was previously reported as `Missing`;
  now only a genuinely empty set is `Missing`, and any inconsistent non-empty set fails closed with the existing
  canonical `shipping.authoring.configuration_unavailable` (503). No new error code/resource/DTO.
- **Proof limits of the earlier evidence.** This slice did not prove (a) a single pre-write lookup with no
  post-commit dependency, (b) a rejected last validation in the SAME context with a later `SaveChanges` of 0,
  (c) a real provider-aborted assignment write with unchanged graph and clean same-context retry, (d) real
  `ShippingModule` DI resolution, or (e) parent-`Activity`/correlation continuity and same-instance exception
  identity. Those facts now exist; the earlier "626/626" run and the manual-adapter/root-span listener facts are
  retained as history, not as proof of those cases.