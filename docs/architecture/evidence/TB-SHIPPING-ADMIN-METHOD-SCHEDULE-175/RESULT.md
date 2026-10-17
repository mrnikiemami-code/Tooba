# TB-SHIPPING-ADMIN-METHOD-SCHEDULE-175 — Worker Result

Task: `TB-SHIPPING-ADMIN-METHOD-SCHEDULE-175`
Parent: `TB-SHIPPING-METHOD-COVERAGE-BROWSER-174`
Channel: `tooba-main`, Branch: `main`
Expected base: `f0159086ebd42fd125b51ca5f3cfe5261102ba14`
Verdict: **PASS** (with explicit OPEN items; no customer-promise claim)

## Changes

| File | Change |
|---|---|
| `src/frontend/app/admin/shipping-services/flat-method-detail-dialog.tsx` | View-mode `readOnly` is now passed to the Schedule tab (previously a View dialog could write/toggle the schedule). |
| `src/frontend/app/admin/shipping-services/shipping-one-level-schedule-tab.tsx` | `readOnly` prop; every write path guarded and disabled; explicit read-only notice; coverage banner; customer-effect and identity-isolation disclosures. |
| `src/frontend/app/admin/shipping-services/shipping-one-level-schedule-model.ts` | `resolveScheduleCoverage` + labels/explanations mirroring the owner evaluator (unrestricted / inactive-only / ambiguous / restricted-empty / restricted) and stable Persian disclosures. |
| `src/frontend/app/admin/shipping-services/shipping-one-level-schedule-tab.test.ts` | 3 new tests: coverage semantics, customer-effect honesty, View-mode write ban + dialog wiring. |

No backend, schema, migration, DI, gate, CSS, storefront or shared-component change.

## Verification

### Frontend

- `npx tsc --noEmit` → 0 errors.
- `node --experimental-strip-types --test shipping-one-level-schedule-tab.test.ts shipping-operating-schedule-model.test.ts shipping-operating-schedule-api.test.ts`
  → **37 pass / 0 fail**.

### Backend (focused, no change required)

- `dotnet test Tooba.Shipping.Tests --filter "OperatingSchedule|ScheduleAvailability|OperatingScheduleCompatibility"`
  → **51 pass / 0 fail**.

### Isolated owner-API acceptance (real Host, disposable stack)

Environment: `tooba-tb175-pg` (PostgreSQL 16-alpine) on `127.0.0.1:55495`, isolated Host on
`127.0.0.1:5429`, database `tooba_175_scratch`, messaging `tooba_175_messaging`. Shared
`:5432`, `:5088`, `:3000`, `tooba_alpha`, `tenant-alpha` untouched. Migrations applied by the
canonical runner; host health 200 after 41 s.

`acceptance-175.js` → **30 / 30 PASS** (matrix: `assertion-matrix-175.json`, trace: `http-trace-175.txt`):

- baseline: no schedule for `tipax:standard`, sibling `tipax:express` absent, 0 DB rows;
- write `standard` (Asia/Tehran, Mon 09:00–12:00) → 200, re-read from server → exact identity,
  exact window, active; DB: exactly 1 schedule row + 1 window row;
- sibling `express` writes its own Tue 10:00–11:00 row; standard stays byte-identical;
- legacy service-level (`optionCode = null`) row created and proven immutable under method edits;
  DB shows 3 distinct identity rows (service-level, standard, express);
- full replacement keeps the same `scheduleId` and replaces only that identity's windows;
- restricted-empty (`windows: []`) is accepted as a real active row with 0 windows and is not
  silently deleted/reverted;
- fail-closed: unknown option → 400 `shipping.service_option.unknown`; body/path service mismatch →
  400; cross-service PATCH → 400 `shipping.service_scope.mismatch` (row stays active); anonymous
  GET/PUT → 401; rejected writes changed no row.

### View-mode read-only

`mode === "view"` → `readOnly` → all form inputs, add/remove, save, and activate/deactivate are
disabled, and `save()`/`toggle()`/`updateRow()`/`addRow()`/`removeRow()` return before any API call.
Covered by the new source-wiring test (no browser leg this run).

## Downstream effect (critical trace)

`StorefrontShippingService.BuildGroupHorizons` (Order module) builds customer minimum dates from
method `LeadDays` + seller preparation days and uses the static
`StorefrontShippingCalculator.DayTimeWindows`. It does **not** read
`shipping.operating_schedules`. The `StorefrontShippingOperationalShadowObserver` bridge is
observation-only and both its flags default **false** (documented never to change price/promise/
draft/order snapshot).

Consequently: the saved Admin schedule is authoritative for the Shipping operating-schedule policy
and the Admin operational quote, but it is **not** currently authoritative for customer delivery
promises. The tab states this explicitly; no successful delivery-promise change is claimed.

## Remaining OPEN (carrier-level, no owner contract in scope)

1. Real carrier calendar / working-day calendar import (OPEN).
2. Capacity enforcement or atomic slot reservation (currently `NotEnforced`) (OPEN).
3. Timezone-aware customer date/time windows driven by the schedule (OPEN; would require an
   Order→Shipping enforcement contract, shadow gate stays OFF).
4. UI can display restricted-empty but intentionally cannot create it (client form requires ≥1
   window); creating it requires the owner API directly (OPEN if a UI path is wanted).
5. Adjacent observation (not changed, outside scope): the Packaging tab also does not receive
   `readOnly` from `flat-method-detail-dialog.tsx`.

## Safety / environment

- Only task-owned disposable container/DB/ports; shared services untouched.
- No reset, stash, force push, schema rebaseline, broad suite loop, fake customer data, real SMS/PSP,
  payout, carrier quotation or shipping-share allocation.
- Contracts-only crossings and existing CQRS/Result conventions; no Host business logic.
- Existing one-level list, three actions, four non-nested tabs, RTL/CSS preserved.
- Credentials live only in the environment/untracked temp file; no secret in evidence.

## Timing

- Task claimed ~10:07 local; scratch bootstrap completed 10:47; acceptance 10:49; backend tests ~10:53.
- The 45-minute HARD box was exceeded while proving persistence + downstream truth; work was finished
  to a valid PASS state rather than leaving an unverified slice. No further task was claimed.

Evidence-Commit: this commit (HEAD == origin/main)
