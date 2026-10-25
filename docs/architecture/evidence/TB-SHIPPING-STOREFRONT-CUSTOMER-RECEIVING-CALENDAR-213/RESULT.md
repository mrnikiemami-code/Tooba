# TB-SHIPPING-STOREFRONT-CUSTOMER-RECEIVING-CALENDAR-213 — RESULT

Base `09051dcb7dd6cfb212f940bd5615cb6bbae0dafd`. Mode `BOUNDED_EXISTING_CALCULATOR_CUSTOMER_RECEIVING_CALENDAR`.
One concrete additive slice inside the existing calculator; no new service, engine, DbContext, endpoint or flag.

## User decisions this slice implements (rules §29, not re-asked)

- "1–3 days delivery" = **customer receipt** promise measured from the **order date**; seller preparation and
  carrier transit sit *inside* the promise, so no preparation days are added again.
- Authored week = **customer receiving days/hours** (each working day has its own interval), **not** the carrier
  pickup schedule and **not** `RequestedPickupAtUtc`.
- "به روز تعطیل ... روز تعطیل را بپر از روش" — closed days are skipped both when counting the minimum and when
  offering dates; a closed date is never offered.
- Reference = order date (offset 0, only eligible if that day is working); next working day = #1, next = #2 …
  Example: order Saturday, Sunday working #1, Monday closed, Tuesday working #2 ⇒ earliest **Tuesday**.
- Selected hours come from that date's own configured interval — never the static `9-12…` array, never browser clock.
- The stored maximum delivery duration is **not** dropped or remapped to preparation days.
- Not implemented/claimed: intraday cutoff, past-slot filtering, slot splitting/capacity, exceptional holidays,
  horizon derivation from the authored max range.

## Exact files

1. `src/backend/Modules/Order/Tooba.Order.Application/Storefront/Services/StorefrontShippingCalculator.cs`
   — additive only:
   - `BuildCustomerReceivingCalendar(DateOnly referenceLocalOrderDate, int minimumWorkingDayOffset, StorefrontShippingReceivingWeek week, int deliveryHorizonDays)`
   - `MaxReceivingWorkingDayOffset = 365`, `MaxReceivingHorizonDays = 90` (explicit bounds, rejection not clamping)
   - private `ResolveReceivingWeek` (validates 7 unique in-range days + positive non-reversed working interval)
   - private `FindEarliestReceivingDate` (offset 0 = reference if working else next working day; N≥1 = N-th
     working day strictly after the reference)
   - every existing member (`ComputeMinimumDeliveryDate`, `ComputeGroupMinimumDeliveryDate`, `BuildDeliveryDates`,
     `DayTimeWindows`, `ValidTimeWindows`, `EnsureDeliveryNotEarlier`, formatters) untouched.
2. `src/backend/Modules/Order/Tooba.Order.Application/Storefront/Models/StorefrontOrderModels.cs`
   — four cohesive records appended: `StorefrontShippingReceivingDay` (`DayOfWeek`, `IsWorking`, `OpensAt`,
   `ClosesAt` as `TimeOnly`), `StorefrontShippingReceivingWeek` (7 days), `StorefrontShippingReceivingSlot`
   (`Date` + that day's interval), `StorefrontShippingReceivingCalendar` (`EarliestEligibleDate` nullable +
   `AvailableDates`).
3. `src/backend/Modules/Order/Tooba.Order.Tests/Storefront/StorefrontShippingReceivingCalendarTests.cs`
   — 11 focused tests (new file, existing Order test project).

Errors reuse the existing stable `StorefrontOrderErrors.ShippingDeliveryInvalid` (`shipping.delivery.invalid`)
with a diagnostic suffix per fault (`receiving.week.incomplete|day_missing|day_out_of_range|day_duplicate|
working_interval_invalid`, `receiving.minimum_working_day_offset`, `receiving.horizon_days`). No new error code,
no localization resource, no silent repair/clamp/default week.

## Behavior proven (dates are real, reference Saturday 2026-10-24)

| case | expectation | result |
| --- | --- | --- |
| all days working, offset 2 | earliest Mon 2026-10-26 | PASS |
| Sunday working, Monday closed, offset 2 (owner example) | earliest Tue 2026-10-27, Monday absent | PASS |
| reference on a closed day, offset 0 | next working day, never the closed date | PASS |
| Thursday+Friday closed, offset 2 from Tue 2026-10-27 | Sat 2026-10-31 across the week boundary | PASS |
| Wednesday closed after the earliest date | Mon/Tue/Thu offered, Wed never offered | PASS |
| per-day hours incl fractional ticks | each date keeps its own `TimeOnly` (10:30:15.5–14:45:45.25) | PASS |
| fully closed week | `EarliestEligibleDate == null`, empty list, no loop | PASS |
| 6-day / duplicate-day / `(DayOfWeek)7` week | rejected, no fallback | PASS |
| reversed or zero-length working interval, offset −1, horizon −1 | rejected, no clamp | PASS |
| closed day with odd interval | interval unused, no repair | PASS |
| two independent groups (offset 1 vs 3, 09–12 vs 16–21) | independent minimums and hours, inputs unmutated | PASS |
| legacy calculator facts | `+2+3 = Thu 2026-10-29`, 5 consecutive dates, 4 static windows, empty when earlier | PASS |

## Verification actually run

- `dotnet build src/backend/Modules/Order/Tooba.Order.Tests/Tooba.Order.Tests.csproj` → succeeded, 0 warnings/0 errors.
- `dotnet test … --filter FullyQualifiedName~StorefrontShippingReceivingCalendarTests` → **11 passed / 0 failed / 0 skipped**
  (first run 9/11 exposed two wrong *test expectations* — my own off-by-one in the constant Tuesday and an
  interval attached to the wrong date in the two-group test; the implementation semantics were correct and the
  assertions were fixed, no assertion weakened).
- No whole-solution/Host build, no 645-suite, no browser/DB/container/HTTP, no data write, no restart, no package install.

## Boundary kept

`ProjectAsync`/`SaveSelectionAsync`/`CommitAsync`, DI, flags, live checkout price, draft publication, the legacy
carrier-pickup `OperatingSchedule`, schema/DbContext/migrations, Host and frontend/storefront CSS are **unchanged**.
Order references no Shipping Domain/Infrastructure type (only the pre-existing Fulfillment Contracts using stays).
This is a tested computation primitive: **Storefront integration is NOT complete** — nothing calls it yet, and the
storefront still offers the legacy static windows for the live path.

## Remaining work (named, not guessed)

1. **Single next integration step**: a reviewed projection/selection/commit-wide wiring that builds this calendar
   from the *published* method week (per group, in the method timezone) and replaces static `DayTimeWindows`
   only after publication authority exists; must keep each shipping group independent.
2. Published-method authority (TB212-R1 slice: owner-scoped immutable revision read through Shipping Contracts)
   is still unimplemented, so no customer path may read draft tables.
3. Owner/edition authorization for `Marketplace`/`Seller` (server-authorized scope, no browser-supplied owner).
4. Measurement/amount condition: goods amount of the **same group after discount and tax**, sourced from
   pricing/Cart/Order — Shipping must not compute it.
5. Tax/discount allocation source of truth per group, and the money/day mapping for the legacy aggregates.

## Timing

Claimed `2026-10-25T09:29:06Z`; implementation stop respected (code complete ≈minute 12); build+one focused test
run+evidence+commit/push by ≈`09:46Z` (~17 min, inside the 20-minute box). One correction round used (the two
test-expectation fixes), which is the allowed maximum. STOP_FOR_ARCHITECT / Automatic-Next-Task NONE.
