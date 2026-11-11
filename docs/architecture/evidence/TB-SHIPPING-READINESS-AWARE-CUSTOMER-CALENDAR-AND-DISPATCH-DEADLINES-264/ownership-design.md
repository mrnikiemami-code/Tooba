# TB-264 — ownership and business semantics of the physical timing policy

Scope: turning «now» from an assumed goods-ready instant into an **explicit, owned, editable physical
preparation/transfer policy**, and carrying the derived operational deadlines from the storefront calendar
and the accepted evidence into the existing operational queue and order view. No new queue, no new settings
app, no second operational UI, no Returns/financial work.

## 1. What was inspected (read-only) before choosing the model

| Candidate source | Finding | Decision |
| --- | --- | --- |
| `ShippingRatePolicy` / `ShippingQuote` `PreparationDays` + `LeadDays` | Real, but **calendar days of a rate row**, already counted as the customer promise; not physical elapsed minutes and not editable per method policy | NOT reused as the preparation allowance. Untouched. |
| `ShippingAuthoringMethodSettings` (payment mode + calculation mode) | The one **editable, owned, versioned** per-method policy in the authoring draft, carried into the revision snapshot, the captured quote and the storefront | **Chosen owner** of the two elapsed allowances |
| `ShippingAuthoringRevisionSnapshot` / captured revision payload | Already the durable, immutable input of pricing (`ShippingCapturedRevisionQuoteLookup` → `quote.Settings`) | Chosen carrier of the pair to the storefront |
| Carrier acceptance week (`ShippingAuthoringCarrierSchedule`) and customer receiving week | Existing calendars, already the only source of carrier/customer windows | Reused unchanged — no new week type, no "warehouse week" invented |
| Processing/work calendar for the seller | **No such source exists** | Not invented. The two allowances are **elapsed** time from the trusted eligibility instant; they are never routed through processing/carrier/customer closed hours, and this is stated in the domain docs so an operator label cannot claim working-time semantics |

## 2. Units and meaning (single, stated semantics)

- `PreparationMinutes` — **elapsed minutes** of physical preparation at origin (0…10080 = 7 days).
- `HandoffMinutes` — **elapsed minutes** to move the parcel to the carrier (0…1440 = 1 day).
- The pair is **all-or-nothing**: both absent = "timing policy not configured" (historical policy v1);
  exactly one present = typed refusal (`shipping.authoring.method_settings.invalid` /
  `shipping.scheduling.timing_policy_incomplete`). **Absent is never zero and never "instantly ready"**.
  Explicit `0/0` is a real operator statement and is allowed.
- Both are counted **once**, before the first possible carrier acceptance. Transport working days are
  counted **after** the handoff day, exactly as TB-256 established. Preparation/transfer are never added
  again per transport day, and `PreparationDays` is not double-counted.
- The pair has no relation to money: `ShippingCapturedQuoteTotal`/fee figures are untouched, and the
  storefront never exposes an admin-only deadline to the buyer.

## 3. Owning seams (write/read paths)

| Concern | Owner | Path |
| --- | --- | --- |
| Editable pair | Shipping (authoring) | `ShippingAuthoringDraft.PreflightMethodSettings(..., preparationMinutes, handoffMinutes)` → `MethodSettingsPlan` → `ShippingAuthoringMethodSettings` → `ShippingAuthoringRevisionSnapshot` → captured quote `Settings` |
| Forward/backward arithmetic | Shipping Contracts (**one** authority) | `ShippingDispatchSchedulingAuthority.BuildReceivingCalendar(..., preparationMinutes, handoffMinutes)` and `.ResolvePreparationDeadlines(...)`; `PreparationAwarePolicyVersion = 2` |
| Storefront feasible dates | Order (storefront composition) | `StorefrontCapturedGroupCompositionCore` → `BuildReceivingCalendar` with `quote.Settings` prep/handoff |
| Accepted durable evidence | Order (checkout acceptance) | `StorefrontPlannedDispatchResolver.TryResolve` → `ShippingGroupPlannedDispatch.Create` (13 dispatch columns, policy version 2) → `ShippingGroupAcceptedEvidence` |
| Revalidation/readability | Order | `CheckoutDirectory.CapturedAcceptance` compares policy version, both minutes and the triple; `OrderShippingGroupHistoryReader` replays the same facts; v1 rows read as `null` deadlines |
| Queue facts | Order → Contracts only | `OrderDispatchPlanFactsBridge` exposes `PreparationStartUtc`, `TransferDepartureUtc`, `HandoffDeadlineUtc`, `PolicyVersion` (no cross-module DbContext) |
| Queue classification | Fulfillment | `AdminFulfillmentDispatchTimingAuthority.Classify` → `AdminFulfillmentPreparationState` (`None/NotApplicable/Upcoming/Today/Overdue/HandoffMissed`), `dispatch_preparation_due` filter, `PreparationDue` count |
| Admin order detail | Order | `AdminOrderDetailComposer` → `AdminPlannedDispatchView` deadlines → `OperationalDeliveryTimeline` («آماده‌سازی از …», «آخرین مهلت تحویل به حامل …») |

No cross-module entity/DbContext coupling was added: Order owns the plan, Fulfillment reads it only through
the existing Contracts fact record, and the browser computes no business date.

## 4. Forward and backward rules (as implemented)

**Forward** (storefront calendar — customer-facing dates only):
`trusted order eligibility instant` + `preparationMinutes + handoffMinutes` = the instant the parcel can
physically be at the carrier → `ResolveFirstHandoff` on the carrier week (half-open `[open, close)`, exactly
close misses, before open waits for opening) → `AdvanceTransportDays(MAX)` → customer working dates/slots.
A method with no configured pair keeps the historical reference instant (policy v1) — no silent upgrade.

**Backward** (chosen customer date → operational deadlines):
`ResolveLatestHandoffForReceivingDate(receivingDate, carrierWeek, maxTransportDays)` finds the latest
feasible acceptance window whose forward arrival is on/before the chosen date; then
`HandoffDeadlineUtc = closesAt`, `TransferDepartureUtc = close − handoffMinutes`,
`PreparationStartUtc = transferDeparture − preparationMinutes`.
The deadline is the **exclusive close instant** and is presented as «پیش از»/«تا» accordingly; it is a
derived deadline, never a reserved carrier appointment. Three distinct facts are stored and shown
separately (latest preparation start ≠ earliest ready ≠ customer promise).

## 5. Policy versioning and history

- `PolicyVersion = 1` (legacy): 8 non-null dispatch columns, **no** deadlines; readable forever, rendered as
  «بدون موعد آماده‌سازی», never retro-labelled as prepared or overdue.
- `PolicyVersion = 2`: 13 non-null dispatch columns (same 8 + 3 instants + 2 minute inputs).
- The additive DB constraint `ck_{cart|order}_shipping_groups_accepted_dispatch_plan` accepts
  `num_nonnulls IN (0, 8, 13)` and, when the triple exists, enforces `policy_version >= 2`,
  `preparation < transfer <= handoff`, `preparation_minutes ∈ [0,10080]`, `handoff_minutes ∈ [0,1440]`.
  A partial triple is refused by the database itself (measured: SQLSTATE 23514).
- Accepted plans are immutable evidence: a later method/calendar edit never recomputes them.

## 6. Operator surfaces (reused, not duplicated)

- Method authoring general tab: «زمان آماده‌سازی» and «زمان انتقال به حامل» (minutes), part of the editor
  session payload/dirty predicate, one toast per confirmed save/publish, field-level validation, existing
  `MessageBox` for a blocking error; a timing-only patch writes the complete pair or nothing.
- Work queue: its own `dispatch_preparation_due` filter and `PreparationDue` count (the dispatch buckets are
  **not** redefined; «سررسیدشده» additionally treats a due preparation as due now), a preparation state badge,
  and a compact «آماده‌سازی تا … · خروج تا … · تحویل به حامل تا …» cell.
- Shared operations route / order detail: the existing timeline component shows the operational row in the
  carrier zone; the customer promise stays its own row; nothing financial is added or removed.
