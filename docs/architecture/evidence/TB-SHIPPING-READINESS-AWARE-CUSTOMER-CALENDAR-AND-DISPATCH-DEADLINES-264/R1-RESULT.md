# TB-264-R1 — worker result (bounded correction of the four Architect gaps)

Status **PASS within the issued R1 correction scope**. Decision
**NON_DECREASING_EXPLICIT_ZERO_ALLOWANCES_WINDOW_CONSTRAINED_BACKWARD_PLAN_EXPLICIT_PROCESSING_WEEK_AND_CONNECTED_CAPTURE_PROOF**.
NOT Architect ACCEPT, NOT user VISUAL/FUNCTIONAL ACCEPT, NOT "shipping complete", NOT Returns/financial
integration. Base `740d58d3a`; code/evidence SHA in `result-contract-r1.txt`. One worker, one task, no agent
delegation, no historical agent resumed.

## What was wrong (issued gap → root cause found by reading the code/the live rows)

| Gap | Cause |
|---|---|
| **A** explicit `0` allowances were refused | `ResolvePreparationDeadlines` required strictly increasing instants, so `0/0`, `0/30` and `30/0` were unreachable even though authoring accepted `0`; the Order check constraint also demanded strict `<`. |
| **B** a late carrier close could still be offered as a customer promise | the backward authority knew nothing about the chosen customer date/zone, so a same-day `0`-day transport plan could produce a handoff deadline **after** the customer window opened. |
| **C** "preparation minutes" were elapsed 24/7 and a method without a processing week silently borrowed the "ready now" instant | there was no stored warehouse processing week and no explicit continuous mode; a new commitment on a method with a timing pair but no processing week fell back to v1 semantics. |
| **D** no connected proof on the canonical install | the publication capture graph (`CaptureGraphQuery`) never loaded `ProcessingSchedule`, so an **existing, persisted** processing week was invisible to capture: the published body was written as `FormatVersion 3` with `"ProcessingHours": null` even though the draft row and its seven days existed. This was found on the live install, not assumed. |

## Landed

**A — non-decreasing ordering.** `ShippingDispatchSchedulingAuthority.ResolvePreparationDeadlines` accepts
`preparationStart <= transferDeparture <= handoffDeadlineUtc`, with exact equality only where the matching
allowance is `0`; the carrier close instant itself stays exclusive. `ShippingGroupPlannedDispatch` (Order
domain) and the two accepted-plan check constraints (`order.order_shipping_groups`,
`order.cart_shipping_groups`) were relaxed to the same `<=` rule, and a partial/decreasing triple is still
refused by the database (`23514`).

**B — the customer commitment constrains the backward plan.**
`ShippingReceivingCommitment(Date, OpensAt, ClosesAt, TimeZoneId)` is now an input of the same authority:
for a same-day transport the handoff deadline is `min(carrierCloseUtc, windowOpensUtc)`, and a commitment
that opens before the carrier does on that date moves the candidate date to an earlier carrier date instead
of producing an unreachable deadline. `WindowAwarePolicyVersion = 3` records that the plan was derived from
a real receiving window; an unprovable (DST) instant is a typed refusal, never a guess.

**C — the processing week is an explicit stored policy.** New owned table pair
`shipping.authoring_processing_schedules` / `authoring_processing_schedule_days` (additive migration
`20261112120000_AddProcessingHoursAndTimingPolicy`), carried through the editor document, the draft read
models, the capture payload (`ProcessingHours`, `ProcessingHoursFormatVersion = 4`) and the storefront quote.
`ShippingAuthoringPreparationProcessingMode` makes 24/7 elapsed operation a **deliberate** choice
(`Continuous`) instead of a silent default; `WorkingWeek` counts preparation minutes **inside** the stored
week (and refuses a week with no working day), while the transfer allowance stays elapsed and is counted
once. A new commitment on a method that has a timing pair but no processing week is refused
(`shipping.scheduling.timing_policy_unconfigured`) rather than treated as instantly ready. Historical v1–v3
snapshots keep their exact old meaning; nothing is backfilled or re-labelled.

**D — the capture graph bug is fixed and proven on the canonical install.** `CaptureGraphQuery` (and the
three sibling draft readers: get, editor-document replace, type/settings readback) now include
`ProcessingSchedule`/`Days`. A publication captured before the fix is kept as immutable history and still
reads as pre-264 data; a publication captured after it is format 4 with the processing week.

## Proof (each check once, no green loop)

* **Shipping** `ShippingDispatchSchedulingAuthority264R1Tests` + `ShippingCapturedRevisionQuoteTests` +
  `ShippingDispatchSchedulingAuthority264Tests` — **32/32 passed** (`0/0`, `0/30`, `30/0`, window-constrained
  same-day plan, processing-week counting, unconfigured policy refusal, legacy v1–v3 compatibility).
* **Order** `AdminDispatchPreparation264R1RelationalTests` on real `tooba_alpha` — **2/2 passed**
  (explicit zeros accepted end to end; a decreasing/partial triple refused by the database).
* **Canonical connected proof** (`tb264r1-connected-proof.txt`, read-only after the HTTP writes):
  * pre-fix capture `01a1cd1e-…` → `format_version = 3`, `ProcessingHours = null` (the defect, preserved as
    history);
  * after the include fix, `PUT editor-document` → `200` revision 5/6, then
    `POST publication` → `200`, `selectedFormatVersion = 4`, snapshot
    `01a1cd2d-dc41-7000-abf0-322ca18255c4`, selection version 5, `selectedMatchesCurrentDraftRevision = true`;
  * the persisted body reads `FormatVersion = 4`, `PreparationMinutes = 240`, `HandoffMinutes = 60`,
    `PreparationMode = 1` (`WorkingWeek`), `ProcessingHours.TimeZoneId = Asia/Tehran` with 7 days / 6 working,
    `CarrierAcceptance` 7 days.
* **Live storefront projection** (`tb264r1-storefront-projection.txt`): a task-labelled guest cart
  (`01a1cd1f-…`) through the real endpoints returns the captured preview for its seller group, quoted from the
  **seller-owned** capture (`01a186f7-…`, rev 10 / selection 11) and truthfully refuses dates with
  `storefront.captured_group.carrier_schedule_missing` — it does not borrow the Store-owned method and does
  not invent a date.
* `dotnet publish` of `Tooba.Host` and the whole solution build: **0 errors**.

## Deploy state (canonical install, unchanged topology)

ONE database `tooba_alpha` @ 127.0.0.1:5432 (SingleStore, tenant `store-alpha`), Host 5088, Next 3000.
Migrations applied by the repository runner: `shipping.__ef_migrations_history` head
`20261112120000_AddProcessingHoursAndTimingPolicy`, `"order".__ef_migrations_history` head
`20261112130000_RelaxPreparationDispatchZeroAllowances`. Host republished to `artifacts/publish-primary`,
restarted (**PID 8964**), `/health` 200 and `/health/ready` 200 (SingleStore). Frontend unchanged in this
round (BUILD_ID `Hou23ETYm3l_DFIPdinpW`, PID 41488). No disposable/test/parallel/Docker database, no 15433,
no edition/connection/port switch, DataProtection and CapturedPreview untouched (local ON / checked-in OFF),
no manual DDL/business SQL/reset/reseed/backfill, no user order/cart/parcel row read or written, no provider
call. Task-labelled fixtures only: draft `01a17d0d-…`, guest cart `01a1cd1f-…`, line `01a1cd20-…`,
snapshots `01a1cd1e-…` / `01a1cd2d-7601-…` / `01a1cd2d-dc41-…`.

## Known limits (explicit)

* No **Store-owned in-stock offer** exists in the canonical catalog, and a seller's own shipping
  configuration is user data; therefore no live *positive* customer-date row or live *positive* selection
  write could be produced without writing someone else's configuration or minting a new Store offer. Neither
  was authorised, so neither was done. The positive calendar/deadline math is proven by the 32 focused tests
  and by the format-4 payload that the same authority consumes.
* `259`–`263` behaviour is untouched; `265` (courier-origin return ↔ Returns linkage) remains **NOT ISSUED**.
* The preparation week is a warehouse processing window, not a claim about carrier booking; no slot is
  reserved anywhere.
