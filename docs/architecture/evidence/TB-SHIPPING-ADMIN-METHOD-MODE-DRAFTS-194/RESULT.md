# TB-SHIPPING-ADMIN-METHOD-MODE-DRAFTS-194 — Result

Date: 2026-10-22
Channel: tooba-main · Worker: tooba-worker-01 · Agent: cursor
Expected-Base-SHA: `06737dd7428ee96af351af22c62f4389f83fabc3`
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_INDEPENDENT_FREIGHT_AND_CALCULATION_DRAFT_SETTINGS
Stop-State: STOP_FOR_ARCHITECT · Automatic-Next-Task: NONE

Status: see the Worker result contract posted through Bridge. `Worker PASS != Architect ACCEPT`.

## 1. Base verification

Fetched `origin/main`; `HEAD` was the accepted TB-193-R1 checkpoint `fce7fab30`, a strict ancestor of
the expected base; fast-forwarded only (`git merge --ff-only origin/main`) to
`06737dd7428ee96af351af22c62f4389f83fabc3`. No reset/rebase/stash/force-push. Unrelated dirty and
untracked evidence files were preserved and are not part of the code commit.

## 2. Bounded capability implemented

Two **independent** method-root draft selections owned once by the draft aggregate:

- `shipping.authoring_method_settings` (one row per draft, PK `draft_id`):
  - `freight_payment_mode` — reuses the existing `Tooba.Shipping.Contracts.Packaging.ShippingFreightPayerMode`
    (`Prepaid = 0`, `CollectOnDelivery = 1`), no second catalog or wire convention.
  - `calculation_mode` — new typed `Tooba.Shipping.Contracts.Authoring.ShippingAuthoringCalculationMode`
    (`Manual = 0`, `CarrierApi = 1`, `Free = 2`). `CarrierApi` is a requested mode only; no carrier identity,
    credentials, quote, availability or readiness is inferred. `Free` neither zeroes costs nor infers a funder;
    stored manual rows are configuration and remain untouched.
  - `updated_at`; `fk_authoring_method_settings_draft ... ON DELETE CASCADE`;
    CHECKs `ck_authoring_method_settings_freight_mode (0,1)` and
    `ck_authoring_method_settings_calculation_mode (0,1,2)`; both columns NOT NULL, so SQL NULL/UNKNOWN
    cannot bypass a required choice and an unknown integer is never coerced to a defined enum member.
- Absent row = explicitly **UNCONFIGURED**, never implicit `Manual`/`Prepaid`.
- Domain: `PreflightMethodSettings` validates both explicit values and definedness **before** any mutation and
  reports `HasChanges`; `ApplyMethodSettings` creates/updates the row, advances the SAME root `Revision` once and
  sets `UpdatedAt`. Identical PUT is a truthful no-op (no revision/timestamp advance); a stale
  `expectedRevision` is the canonical 409 even for an identical payload.
- Application: one query + one command/handler shared by both audiences; handlers map to
  `ShippingAuthoringMethodSettingsItem(DraftId, Revision, IsConfigured, FreightPaymentMode?, CalculationMode?)`.
- Endpoints (admin and seller): `GET|PUT /v1/{admin|seller}/shipping-services/authoring-drafts/{draftId:guid}/method-settings`.
  Authorization runs through the real owner authorizer BEFORE `ISender` dispatch (`fulfillment.view` for GET,
  `fulfillment.manage` for PUT); SingleStore seller stays canonically unavailable before seller resolution.
  Owner/seller/edition/tenant/currency/language/activation are never bound from transport.
- Validators: route id non-empty, `expectedRevision > 0`, and BOTH choices explicitly present and defined
  (`shipping.validation.method_settings_freight_mode_invalid`,
  `shipping.validation.method_settings_calculation_mode_invalid`). Direct non-HTTP invocation is also
  rejected (`shipping.authoring.method_settings_invalid`, one descriptor + neutral/fa resources).

## 3. Executed proof (disposable PostgreSQL 17, 127.0.0.1:55432 / shipping_tb190r1 / tb190r1)

Target parsed fail-closed from `ShippingRelationalTestTarget`; `current_database()`/`current_user` verified
before any write. Fresh task-owned container `tb194-pg`; no shared `:5432`/`tooba_alpha` use.

- `ShippingAuthoringMethodSettingsEndpointTests` — 4/4, through the real route delegates, real owner
  authorizer, real body reader, real MediatR validation, real handlers and real directory over the real
  `ShippingDbContext`:
  1. unconfigured GET (no defaults) + all six combinations + single-axis switch + truthful no-op +
     stale-revision 409 (`shipping.authoring.draft_revision_conflict`) + unchanged revision.
  2. missing / null / unknown / non-positive-revision payloads → 400 `validation.failed` with the exact
     validation codes, `HandlerReached == 0`, revision/`UpdatedAt` unchanged, still unconfigured; malformed JSON
     follows canonical body-reader behaviour; a direct same-context rejected call throws before any tracked
     write and leaves an empty non-Unchanged tracker.
  3. setting changes preserve geography, manual rows/amounts, USD currency binding, Persian text and the
     seven-day weekly schedule, and a subsequent region write preserves the settings row in return.
  4. view-only admin PUT denied before dispatch (`SentCount == 0`, `HandlerReached == 0`); foreign owner safe
     404 with no change; Marketplace seller success through the real seller gate; scoped seller grant denied
     `shipping.authoring.owner_scope_denied` pre-dispatch; SingleStore seller 404
     `shipping.authoring.owner_scope_unavailable` with zero seller-resolution calls.
- `ShippingAuthoringMethodSettingsStorageTests` — 3/3, repeated three consecutive times (all passing):
  1. round-trip through a NEW context, unconfigured readback, first write revision 1→5 style advance, no-op
     no-advance, single-axis switch, and preservation of rates/currency/text/schedule across a later region
     reorder (and vice versa).
  2. parameterized raw negatives, each rolled back: NULL freight (`23502`, column `freight_payment_mode`),
     NULL calculation (`23502`, column `calculation_mode`), unknown freight (`23514`,
     `ck_authoring_method_settings_freight_mode`), unknown calculation (`23514`,
     `ck_authoring_method_settings_calculation_mode`), duplicate PK (`23505`,
     `pk_authoring_method_settings`), invalid parent (`23503`, `fk_authoring_method_settings_draft`).
  3. deterministic settings-vs-rate race on the SAME root revision: barrier at each participant's FIRST
     `SavingChanges` (after reading revision 1 and staging, before any write), `Arrivals == 2`, both ORIGINAL
     revisions asserted `== 1`; EXACTLY one success and one `shipping.authoring.draft_revision_conflict`;
     persisted root revision EXACTLY 2; winner-dependent settings/rate state asserted in both directions; raw
     row counts `authoring_method_settings = 1|0` and `authoring_rate_rows = 0|1`; the ACTUAL loser context has
     no non-Unchanged tracked entry and re-reads the winner's revision 2 state.
- Whole Shipping suite on the disposable target: **616/616 passed, 0 failed, 0 skipped** (609 accepted
  baseline + 7 new). Focused projects only; no Host.Tests or solution-wide build.

## 4. Migration and parity

One additive migration `20261022093803_AuthoringMethodSettings` + designer + snapshot. Generated forward,
idempotent SQL pinned at `forward.sql`. No historical migration/designer edited (byte parity by source
identity), no legacy table mutation, no backfill, no cross-module FK, no startup migrator. Migrations were
applied only to the disposable test target.

## 5. Isolation and cleanup

Container `tb194-pg` removed; `127.0.0.1:55432` verified closed before reporting. No credential logging, no
shared DB/app restart, no package install, no baseline refresh, no guard weakening. Unrelated dirty files and
historical evidence artifacts preserved.

## 6. Explicit non-implementation

Carrier integration/identity/quote/coordinates, free cost/funder accountability, runtime evaluation, customer
quote/promise, activation/publication, taxonomy/cutoff, Admin UI integration and production functional
acceptance remain OPEN/PENDING. TB-185 stays paused.
