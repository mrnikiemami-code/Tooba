# TB-263 — worker result: repair the stuck damaged replacement, and never call a known server answer "unknown"

Status **PASS** within the issued scope, decision
**SERVER_KNOWN_PENDING_AND_DETERMINISTIC_REFUSAL_DISTINGUISHED_FROM_UNKNOWN_WITH_OWNING_OCCURRENCE_PROJECTION_AND_EXISTING_TOAST_MESSAGEBOX**,
NOT Architect ACCEPT, NOT user VISUAL/FUNCTIONAL ACCEPT, NOT "shipping is complete", NOT Returns/financial integration.
Base (expected/verified ancestor) `7117d6d4977b14f0a37c4eda1c4950967bb29a44`; work base `56e7e9c56`; code/evidence/merge
SHA in `result-contract.txt`. One worker, one task, no agent delegation, no historical agent resumed.

## 1. Reproduction first — the operator's own returned parcel, READ-ONLY (zero writes)

The screenshot record is treated strictly read-only: checkout `13feece0-4890-4e45-b92f-4d4b1a5e0c97`, fulfillment
`3ce14fb8-6453-44c0-b364-0f919c8bf2e0`, returned source parcel `a44aa20e-ecf3-4964-b828-bf1fc4a72eac`, sibling
dispatched parcel `01a1ca38-80a6-7000-9d20-36644851fae7`. Reading `GET /v1/admin/orders/{id}/operations` and
`GET /v1/admin/orders/{id}` on the running Host (the pre-repair build) produced, before any change was deployed:

```
OPS 200  action resolve_damaged_goods labelFa=«ادامهٔ تعیین تکلیف کالای آسیب‌دیده» shipment=a44aa20e-…
SHIP status=Returned receipt=[{line 8e0a897b-…, quantity 1, condition Damaged}]
     damaged=[{ id 01a1cbc2-b96d-7000-8c78-d12e172ff327, line 8e0a897b-…, quantity 1,
                outcome Replacement, status Recorded, occurrenceKey "" }]
     lines=[{ 8e0a897b-…, qty 1, outstanding 0 }]
LINE 8e0a897b-… packed 1 allocated 1 shipped 1 usable 0 resendable 0 operationalStatus Returned
```

So the durable decision **existed** (`Recorded`, `Replacement`, quantity 1, its damaged unit fully committed) and the
server still offered «ادامهٔ تعیین تکلیف». The only missing fact was the decision's **own occurrence key**: the
admin projection returned `occurrenceKey: ""`.

Consequence, traced through the owning seams instead of guessed:

1. `admin-order-operational-delivery-section.tsx` built `damagedResume` from the pending decision and passed the
   EMPTY `occurrenceKey` through to the shared review dialog.
2. `post-dispatch-exception-dialog.tsx` therefore sent `resolve_damaged_goods` with a blank `idempotencyKey`.
3. The owning aggregate refuses that deterministically: `FulfillmentUnit` line 875 throws
   `fulfillment.damage.resolution_occurrence_required` (HTTP 409, typed `errorCode`), and a fresh-looking decision
   composed against an already-committed damaged unit would be `fulfillment.damage.exceeds_outstanding`
   (`FulfillmentUnit` line 911).
4. The review treated **every** non-`ok` answer as an unknown response: it stamped the retained envelope as an
   uncertain attempt and rendered the amber "نتیجهٔ قطعی…مشخص نشد" block plus the generic red
   «خطای سرور را بررسی کنید … این پنجره را نبندید» copy, with only «ارسال دوبارهٔ همان درخواست» as an exit.

The same misclassification also hid the genuine known-pending business state
(`fulfillment.damage.replacement_unavailable`, 409 when no healthy stock exists): the durable decision is recorded and
continues later, yet the operator was told the response was lost. Both are semantic defects in one surface, not a
backend lifecycle failure.

## 2. Repair (each at its owning seam, no lifecycle rewrite, no auth weakening, no fake success)

- **A — owning projection carries the reviewed identity.** `FulfillmentAdminOperationsAdapter` now forwards
  `OccurrenceKey` into `DamagedResolutionSnapshot`. The operator surface receives server-owned evidence, so a
  reload/another operator continues the SAME occurrence verbatim; nothing is minted client-side.
- **B — ONE semantic classification of a submitted review.**
  `features/order-operational-groups/post-dispatch-outcome.ts` defines `completed | pending | refused | unknown`
  and classifies the transport facts: `ok` → completed; denied → refused (blocking); no status / 5xx → unknown;
  `fulfillment.damage.replacement_unavailable` → **pending**; every other deterministic 4xx → refused with the exact
  localized reason. Only `completed` clears the retained attempt; `pending` and `refused` keep the exact reviewed
  envelope (source/quantity/outcome/occurrence), so a retry cannot mint a second decision or hold.
- **C — honest feedback surfaces, reusing the app's existing primitives.** Transient success = the existing
  `react-toastify` `toast.success`, once per confirmed action (never on GET/reload/re-render). Blocking refusal =
  the existing design-system `MessageBox` (newly exported, returns `null` while closed so it is never a second empty
  modal in the DOM); `AuthoringMessageBox` now delegates to it — one component, no parallel dialog framework.
  Pending is a compact sky-blue durable-status block inside the same review (not a toast, not an error); unknown keeps
  the amber "same request only" block; field validation stays adjacent to its field. The verbose
  "inspect the server / do not close this window" copy is gone.
- **D — localized operator vocabulary.** `admin-error-map.ts` gained Persian/English copy for every
  `fulfillment.damage.*` code (26), so no raw enum or machine code can reach the operator.
- **E — safe recovery.** Closing is safe: the retained envelope is restored read-only on reopen and can only be sent
  verbatim; the panel refuses a NEW send while the reviewed attempt is unresolved and re-reads the recorded attempt
  when the fresh projection stops offering the action. No auto-POST loop.

## 3. Independent execution proof (real Host over the one canonical runtime, no browser, no user mutation)

- **Projection, before → after** on the same read-only record (`node scripts/tb263-probe.mjs`, GET only):
  `occurrenceKey: ""` → `occurrenceKey: "a3618153-0e89-4205-bb30-3dcb90c9688f"`, decision still
  `status Recorded`, quantity 1, receipt and every user row unchanged, zero writes.
- **Public-command path** `AdminOrderPostDispatchReplay` **11/11** on real `tooba_alpha` storage: no stock →
  `fulfillment.damage.replacement_unavailable` with zero parcel/zero hold; the same key retried stays pending; healthy
  stock supplied through the owning path completes the SAME occurrence with one linked parcel and one non-expiring
  hold; cancel releases only the parcel allocation (`ReleaseCalls=0`); retry rebinds the retained hold
  (`secondReserve=0`); stale old-parcel cancel detaches nothing; exact public replay answers the live parcel.
  This run appended its own receipt line to the retained 262-R1 evidence file (fulfillment
  `ac758945-093f-4285-9e70-00e1cad0c700`, checkout `f8dfba61-831e-4cd8-bf83-ea99fc644e7f`, source
  `42d711a7-42c1-40b5-858c-9e159cb327a8`, rows `shipments2/receipts1`).
- **Owner aggregate** `DamagedResolution262Tests` **8/8** (source-scoped decision, quantity conservation, one unit one
  decision, cancellation/retention semantics unchanged by this repair).
- **Host relational** `DamagedGoodsResolution262Relational` **8/8** on real `tooba_alpha` (independent contexts,
  non-expiring hold, reclaimer predicate matches 0 rows, concurrency one-winner).

## 4. Mounted proof (REAL section + panel + dialog, `tsx --test`, jsdom)

`app/admin/admin-order-operational-delivery.mount.test.ts` **58/58**, including four NEW TB-263 cases:

- authoritative completion → review closes, exactly ONE `toast.success`, retained envelope cleared, owning facts
  refreshed once, no amber/red/blocking residue;
- `replacement_unavailable` → the sky-blue pending statement, no toast, no "lost response" wording, and the second
  submit replays the SAME `idempotencyKey` (`occ-tb263-recorded`) with the same quantity/outcome — no second decision,
  no second hold;
- genuine transport loss → amber retained envelope, no "inspect server / do not close this window", close and reopen
  restores it read-only and the retry replays the SAME key;
- deterministic denial → review closes into exactly ONE blocking `MessageBox` (one close action, no duplicate inline
  error, no toast) while the exact envelope stays retained.

Plus the retained TB-258/259/261/262/R1 cases in the same file, which prove the surfaces this change touched did not
regress (single-flight, durable-before-send, mixed receipt quantities, packing choice, damaged decision replay).
`admin-error-map.test.ts` **6/6**; frontend typecheck **0**; production build **0**.

## 5. Deploy and runtime (same canonical instance, no schema change)

No migration added or applied (no schema change, no `Migrations` file in the change set). Host republished to
`artifacts/publish-primary` and restarted (previous PID **7056** stopped; new PID **6216**; `/health` 200,
`/health/ready` 200 → `SingleStore`, postgresql configured). FE rebuilt and restarted (previous PID **45964** stopped;
new PID **43700**; `BUILD_ID` `iyES24s7U0oBnc8uyG4_S`; `/fa` 200; the operations route for the read-only record 200;
the served chunks `1323-b2ed9262639c7262.js` and `7825-0780d97d33f9949c.js` carry the pending/durable copy).

ONE database `tooba_alpha` @ 127.0.0.1:5432, Edition `SingleStore`, tenant `store-alpha`; no disposable/test/parallel/
Docker DB, no 15433, no edition/connection/port/appsettings change; DataProtection and CapturedPreview unchanged
(local ON / checked-in OFF); no manual DDL/business SQL/reset/reseed/backfill; the user/screenshot rows were read-only
and not mutated; task-labelled production-path fixtures only (`TB262-*`, `TB258-*` receipts already recorded); no
provider call, no fake waybill; 255 parked; live Tipax deferred.

## 6. User trial (hand-off, not performed by the worker)

`http://127.0.0.1:3000/fa/admin/orders/13feece0-4890-4e45-b92f-4d4b1a5e0c97/operations?fulfillmentId=3ce14fb8-6453-44c0-b364-0f919c8bf2e0`
→ on the returned parcel `a44aa20e-…` press «ادامهٔ تعیین تکلیف کالای آسیب‌دیده». With no healthy stock the same decision
is stated as a durable pending fact (no red banner, no "unknown" wording) and can be closed safely; once healthy stock
exists the same decision completes into one linked replacement parcel. Nothing on this page asks the operator to
inspect a server error or keep a window open.

## 7. Known limits (honest)

- No live click/mutation on the user's screenshot record: the before/after proof is GET-only, and completion with
  healthy stock is proven by the owning public command against task-labelled fixtures on the same database.
- The worker did not perform the user trial; no screenshot matrix was produced (not requested).
- A `referral` remains an honest pending customer-service item; Returns/settlement linkage is task 265 and was NOT
  implemented. Calendar readiness is task 264 and was NOT implemented.
- Concurrency/recovery are proven with independent real contexts and DB guards, not a stress test.
