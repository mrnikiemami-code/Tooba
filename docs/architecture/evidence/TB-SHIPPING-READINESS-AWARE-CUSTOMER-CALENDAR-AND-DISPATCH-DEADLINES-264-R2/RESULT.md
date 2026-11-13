# TB-264-R2 — worker result (delivery closure)

Status **PASS within the issued delivery-closure scope**. Decision
**DELIVERED_CHANGED_FRONTEND_WITH_REAL_AUTHORING_SAVE_RELOAD_PUBLISH_AND_ONE_POSITIVE_STOREFRONT_SELECTION_TO_ACCEPTED_POLICY3_TO_QUEUE_CHAIN**.

NOT Architect ACCEPT, NOT user VISUAL/FUNCTIONAL ACCEPT, NOT "shipping complete", NOT a paid checkout, NOT
Returns/financial integration. Base `c9d87308b8c5e3ec7347a984a42ce5b0a44c40fa`; one worker, one task, no agent
delegation, no historical agent resumed. `265` remains **NOT ISSUED**.

This round was delivery closure only: no scheduler rewrite, no new algorithm, no new return flow. The R1
source corrections (explicit zeros / window-constrained backward plan / explicit processing week) are retained
untouched; the R1 focused suites were not re-run as a green loop.

## 1. What the review said was missing, and what was actually done

| Review gap | This round |
|---|---|
| Changed FE never built/served; runtime still reported `Hou23ETYm3l_DFIPdinpW` | FE rebuilt **once** from the changed sources and the canonical Next process on 3000 was restarted onto it. Served `BUILD_ID = ILzh0yMqPr3RDrjGp3wYE`; `/fa` 200; the served chunk carrying «زمان آماده‌سازی» and the processing-mode controls was fetched from the **running** server (not from disk). |
| No real form Save → reload → publish proof | On the REAL authoring page in a browser: edit mode → handoff minutes `75 → 80`, Thursday warehouse processing close `18:00 → 17:30` → real «ذخیره تغییرات» click → intercepted `PUT …/editor-document` **200** → page reload → the operator sees `80` and «۰۹:۰۰ تا ۱۷:۳۰» again → real «انتشار» → confirmation dialog («بازبینی 8 …») → accept → publication now **selection 5** on revision 8. |
| Only a negative storefront case (`availableDates: []`) | A genuinely separate task-labelled method/capture/offer/cart fixture produced a **positive** calendar (7 dates, hourly 09:00–18:00 slots), the **public** selection writer saved it, and the public commit boundary produced a real Order checkout whose durable row carries policy 3. |
| User review draft was written by R1 | `01a17d0d-…` was **not written at all** this round. All new writes are on `TB264R2` fixtures. The exact R1 mutations are disclosed in the audit note appended to the R1 result/receipts (history kept, nothing rolled back). |

## 2. Delivered frontend (the actual changed build)

* Build: `npm run build` (production) → `BUILD_ID = ILzh0yMqPr3RDrjGp3wYE`; `npx tsc --noEmit` exit 0.
* The previously running Next process (PID 17440, old build `R9whZM-4FOR7dVyjVOgUr`) was stopped and a new one
  started on the new output → **PID 48780**, `/fa` **200**.
* Served-chunk check against `http://127.0.0.1:3000` (not the build folder):
  `/_next/static/chunks/6899-61d97f4a6a9808f2.js` → 200 and contains «زمان آماده‌سازی» plus the
  `WorkingWeek`/`Continuous` processing-mode control; `/_next/static/chunks/6138-ded12a4885f63d10.js` → 200
  carrying `processingSchedule`.
* Source change in this round is one storefront copy addition only (two new blocker codes are now named in
  Persian instead of falling through to a generic message):
  `storefront.captured_group.processing_schedule_missing` and `…processing_schedule_invalid`.
  The processing-week editor, timing pair and mode radios shipped in `424ea723` were already in the sources;
  what was missing was the built/served artifact, which now exists.

## 3. The one positive chain, layer by layer

All writes are on the ONE canonical database `tooba_alpha` @ 127.0.0.1:5432 (SingleStore, tenant
`store-alpha`), Host 5088, Next 3000. Fixtures are task-labelled `TB264R2`; no user order/cart/parcel/payment
row was written.

**A — authoring (real operator form, revision 8 → selection 5).**
Draft `01a1cd43-b98d-7000-b2a9-a35ab39d8058` (`tipax/express`, «TB264R2 تیپاکس اکسپرس آزمایشی», Store-owned),
settings `preparationMinutes 300 · handoffMinutes 80 · preparationMode WorkingWeek`, processing week
`Asia/Tehran` 6 working days of 7, Thursday `09:00–17:30`.
Publication: `state Selected`, `selectionVersion 5`, `selectedSnapshotId 01a1d50f-5275-7000-b050-dda3aedd1838`,
`selectedSourceRevision 8`, `selectedFormatVersion 4`, `selectedMatchesCurrentDraftRevision true`.
The earlier `PUT` on the same draft (revision 7 / selection 4, snapshot `01a1cdb9-…`) is this round's own
pre-reload step, kept as history.

**B — storefront projection (positive, from the same capture).**
Cart `01a1d511-4c3f-7000-8d46-78a053510edb` (registered task customer `09992642003`,
user `01a1d511-23a3-7000-8238-a5ecff4171c7`), one line of task offer
`0442a1fa-1d95-4b24-b9d9-129fc19942d2`:
`POST /v1/storefront/shipping/projection` **200** → confirmation snapshot `01a1d50f-…` (exactly the published
one), source revision 8, selection version 5, **7 available dates 2026-11-16 … 2026-11-22** with real hourly
windows. Sibling methods of other sellers keep their own truthful state (e.g. `post` on another capture still
reports `storefront.captured_group.carrier_schedule_missing`) — no date is invented for anyone.

**C — public selection writer (durable policy 3).**
`PUT /v1/storefront/shipping/selection` **200** with the server-returned snapshot/revision/version/area/rate
identity and `selectedDeliveryDate 2026-11-16`, `selectedDeliveryTimeWindow 09:00-10:00`; response
`selectionRevision 1`, shipping group `01a1d511-7412-7000-a5a7-2d42aef7c393`, charge 158,000 IRR.

**D — acceptance boundary + existing queue (same source plan).**
`POST /v1/storefront/shipping/commit` **200** → checkout `01a1d511-adbb-7000-8575-d3b957453d76`
(seller order `01a1d511-adce-7000-8819-0185f673c4bf`, number `TB-20261113132108-01-320e47`,
`paymentState PendingPayment`). The `"order".order_shipping_groups` row stores
`accepted_dispatch_policy_version 3`,
`preparation_start_utc 2026-11-14T09:30Z`, `transfer_departure_utc 2026-11-14T15:10Z` (= handoff deadline − 80 min),
`handoff_deadline_utc 2026-11-14T16:30Z`, `accepted_dispatch_preparation_minutes 300`,
`accepted_dispatch_handoff_minutes 80`, `accepted_receiving_date 2026-11-16`, zone `Asia/Tehran`,
snapshot `01a1d50f-5275-7000-b050-dda3aedd1838`.
The existing admin queue answers the same numbers unchanged —
`POST /v1/admin/fulfillments/work-queue/query` (`search=TB264R2C2`): key
`group:01a1d511-7412-7000-a5a7-2d42aef7c393`, `dispatchBucket Upcoming`, `policyV 3`,
`prep 2026-11-14T09:30Z`, `transfer 2026-11-14T15:10Z`, `handoff 2026-11-14T16:30Z`,
`plannedHandoffDate 2026-11-14 08:00–20:00 Asia/Tehran`, `customerReceivingDate 2026-11-16 09:00-10:00`,
`preparationState Upcoming`, `nextStepCode mark_processing`, and the operations locator
`/fa/admin/orders/01a1d511-adbb-7000-8575-d3b957453d76/operations?fulfillmentId=9fe40e71-23cb-4014-8a35-e4c3231c9dab&shippingGroupId=01a1d511-7412-7000-a5a7-2d42aef7c393&from=queue`.

**Fixtures used (explicit).** Two task-owned drafts (`01a17d0d` was **not** in them): `01a1cd43-…` (method,
Store-owned), two task customers (`09992642003` registered this round, `09992642001` from the earlier step),
carts `01a1d511-4c3f-…` / `01a1d510-0738-…`, checkouts `01a1d511-adbb-…` and `01a1cdba-7d2d-…`, plus the
`TB264R2C-…` fulfillment unit created through the production `FulfillmentUnit.CreateFromPaidOrder` factory.

**Disclosed boundary.** No live paid checkout was performed: a paid order needs a real payment provider, and
this task forbids one. The **plan** under test is authoritative Order data produced by the public path; the
only fixture layer is the Fulfillment unit for that same seller order/group, built through the owning
production factory (the same path the paid integration uses). Nothing in Order was inserted, updated or
re-derived by a test.

## 4. Focused checks (each once, no broad/green loop)

| Check | Result |
|---|---|
| `AdminPreparationConnected264R2Tests` on real `tooba_alpha` (`TB264R2_CONNECTION`) | **2/2 passed** — the earlier chain (policy 3, 300/75, prep 11-12T09:30Z, transfer 15:15Z, handoff 16:30Z) and the final published chain (300/80, prep 11-14T09:30Z, transfer 15:10Z, handoff 16:30Z, customer 11-16 09:00-10:00) both project the same instants into the existing queue. |
| FE focused tests for the touched authoring/timing paths (`authoring-timing-264`, `authoring-general-ui`, `storefront-shipping-unavailable-methods`) | **22/22 passed** |
| `npx tsc --noEmit` | **0** |
| `npm run build` | **0** (BUILD_ID `ILzh0yMqPr3RDrjGp3wYE`) |
| Host `/health`, `/health/ready` | **200** (`SingleStore`, messaging healthy) |

One pre-existing failure was **not** caused by this round and is not claimed as fixed:
`storefront-shipping-group-schedule.test.ts` asserts a stale JSX regex against
`storefront-shipping-group-cards.tsx`; the source change in this round only adds two message strings, so the
assertion is stale relative to the file it inspects. It is reported, not touched.

## 5. The bucket assertion had to become time-relative (honest correction)

The earlier connected case froze `dispatchBucket = Today`, which was true on 2026-11-12 and false on 2026-11-13.
The **production** behaviour was right (the row truthfully became `Overdue` / `HandoffMissed`, and the new row
is `Upcoming`); only the test expectation was stale. It now derives the expected bucket from the plan's own
handoff date in the carrier zone, so the assertion proves "the queue classifies this exact durable plan
truthfully today" instead of "today is the day the fixture was made". No production rule changed.

## 6. User-service audit (R1 correction)

R1 wrote the user's review draft `01a17d0d-e033-7000-af43-49a1daa28822` and reported it as a task fixture, and
it also claimed the frontend was untouched. Both statements were wrong. The exact mutations are disclosed in the
note appended to `264/R1-RESULT.md` and `264/R1-RECEIPTS.md`; history is preserved, nothing was rolled back or
republished, and this round did not write that draft (verified read-only: revision 6, settings 240/60
`WorkingWeek`, newest snapshot `01a1cd2d-dc41-…` from R1).

## 7. Constraints respected

ONE database `tooba_alpha` (5432, SingleStore, `store-alpha`); Next 3000 / Host 5088 only. No disposable/test/
parallel/Docker database, no 15433, no edition/connection/port change. No migration added or applied this round
(heads unchanged: Shipping `20261112120000_AddProcessingHoursAndTimingPolicy`,
Order `20261112130000_RelaxPreparationDispatchZeroAllowances`). No manual DDL / business SQL / reset / reseed /
backfill / global reclaimer. User orders/carts/payments/parcels read-only. DataProtection and CapturedPreview
unchanged (local ON with 7 dates / checked-in OFF). `255` parked, live Tipax deferred, `265` not implemented.

## 8. Known limits

* No live paid checkout and therefore no live `ReadyToFulfill` history for the newest checkout: the Fulfillment
  unit is a disclosed production-factory fixture, and the queue row it produces is the real queue engine reading
  the real accepted plan.
* The authoring Save proof is a browser DOM/network proof (headless CDP), not a screenshot deliverable; the
  toast itself is not captured as an image, only its code path (existing `react-toastify` `toast.success`) is
  unchanged and covered by the retained `tb261-notification` mount test from the previous stage.
* The storefront copy test suite has the one pre-existing stale-regex failure described above.
* Preparation time is counted inside the stored warehouse week — that is a processing window, not a carrier
  booking; no slot is reserved anywhere.
* `storefront-shipping-group-schedule.test.ts` and the legacy `SalesChannel='Web'` admin detail 500 remain
  pre-existing and untouched.
