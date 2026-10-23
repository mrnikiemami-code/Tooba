# TB-SHIPPING-ADMIN-AUTHORING-RATES-WEEK-UI-203-R1 — Worker result

- Task: `TB-SHIPPING-ADMIN-AUTHORING-RATES-WEEK-UI-203-R1`
- Parent: `TB-SHIPPING-ADMIN-AUTHORING-RATES-WEEK-UI-203`
- Mode: `BOUNDED_TIME_BOUND_DISCARD_AND_RATE_READ_PRESENTATION_REPAIR`
- Base: `f4f95bc8afada6ac528c124a8951ffa9622709c8` (verified `HEAD == origin/main`)
- Production-Functional-Acceptance: `PENDING`
- Schema/Data/Flag changes: `NONE`
- Visual: `NOTRUN`
- Stop-State: `STOP_FOR_ARCHITECT`
- Automatic-Next-Task: `NONE`

## Defect 1 — per-bound time provenance (`model/editor-week.ts`)

Cause: one `hoursEdited` flag per DAY made `canonicalTime` require `HH:mm` for BOTH bounds, so editing
only the start re-parsed the untouched stored end and produced `dayTimeInvalid` plus `endTime: null`.

Fix: provenance is now per BOUND (`startEdited` / `endEdited`). `withDayTime` marks only the edited
bound; `withDayWorking` marks neither. Serialization canonicalizes each bound independently, so an
untouched bound keeps its exact stored token (`18:00:00.5000000`), an edited one becomes `HH:mm:00`.
Range checks compare real values through `timeValue` (100 ns ticks), so `18:00:00` equals
`18:00:00.0000000` and a zero-length interval stays invalid. `sameDays` compares values, not spelling,
so a reverted day is clean again. No timezone/DST algorithm was added.

## Defect 2 — confirmed Cancel Edit reset only General (`model/authoring-drafts.ts`, screen)

Cause: `enterDestination("view")` reseeded only `form` and cleared the session intent; `rates`/`week`
were left modified and could be resurrected into the next PUT.

Fix: new cohesive `seedAuthoringDrafts` / `resetAuthoringDrafts(document)` returns General + rates +
week from the SAME confirmed baseline. The screen's `formSeed` effect uses the seed helper and the
confirmed Cancel Edit path calls `resetAuthoringDrafts(document)` then `setForm/setRates/setWeek` and
`controller.edit({})`. The `commitUncertain` branch is unchanged in semantics (latch kept, only the
explicit fresh read recovers) and now also drops the local drafts. Back-destination and busy/blocked
behaviour were not touched.

## Defect 3 — pending timezone is local work (`model/editor-week.ts`, messages, week editor)

`weekDirty` now returns true for a non-empty pending timezone on an unconfigured draft (empty is
clean), and `weekIssues` returns `pendingTimeZoneUnapplied`, which blocks Apply with a localized
configure-or-reset message. `buildWeeklyScheduleEdit` still returns `undefined` for unconfigured
drafts, so no fabricated days/wrapper and no no-change PUT; `configureWeek` still creates only seven
CLOSED local days from the explicit timezone. The week editor exposes an explicit clear (reset) action
for the pending text.

## Defect 4 — read-only conditions (`ui/rate-presentation.ts`, rates editor)

`rateConditionText` (stored row) and `rateDraftConditionText` (draft text) render ALL real axes —
weight, item count and merchandise amount — with null/open bounds shown as `—`, exact decimal token
text for money, and `noCondition` only when no axis exists. The read-only row in
`AuthoringRatesEditor` now uses the wired formatter instead of a weight-only branch.

## Verification actually executed

| Command | Result |
| --- | --- |
| `node --experimental-strip-types --test authoring-rates-week-203-r1.test.ts` | PASS 9/9 |
| `node --experimental-strip-types --test authoring-rates-week-203.test.ts` (203's facts after the fix) | PASS 7/7 |
| `npx tsc --noEmit` | PASS (exit 0) |
| feature-boundary / import-boundary / test-discovery / flat-folder guards | PASS 7/7 |

Not run (per task bounds): whole 48-fact foundation sweep, backend/Host/solution/build/source-size
sweeps (their four pre-existing failures are untouched, no baseline edited), mounted/browser/visual.

Coverage honesty: defects 1, 3 and 4 are covered by pure-function regressions plus source-level
wiring assertions for the components; defect 2 is covered by the extracted reset helper plus a
source assertion that the real `enterDestination` block calls that helper and reseeds all three
drafts — **no mounted React/browser run was performed or claimed**.

## Deviations / limits

- Two bounded test-harness corrections (a malformed-input fixture that was actually a missing-pair
  case, and a slice that matched an earlier `setMode("view")`); no production-code change for either.
- No backend/DTO/API/transport/schema/Host/design-system/package/baseline change; no new framework.
- Visual acceptance is NOT claimed; no certification or delivery-complete claim.
