BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-CARRIER-ACCEPTANCE-CALENDAR-AND-DISPATCH-PLAN-256-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Status: PASS
Decision: REAL_UNDERLINED_TIMING_TABS_CONCISE_OPERATOR_COPY_AND_BOUNDED_NEW_ACCEPTANCE_FEASIBILITY_CORRECTIONS
Base-SHA: 162a319ad3d0ded796e5a474d1b269c26fa81be2
Issued-Expected-Base: 639947fc5573a3ae29ec1662d117c7d75e40b856 (ancestor of the checked-out issue/handoff doc commits; nothing unrelated was reset)
NOT-Accept: Architect review, user visual acceptance, live Tipax and 257–260 remain NOT claimed by this worker result.
automaticNextImplementationTask: NONE
STOP_FOR_ARCHITECT
END_TOOBA_WORKER_RESULT

# RESULT — TB-SHIPPING-CARRIER-ACCEPTANCE-CALENDAR-AND-DISPATCH-PLAN-256-R1

Worker result only. **Worker PASS is not Architect/user ACCEPT.** The user trial of the corrected tabs and
scheduling behaviour is the next step; 255 stays parked and 257–260 are not issued.

## 1. Visual correction — real tabs, not exclusive-pill buttons

`features/admin-shipping-authoring/ui/authoring-screen.tsx` now renders the two timing calendars as ordinary
underlined tabs identical in treatment to the page's existing General/Regions tabs:

- labels «تحویل به مشتری» / «پذیرش حامل», shared bottom baseline, active text + 2px `border-primary`
  underline, inactive neutral text with a transparent underline, **no filled blue selected pill**, no radius;
- `role="tablist"` with a stable `aria-label` («زمان‌بندی ارسال»), `role="tab"` buttons with stable `id`s,
  `aria-selected`, `aria-controls`, roving `tabIndex` (0 on the selected tab, −1 otherwise) and
  `role="tabpanel"` panels that carry `id` + `aria-labelledby` and are toggled with `hidden`;
- direction-aware roving keyboard model in the new pure helper `ui/timing-tab-navigation.ts`
  (`ArrowLeft` = forward in RTL, `ArrowRight` = backward, `Home`/`End` = first/last, wraparound);
- switching is **presentation only**: both editors stay mounted, neither schedule is disabled, no local edit
  is reset, nothing is saved. The page-level General/Regions tablist received the same stable ids,
  `aria-controls`, roving `tabIndex` and arrow-key handling so keyboard behaviour is uniform.

Copy hygiene (`messages/authoring-messages.ts`): the tab labels, carrier week title, one-sentence carrier hint,
the one-time copy hint and the blocker/unconfigured messages are the whole operator copy. Repetitive
paragraphs about independent calendars, provenance/UTC/source/revision/semantics are gone from the normal form.
The purposeful «کپی از ساعات تحویل مشتری» button and its single-line result remain. Validation errors and the
unconfigured empty-state action are kept. No tenant/Store/source code is printed in operator copy.

Measured authorized DOM (task-owned headless Chrome, fresh profile, read-only, no Save/Publish, no screenshot):

| fact | review draft `01a17d0d-e033…` (rev 2, untouched) | task draft `01a1beed-d7df…` (rev 3) |
| --- | --- | --- |
| tablists / timing tablist label | 2 / «زمان‌بندی ارسال» | 2 / «زمان‌بندی ارسال» |
| timing tabs | 2 × 44px high, round 0 | 2 × 44px high, round 0 |
| active tab decoration | 2px `rgb(37,99,235)` underline, transparent background | same |
| aria wiring | ids + `aria-selected` + `aria-controls` + panel `aria-labelledby` all resolved | same |
| switch to «پذیرش حامل» | customer panel hidden, carrier panel visible, **both mounted** (7 customer rows remain) | same, 7 carrier day rows visible |
| local edit retained across switches | customer day closed → carrier tab → back = still closed (`editedStatePreserved: true`), then restored to the baseline (`restoredBaseline: true`) | same |
| «کپی از ساعات تحویل مشتری» | 44×191px, enabled, visible inside the carrier panel | same |
| forbidden technical phrases in the timing section | 0 | 0 |
| keyboard (RTL) | `ArrowLeft` → carrier, `Home` → customer | same |
| horizontal overflow | 1440: none; 1024: none; 390: none (`scrollWidth − innerWidth` = −15/−15/0) | same |

The review draft's carrier acceptance week is **unconfigured** (7 customer days, 0 carrier days, 0 rows) and
the task draft has 7 carrier days — the user's own draft was read, never written: revision 2 and
`updatedAt 2026-10-27T11:10:28Z` are unchanged.

## 2. Gap A — a NEW commitment needs a configured carrier schedule

- Storefront composition now **requires** negotiated planned dispatch (`requirePlannedDispatch`): a new
  commitment without a carrier acceptance week yields the single owning availability blocker
  `storefront.captured_group.carrier_schedule_missing`, no dates, no fabricated calendar from customer hours;
  an invalid carrier week yields `storefront.captured_group.carrier_schedule_invalid`. The lawful money stays
  quoted (`Outcome.Quoted`), so the price is not hidden and nothing is invented.
- Historical compatibility is explicit, not silent: `StorefrontPlannedDispatchResolver.TryResolve` keeps the
  pure `null` success **only** for revalidating evidence stored without a plan
  (`allowAbsentCarrierSchedule: evidence.PlannedDispatch is null` in `CheckoutDirectory.CapturedAcceptance`), so
  read-only legacy orders stay readable while a new incomplete commitment fails closed.
- Focused proof: composer **12/12** (new case included) and the new resolver suite
  `PlannedDispatchCarrierRequirement256R1Tests` **3/3** (missing carrier → refused with its own reason; stored
  legacy evidence → readable and `plan == null`; configured carrier → latest feasible handoff, arrival ≤ chosen
  date).

## 3. Gap B — readiness/preparation semantics

Inspection found **no separate authoritative physical-preparation input** beyond the existing readiness instant,
so no second preparation setting was invented and nothing is double-added: the pure authority still treats
`nowUTC` as goods readiness, counts the MAX transport working days **once after the first feasible handoff**,
keeps the carrier window half-open `[open, close)` (before opening → opening; exactly closing → next valid day;
after closing → next valid day), keeps the zero-day case explicit, and filters elapsed customer slots by real
instants across different carrier/customer time zones. Customer dates and slots are then filtered by the
**separate** customer week. Policy version 1 records that assumption.

## 4. Gap C — a handoff that arrives on or before the chosen date is accepted

`ShippingDispatchSchedulingAuthority.ResolveLatestHandoffForReceivingDate` now returns the **latest** handoff
whose conservative forward arrival is `<= receivingDate` (validated by the same forward rule), so a customer
date on which the carrier is closed is still reachable from an earlier handoff instead of being rejected. The
search stays monotonic and bounded by the week-cycle bound (`transportWorkingDays * 7 + 8`); only a week with no
carrier working day at all produces `NoCarrierWorkingDay`. The customer commitment itself is unchanged.

Proof (`ShippingDispatchSchedulingAuthority256Tests`, 11/11):
MAX-never-MIN; cutoff before/open/just-before/at/after; carrier-noon vs customer 09–17 independence with
Friday-closed roll; different-time-zone midnight vs UTC; zero-day elapsed slots; every fail-closed reason;
40-day sweep asserting each resolved handoff is the latest and the next one overshoots; zero-day cross-zone
instant comparison (Asia/Tehran 09:00 = 05:30Z keeps the 06:00Z customer slot); sparse one-day week at MAX
duration without overflow; readiness counted once with slots from the separate customer calendar.

Simulated cutoffs (code/fixture proven — **not yet testable through the user's live method, which has no
configured carrier acceptance week**; do not change the computer clock to reproduce them):

1. carrier Mon–Fri 09:00–17:00, Monday 08:00 readiness → first handoff Mon 09:00;
2. Monday 16:59 → Mon; **Monday 17:00 exactly → Tuesday** (half-open window);
3. carrier Wednesday closes 12:00 while the customer works to 17:00 → 12:00 handoff, arrival next day;
4. carrier Friday closed + Thursday after close → Monday;
5. MAX 3 with Monday readiness → earliest customer date Thursday (never Tuesday/MIN);
6. zero transport days at 10:30 with 60-minute slots → same date, first offered slot 11:00;
7. customer chooses Saturday while the carrier is closed → handoff Thursday, arrival Friday ≤ Saturday;
8. Asia/Tehran carrier 09:00 (=05:30Z) keeps the customer's 06:00Z slot (clock values are never compared).

## 5. Gap D — «موعد ارسال» as its own read-only row

`AdminOperationalDeliveryGroupView.PlannedDispatch` exposes the **existing** accepted plan (handoff date,
open/close ticks, carrier time zone, effective MAX, policy version, schedule snapshot id, source revision);
`features/order-operational-groups/ui/operational-delivery-timeline.tsx` renders it as a distinct
«موعد ارسال» row next to «تحویل مشتری». A historical group with a null plan renders **no** row and is never
recomputed. No new queue, no money/finance change, no fabricated checkout.

Live read-only order `01a1b92d-e97e-7000-999a-143350d0edaf`: 3 operational groups, 3 «تحویل مشتری» rows,
**0 «موعد ارسال» rows** (all three plans are historical null), RTL, no horizontal overflow. Static mounted
proof: 2/2 (separate row + no row for a null plan), part of the 27/27 operational mount suite.

## 6. Focused proof executed (one database only)

| Area | Command | Result |
| --- | --- | --- |
| Scheduling authority | `dotnet test …Tooba.Shipping.Tests --filter FullyQualifiedName~ShippingDispatchSchedulingAuthority256Tests` | **11/11** |
| Shipping module | `dotnet test …Tooba.Shipping.Tests` | **822 passed / 9 failed / 831** — the 9 are pre-existing, identical to the base worktree |
| Order module | `dotnet test …Tooba.Order.Tests` | **664 passed / 33 failed / 697** — the 33 are pre-existing, identical to the base worktree (relational classes needing a disposable database + the known coupon mock) |
| New carry-requirement suite | `--filter FullyQualifiedName~PlannedDispatchCarrierRequirement256R1Tests` | **3/3** |
| Composer (incl. new Gap A case) | `--filter FullyQualifiedName~StorefrontCapturedGroupProjectionComposerTests` | **12/12** |
| Timing tabs (pure) | `node --experimental-strip-types --test features/admin-shipping-authoring/authoring-timing-tabs-256-r1.test.ts` | **3/3** |
| Operational groups mounted | `npx tsx --tsconfig tsconfig.mount-tests.json --test app/admin/admin-order-operational-delivery.mount.test.ts` | **27/27** |
| Frontend typecheck | `npx tsc --noEmit` | **0** |
| Frontend production build | `npm run build` | **0** |
| Architecture guards | `npm run test:architecture` | `frontend-source-size` 18 findings — the same pre-existing family and count; both touched files were already over the limit at base (`authoring-screen.tsx` 1002→1067 lines) and no baseline JSON was edited |

No test order, dispatch, delivery, cancel, payment, user draft save or publication was performed. No disposable
database, no Docker PostgreSQL, no 15433 server: every SQL/HTTP call this task made was read-only against
`tooba_alpha`.

## 7. Deployed runtime identity (same environment, no edition switch)

- Host `http://127.0.0.1:5088` PID **21536**, `artifacts/publish-primary`, SingleStore / `store-alpha` /
  `tooba_alpha`, `/health` 200.
- FE `http://127.0.0.1:3000` PID **38148** (`next start -p 3000 -H 127.0.0.1`), BUILD_ID
  **`pz_oynTTa4nZUOPrjSPOm`**; `/fa` 200 and the review route 200.
- No migration was added or applied by this task. DataProtection preserved. Local CapturedPreview ON,
  checked-in gates OFF. 255 remains parked, live Tipax remains deferred.

## 8. Disclosure

1. One **pre-existing, site-wide** network artifact appears on admin pages: a cross-origin
   `OPTIONS http://localhost:3000/fa` CORS preflight answers 400 (a plain GET to that URL is 200). It is
   present on the operational order page too, is untouched by this change and does not block the authoring
   route; recorded, not "fixed", not used as proof.
2. The review draft has no carrier acceptance week, so the carrier/customer independence and copy behaviour
   are proven on the mounted suites and on the task-owned configured draft (`01a1beed…`, 7 carrier days), not
   by a saved user configuration. The user's draft was never saved or promoted.
3. `ShippingPersistenceModelTests` model/migration sync and the coupon mock test stay RED, exactly as before.

## 9. User trial checklist (please review on the canonical route)

Route: `http://127.0.0.1:3000/admin/shipping-services/authoring/01a17d0d-e033-7000-af43-49a1daa28822?mode=edit`
Read-only operational order: `http://127.0.0.1:3000/admin/orders/01a1b92d-e97e-7000-999a-143350d0edaf`

1. «تحویل به مشتری» / «پذیرش حامل» look like the page's other tabs (underline under the active one), **not**
   two blue pressed buttons.
2. The two calendars are independent: editing one does not disable, copy or reset the other; switching tabs
   keeps your unsaved edits; the copy button is a deliberate one-time action.
3. A method with no configured carrier acceptance week cannot offer new customer availability — it shows one
   clear blocker instead of an invented calendar.

Open remaining issues (unchanged, not this task): tracking generator duplicates, shipped quantity missing from
the remaining calculation, non-delivery/return/resend absence, optional independent parcels vs forced common
package, incomplete due queue/shared operational view. 257–260 are NOT issued.
