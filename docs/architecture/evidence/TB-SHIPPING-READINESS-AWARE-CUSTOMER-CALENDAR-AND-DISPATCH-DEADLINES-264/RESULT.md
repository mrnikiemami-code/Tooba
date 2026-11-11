# TB-264 — worker result (PASS within the issued scope)

Task: `TB-SHIPPING-READINESS-AWARE-CUSTOMER-CALENDAR-AND-DISPATCH-DEADLINES-264`
Mode: `PREPARATION_AND_CARRIER_TRANSFER_READINESS_TO_FEASIBLE_CUSTOMER_DATES_AND_DURABLE_OPERATIONAL_DEADLINES`
Status: **PASS within the issued scope** — decision
`EXPLICIT_OWNED_PREPARATION_AND_TRANSFER_POLICY_WITH_ONE_FORWARD_BACKWARD_AUTHORITY_DURABLE_POLICY_V2_DEADLINES_AND_PREPARATION_AWARE_EXISTING_QUEUE`.
NOT Architect ACCEPT, NOT user VISUAL/FUNCTIONAL ACCEPT, NOT "shipping complete", NOT Returns/financial
integration. `265` (Returns linkage) remains **NOT ISSUED**.

## 1. The defect this closes

Scheduling treated the order/eligibility instant as "goods ready": a near-closing order was offered a
carrier handoff and a customer date that the physical preparation could never reach, and the dispatcher had
no editable, durable statement of how long preparation/transfer actually take. The queue and the order view
therefore could not say when preparation had to start or when the parcel had to leave.

## 2. What landed

1. **Editable owned policy (Shipping).** `ShippingAuthoringMethodSettings` / `ShippingAuthoringDraft` /
   `ShippingAuthoringEditorDocument` / revision snapshot / captured quote `Settings` carry
   `PreparationMinutes` (0…10080) and `HandoffMinutes` (0…1440) as an explicit all-or-nothing pair.
   `PreflightMethodSettings` refuses a one-sided pair, out-of-range values and a negative value with the
   existing typed error; explicit `0/0` is accepted. The pair is part of the editor payload, decode, dirty
   predicate and conflict retention, so a timing-only save writes the complete pair (proven in the mounted
   authoring model test, incl. "clearing both removes the policy explicitly"). No new setting for the
   already-existing `PreparationDays`.
2. **ONE forward/backward authority (Shipping Contracts).**
   `ShippingDispatchSchedulingAuthority.BuildReceivingCalendar(..., preparationMinutes, handoffMinutes)`
   forward-schedules `nowUtc + preparation + transfer` into the first possible carrier window, then MAX
   transport working days, then customer working dates/slots; a half-configured pair is a typed refusal.
   `ResolvePreparationDeadlines(receivingDate, carrierWeek, transportWorkingDays, preparationMinutes,
   handoffMinutes)` backward-derives `HandoffDeadlineUtc` (exclusive close), `TransferDepartureUtc` and
   `PreparationStartUtc` with `preparation < transfer <= close`, rejecting unprovable DST instants.
   `PreparationAwarePolicyVersion = 2`; policy v1 stays readable and is never retro-labelled.
3. **Durable accepted evidence (Order).** `StorefrontPlannedDispatchResolver` derives and persists the triple
   (policy v2) from the captured quote + chosen receiving date; `ShippingGroupPlannedDispatch` accepts only
   the complete triple with both minute inputs, and checkout revalidation compares policy version + minutes +
   triples. New additive migration `20261111233000_AddPreparationAwareDispatchDeadlines` adds 5 nullable
   columns to `order.order_shipping_groups` and `order.cart_shipping_groups` and re-creates
   `ck_…_accepted_dispatch_plan` as `num_nonnulls IN (0, 8, 13)` plus strict ordering/bounds.
4. **Storefront.** The captured-group composition builds the customer calendar with the method's real
   allowances, so only physically feasible receiving dates/slots are offered; nothing admin-only leaks to the
   buyer and no money/fee changes.
5. **Existing queue (Fulfillment) and order view (Order).** `OrderDispatchPlanFactsBridge` exposes the three
   instants + policy version through Contracts only (no cross-module persistence). The work queue derives
   `AdminFulfillmentPreparationState` (`None/NotApplicable/Upcoming/Today/Overdue/HandoffMissed`) server-side,
   adds its OWN `dispatch_preparation_due` filter + `PreparationDue` count (dispatch buckets are not
   redefined; «سررسیدشده» additionally treats a due preparation as due now), and shows
   «آماده‌سازی تا … · خروج تا … · تحویل به حامل تا …». The shared operations/order timeline renders
   «آماده‌سازی از …» and «آخرین مهلت تحویل به حامل …» in the carrier zone beside — not replacing — the
   customer promise. One action owner, one locator, no bulk dispatch, no auto-mutation.

## 3. Proof (each check run once; no parity/green loop, no screenshots, injected clocks only)

| Case | Evidence |
| --- | --- |
| A + B + zero transit + cross-zone + fail-closed (forward/backward, close-exclusive, MAX transport) | `ShippingDispatchSchedulingAuthority264Tests` **7/7**; captured settings/capture path `ShippingCapturedRevisionQuoteTests` **13/13** (11 retained + 2 TB-264) |
| Prefix/`PreparationDue`/state derivation | `AdminFulfillmentPreparationTiming264Tests` **6/6** |
| Real storage: v2 triple + policy version roundtrip, bridge equality, v1 `null`, **partial triple refused (23514)** | `AdminDispatchPreparation264RelationalTests` **1/1** on `tooba_alpha` — `TB264-212116 … prep=11:15Z transfer=12:45Z deadline=13:30Z policyV2=2 … columns=13/8 partialTriple=23514-refused bridge=exact` (`tb264-relational.txt`) |
| Real storage: accepted v2 plan → **actual queue query** → preparation state + own deadlines + filters + counts, legacy sibling untouched | `AdminFulfillmentWorkUnitProjection260R1RelationalTests` **6/6** (5 retained 260-R1 + new E) — `TB264-E-213454 … rows=2 bucket=Upcoming prep=Overdue … legacyPrep=None preparationDue=1 filterPreparationDue=1 filterUpcoming=2 locator=group:5039c4ab…` (`tb264-queue.txt`) |
| Mounted UI (deadline row for v2, none invented for v1) + retained physical-work regressions | `admin-order-operational-delivery.mount.test.ts` **60/60** (2 TB-264 + 4 retained TB-263 and all earlier cases) |
| Authoring timing patch semantics | `authoring-timing-264.test.ts` **4/4** |
| Types / production build | `npx tsc --noEmit` 0 errors; `next build` 0 errors (BUILD_ID `Hou23ETYm3l_DFIPdinpW`) |
| Schema | `Tooba.MigrationRunner plan --tenant store-alpha` → Order head `20261111233000_AddPreparationAwareDispatchDeadlines`, every module `pendingMigrations: []` |

Migration was applied through the repository runner after a private backup
`D:\temp\tb264-backup\tooba_alpha-pre-tb264.dump` (3,267,430 bytes, 2026-11-12 00:24). No manual DDL, no
business SQL, no reset/reseed/backfill.

## 4. Runtime

- Host republished to `artifacts/publish-primary` (12:21) and restarted — **PID 6276**, `/health` 200,
  `/health/ready` 200 (`ready`).
- Frontend production build `Hou23ETYm3l_DFIPdinpW`, served by a restarted server — **PID 41488**, `/fa` 200,
  authoring route 200, `_next/static/Hou23ETYm3l_DFIPdinpW/_buildManifest.js` 200, and the new chunk
  `chunks/6899-a224cf719f19cb19.js` (carrying «زمان آماده‌سازی») is served **200**.
- ONE database: `tooba_alpha` @ `127.0.0.1:5432`, SingleStore, tenant `store-alpha`. No disposable/test/
  parallel/Docker DB, no 15433, no edition/connection/port switch. DataProtection and CapturedPreview
  unchanged (local ON / checked-in OFF). User and screenshot rows read-only, task-labelled `TB264-*` fixtures
  only, no provider call, no fake waybill, `255` parked, live Tipax deferred, active-order replacement
  reservations untouched (parcel cancel/return/retry still releases nothing).

## 5. Boundaries and honest limits

- **No live paid checkout was executed.** The acceptance path is proven through the production resolver +
  domain factory + real DB materializer/bridge and the queue query, not through a real customer payment. The
  user trial below therefore exercises a method edit + publish/readback + whatever real order offers a
  feasible date; it does not claim a paid checkout by the worker.
- The customer receiving calendar is server-derived; the browser never computes a date. The storefront's
  offered dates for a method whose pair is unset stay exactly the historical ones (no silent upgrade).
- The two allowances are **elapsed** time; there is no warehouse processing calendar in the product, so no
  working-hours claim is made and none was invented.
- Preparation/transfer are counted once; transport days keep TB-256 semantics; no holiday calendar exists.
- Queue priority/counts keep their TB-260/260-R1 identities; `dispatch_preparation_due` is additive.
- Multi-item common groups take the max readiness of their members only through their own plan; independent
  work units never borrow a sibling's plan (retained 260-R1 cases A–D).
- `Referral`/Returns/money remain untouched; `265` is NOT ISSUED.

## 6. User trial (canonical, after deployment)

1. `http://127.0.0.1:3000/fa/admin/shipping-services` → the task's method → general tab: set
   «زمان آماده‌سازی» and «زمان انتقال به حامل» → save (one toast), reload, confirm both values and no
   half-written policy; publish once and confirm the storefront dates move later by the allowances.
2. `http://127.0.0.1:3000/fa/admin/fulfillments` → filter «شروع آماده‌سازی رسیده» → open a due row's
   «عملیات ارسال» → the timeline shows «آماده‌سازی از …» / «آخرین مهلت تحویل به حامل …» in the carrier zone
   beside the customer promise (a legacy row correctly shows «بدون موعد آماده‌سازی»).
