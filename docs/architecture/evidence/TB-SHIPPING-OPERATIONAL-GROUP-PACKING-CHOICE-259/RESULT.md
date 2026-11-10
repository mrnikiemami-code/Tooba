# TB-259 — RESULT

    Task          : TB-SHIPPING-OPERATIONAL-GROUP-PACKING-CHOICE-259
    Parent-Task   : TB-SHIPPING-NONDELIVERY-RETURN-AND-RESEND-258-R3
    Channel       : tooba-main
    Worker-Id     : tooba-worker-01
    Repository    : mrnikiemami-code/Tooba (branch main)
    Expected base : fbf24b6aec52a62960227d57a40f3bbe45879e04
    Code-SHA      : 4c71a668e
    Evidence-SHA  : this evidence commit; the ledger commit that follows it names it and is the merge HEAD
    Decision      : OPTIONAL_COMMON_PACKAGE_OR_INDEPENDENT_PARCELS_INSIDE_ONE_SERVER_OWNED_COMPATIBLE_GROUP_WITH_THE_CHOICE_INSIDE_THE_IMMUTABLE_OCCURRENCE_ENVELOPE
    Status        : PASS within the issued issue-4 scope
    Explicitly    : NOT Architect ACCEPT, NOT user VISUAL/FUNCTIONAL ACCEPT, NOT the five-defect completion

## 1. What the defect was

`MarketplaceOperationalGroupWorkflow` had exactly ONE physical outcome: it created the constituent member
parcels and then ALWAYS consolidated them into one common package. There was no way for an operator to
keep the same lawful operational group (same proven origin, destination/window commitment, accepted
method/service/option, currency and transport) while sending each member's parcel on its own tracking and
dispatch lifecycle. Grouping (what the customer bought together and how it must be operated) was fused to
physical packaging (one box or several), so compatibility silently implied consolidation.

## 2. What landed

### A. The choice is a first-class, validated part of the immutable envelope (Order Contracts/Domain)

* `OperationalGroupPackingMode { Common = 0, Separate = 1 }` and `OperationalGroupPackingModes` live in the
  Order Contracts shipping authority next to the other workflow tokens. `TryParse` maps an absent/blank
  token to `Common` (the historical meaning) and accepts only `"common"`/`"separate"`; anything else is a
  typed refusal `order.operational_group.workflow_packing_mode_invalid` with zero writes — never a silent
  fallback to one of the two lawful choices.
* `OperationalGroupOccurrenceEnvelopeCodec.CanonicalEnvelopeJson` emits `"packingMode":"separate"` ONLY for
  the separate choice. A common envelope stays byte-identical to the TB-253-R4 canonical JSON, so every
  already recorded occurrence keeps its exact fingerprint and replays unchanged, and `ReadPackingMode` on a
  legacy row answers `Common`. Historical occurrences are therefore not reinterpreted.
* `OrderOperationalGroupOccurrence.CompleteSeparate(method, now)` records the separate completion shape:
  `ConsolidatedPackageId = null`, `PackageNumber = null`, `ShippingMethodCode` kept, `State = Completed`.
  A null package id is the recorded RESULT of this shape, never evidence of pending work.
* `OrderOperationalGroupOccurrenceStore.RecordCompletedSeparateAsync` persists it on the existing owning
  occurrence table, and `GetAsync` returns the recorded `PackingMode`. No new table, no new column:
  the choice travels inside the existing durable envelope JSON.

### B. One owning workflow (no parallel service, no new endpoint)

`MarketplaceOperationalGroupWorkflow` was extended, not duplicated:

* `ResolveAsync` is the pure validation step: occurrence required, packing token validated, member set /
  quantities / explicit reuse ids / accepted method checked, and the COMPLETE envelope with its packing
  mode + fingerprint built — with no write of any kind.
* `RunReviewedPlanAsync` executes a pre-resolved plan. For `Separate` it creates/reuses exactly the reviewed
  member parcels, writes NO package and NO package member, records the separate completion and answers
  `{ packingMode:"separate", consolidatedPackageId:null, packageNumber:null, shipmentIds, members }`.
  For `Common` it keeps the existing exact package membership and package reconciliation unchanged.
* `TryReconcileRecordedAsync` is the new public exact-intent seam. It answers an ALREADY recorded occurrence
  from its own durable links BEFORE any fresh readiness projection (partial progress may already have
  consumed the remaining quantity that projection relies on), while re-checking the CURRENT actor, the
  checkout and the exact group + envelope fingerprint every time. A retained browser value is never
  authorization. An absent occurrence returns null so the caller must pass the ordinary projection gate:
  there is no broad bypass.
* Replay identity: same occurrence + same complete envelope returns the exact recorded result (including a
  completed separate shape with no package); same occurrence + changed mode / quantity / reuse / source
  membership is `order.operational_group.workflow_intent_conflict` with zero added rows; an unknown packing
  token is the typed mode refusal with zero rows.

### C. Public command + projection (Order Application)

`AdminOrderOperationsOrchestrator` routes `prepare_operational_group` through one narrow seam
`RunOperationalGroupAsync`: load the checkout (typed invalidity), keep the existing cancelled-order refusal,
ask the workflow to reconcile a recorded occurrence, and only otherwise fall through to the unchanged
`RunProjectedCoreAsync` gate. The projection's prepare action is renamed to the concise
«آماده‌سازی ارسال گروه» and carries `packingModes: ["common","separate"]` so the SERVER owns which physical
choices are offered; the UI never decides availability.

### D. One review surface (Frontend)

* `admin-operational-group-workflow-dialog.tsx` renders ONE explicit fieldset «بسته‌بندی فیزیکی این گروه»
  with two labelled 44px choices — «یک بستهٔ مشترک» and «مرسوله‌های جدا» with one concise sentence each — and
  renders exactly the choices the server projected. The safe default stays the existing common package;
  the submit label and the panel title follow the reviewed choice. Nothing is offered inline on the normal
  page (254 display-only rule kept).
* `operational-group-occurrence-store.ts` now carries `packingMode` in the reviewed envelope AND in the
  request fingerprint (so switching the choice is a genuinely different operation, blocked while an
  attempt is unresolved), normalizes legacy stored attempts to `common`, and `writeOperationalGroupAttempt`
  returns `true` only after a `sessionStorage.setItem` verified by read-back.
* `admin-order-operational-delivery-section.tsx` refuses a NEW send when the complete envelope is not
  reload-durable: ONE concise actionable inline message, ZERO fetch, the reviewed choice and open review
  kept for a corrected retry. A recorded attempt remains read-only (radios disabled, recorded choice shown)
  and only the recorded envelope may be reconciled.
* `admin-order-items-shipping-panel.tsx` maps the server-projected modes into the dialog, posts
  `operationalGroupPackingMode`, and uses a distinct success sentence per completion shape.
  `admin-order-operations.ts` carries the new request field and the projected `packingModes`.

## 3. Proof (ONE canonical database)

Relational, real `tooba_alpha` @ 127.0.0.1:5432 — `MarketplaceOperationalPackingChoice259Tests`
7/7 passed on the final code (fixtures are task-labelled committed-domain rows created through the owning
domain/application boundaries and read back through the real facts/history readers and the real admin
adapter; only the platform access reader is a controlled seam because grants live in Host AccessControl):

| Case | Result (recorded ids in `tb259-relational.txt`) |
| --- | --- |
| A common | one group, 2 constituent parcels, exactly 1 consolidated package (`01a1c5c2-e849-…`), replay returns the same ids |
| B separate | one group, 2 independent parcels, **0 package rows**, completed separate mode replays through the PUBLIC command even though fresh readiness is gone; an ABSENT occurrence still hits `order.operation.invalid` |
| C conflict | same occurrence + changed mode → `workflow_intent_conflict`; changed quantity → conflict; the public command answers the same conflict; unknown token → `workflow_packing_mode_invalid`; added rows = 0 (still 2 parcels) |
| C2 recovery | a fault injected exactly between member writes (both completion shapes) recovers the exact recorded ids on the same occurrence/key — nothing swept, nothing duplicated |
| C3 concurrency | two independent contexts on one occurrence allocate exactly ONE parcel set (2 parcels, 0 packages) |
| D authority | foreign group / sibling-group parcel reuse / Store-owned group / unauthorized actor refuse before writes (4 typed refusals) |

Frontend mounted proof on the REAL section + panel + dialog —
`app/admin/admin-order-operational-delivery.mount.test.ts`: **45/45** (41 retained cases + 5 TB-259:
choice reviewed and durable before POST, separate completion is authoritative completion, retained choice
read-only with a changed choice refused, non-durable storage refuses the NEW send with zero POST then sends
exactly once when storage is restored, and only server-projected choices are offered).

Retained regressions: Fulfillment `PostDispatchException` + `AutomaticTracking` **40/40**; Order public
258 command `AdminOrderPostDispatchReplayTests` **9/9**; Order architecture guard
`Application_ops_has_no_central_ExecuteAsync_dispatcher` **1/1** (the extracted plan runner is named
`RunReviewedPlanAsync`).

Whole Order module parity against the expected base (identical-failure claim, NOT a green claim):
base `Failed 96 / Passed 613 / Total 709`, current `Failed 96 / Passed 620 / Total 716`, only-in-current 0,
only-in-base 0 (`tb259-order-suite-parity.txt`). The 96 are the same environment-refusing disposable-PG and
discretionary-PG/coupon classes at base and after this task; nothing was re-baselined and no guard text was
edited.

Build: `npx tsc --noEmit` **0**; `npm run build` **success**, BUILD_ID `UZ3uUgVwd1cDuJ1wXVeJA`;
`dotnet publish ... -c Release -o artifacts/publish-primary` **success**.

## 4. Deploy / runtime

* Host: republished to `artifacts/publish-primary` and restarted (**PID 24396**), `/health` **200**,
  same SingleStore / `store-alpha` / `tooba_alpha` installation as before.
* Frontend: rebuilt and restarted (**PID 43948**), `/fa` **200**, current
  `/_next/static/UZ3uUgVwd1cDuJ1wXVeJA/_buildManifest.js` **200**, previous build
  `rEs1YLE7thpJEsRLwtJ3H` now **404**. The served chunk `9127-3e442ffa2d287eb7.js` (200) contains
  «مرسوله‌های جدا», `packingMode`, the `getItem(...)===` read-back and the «درخواست ارسال نشد» durability
  refusal.
* Existing read-only review route `/fa/admin/orders/01a1b92d-e97e-7000-999a-143350d0edaf` → **200**.
* Schema: **no migration added, none applied, nothing re-applied** — the choice lives in the existing
  occurrence envelope JSON; `ConsolidatedPackageId` was already nullable. No manual DDL, reset, reseed,
  backfill or business SQL. ONE database `tooba_alpha`; no disposable/parallel/Docker database, no 15433,
  no edition/connection/port change; DataProtection and CapturedPreview state untouched (local ON /
  checked-in OFF preserved); user order/cart/parcel/payment/service rows read-only; 255 parked; live Tipax
  deferred — no provider call, no fabricated waybill.

## 5. Honest limits

* The canonical review order `01a1b92d…` is Store-owned, so it correctly exposes NO cross-seller Marketplace
  operational group. Positive proof for both packing shapes comes from task-labelled committed-domain
  fixtures on the SAME database plus mounted real components — this does NOT prove a real Marketplace
  storefront checkout or a live provider.
* No DOM/screenshot pass was run this stage: the deliverables are behavioural (dialogs, durable envelopes,
  completion shapes), the mounted suite exercises the real section/panel/dialog, and the canonical order
  has no reachable group dialog to observe. RTL/44px/overflow were therefore not re-measured in a browser
  at 1440/1024/390.
* Separate mode creates at most ONE reviewed constituent parcel per selected member (the current workflow's
  bounded shape) — no arbitrary bin packing, dimension summation or multi-box optimizer was invented.
* Common mode reusing a previously separate Created parcel is validated by the existing package membership
  rules (ownership, compatibility, not-already-packaged); a parent PACKAGE still has no non-delivery/return
  workflow — the 258 parent-package refusal remains an honest, unchanged limitation.
* Concurrency is proven with two independent contexts plus the existing DB guards, not a stress/load test.
* Reusing/retaining a recorded occurrence is proven through the public command and the mounted client for
  the shapes above; no live multi-operator browser session was exercised.
* Financial seller orders, accepted fees/method/date, committed history, 257 packed−shipped−open, 258
  source-safe resend capacity, inventory reservation and customer-delivery conservation are untouched by
  the packing choice; no fee is recalculated or re-quoted.

## 6. Handoff

Worker PASS inside the issued scope only. `STOP_FOR_ARCHITECT`.
`automaticNextImplementationTask: NONE`.
Issue 5 (due workqueue / shared operational order view, task 260) remains **NOT ISSUED**.
