# TB-SHIPPING-MVP-DESTINATION-066 — evidence summary

Full narrative: [`docs/planning/shipping/SHIPPING-MVP-DESTINATION-066.md`](../../../planning/shipping/SHIPPING-MVP-DESTINATION-066.md).

## Verdict

**PASS (scoped)** — a validated, city-capable customer destination identity now travels from the
real storefront selection through the projection boundary into shadow observation, without
changing the legacy quote, and the 065 admin picker stale-response race is closed.

* Base SHA: `9f297f43fb8b7faa9290bfb39dd85aef71095a99` (exact match, no source drift)
* Task row: `818761fa-ce0c-4a83-91e0-30657b7858b6`

## Files

| File | Purpose |
| --- | --- |
| `task-receipt.json` | claimed Bridge task row and contract metadata |
| `log-focused-tests.txt` | builds, targeted backend tests, frontend tests, typecheck, architecture guards |
| `log-live-check.txt` | non-mutating liveness/canonical-error check against the user's Development host |
| `RESULT.bridge.txt` | the exact result envelope posted to Bridge |
| `post-result.js` | poster that validates the envelope, then posts result / completes task / sets Idle |

## Destination flow

**Before:** the storefront city `<select>` only touched local form state; the projection request
carried `provinceName` alone, so the observer was hard-wired to
`ResolveLegacyAsync(country, province, cityName: null, serviceAreaName: null)` and identity could
never be finer than the administrative area.

**After:** the existing projection calls send the selected city; the request/query/observation
input carry an optional `CityName` and optional explicit `DestinationIdentifiers`; the observer
prefers explicit ids validated by `IGeographyLookup.GetByIdsAsync`, otherwise resolves province
**and city** through the canonical resolver (exact code-or-label match, area-scoped city, parent
area derived, `Ambiguous`/`Unresolved` reported). Anything unresolved stays `DestinationNotComparable`
and is never guessed; an invalid explicit id never falls back to text.

## Legacy invariance

`StorefrontShippingService.ProjectAsync` still receives exactly its five original arguments.
`ShadowEnabled`/`PrerequisitesConfirmed` stay false, so the gate returns `Disabled` before any
destination work — zero extra Geography and zero extra Shipping queries on the live path.
No Checkout/payment/order snapshot/pricing/eligibility file was touched. No cutover.

## Picker race

Two independent `createLatestRequestGuard()` generations (cascade and service-area) gate every
state write, and the country `onChange` no longer fires a duplicate fetch. A slow country-wide or
previous-area city response can no longer repopulate the list.

## Validation

| Check | Result |
| --- | --- |
| `Tooba.Order.Tests --filter ~Storefront` | 121 passed / 0 failed |
| observer + rate-shadow focused subset | 27 passed / 0 failed |
| `shipping-destination-picker.test.ts` + coverage model + shipping api | 23 passed / 0 failed |
| `npx tsc --noEmit` (src/frontend) | clean |
| import-boundary / flat-folder / feature-boundary / test-discovery guards | pass |
| `frontend-source-size.guard.test.ts` | pre-existing failure in 4 files this task did not touch; baseline not widened |

## Safety

* No service restarted; the Development host (`:5088`, `tooba_alpha`) was left running, so it still
  hosts pre-066 binaries — stated plainly rather than reported as a live pass.
* No database writes, no migration, no schema change, no cart/order/rate/grant row touched.
* No scratch database, no synthetic server, no destructive git operation; unrelated dirty files preserved.
