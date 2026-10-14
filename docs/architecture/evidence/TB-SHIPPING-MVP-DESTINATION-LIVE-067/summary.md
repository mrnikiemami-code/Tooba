# TB-SHIPPING-MVP-DESTINATION-LIVE-067 — evidence summary

Full narrative: [`docs/planning/shipping/SHIPPING-MVP-DESTINATION-LIVE-067.md`](../../../planning/shipping/SHIPPING-MVP-DESTINATION-LIVE-067.md).

## Verdict

**PASS** — a real customer selection now yields stable Geography ids (country → province → city) that
enter the existing shipping projection body, demonstrated live on the rebuilt Development host with an
authenticated cart whose legacy quote is byte-identical before and after the destination metadata.

* Base SHA: `001fa35f7599b4b8dd7d489b9cdc62836ab18ed4` (exact match, no source drift)
* Task row: `bf6e4b5a-8b44-4442-ac4e-41e4183b9cf5`

## Files

| File | Purpose |
| --- | --- |
| `task-receipt.json` | claimed Bridge task row and contract metadata |
| `log-host-provenance.txt` | identity + controlled restart of the Development host (PID 38716 → 11540) |
| `log-live-geography.txt` | live unauthenticated read of the new public Geography routes + hierarchy proof |
| `log-live-cart.txt` | live authenticated end-to-end proof, incl. legacy quote invariance |
| `live-destination-proof.ps1` | the reproducible canonical-flow script used for that proof |
| `log-test-artifacts-and-cleanup.txt` | task-owned disposable database artifacts |
| `log-focused-tests.txt` | engines, tests, typecheck, architecture guards |
| `RESULT.bridge.txt` | the exact result envelope posted to Bridge |
| `post-result.js` | validates the envelope, then posts result / completes task / sets Idle |

## The two things that had to be real

1. **Customer-readable stable ids.** The admin Geography read is permission-guarded, so a new
   Geography-owned public read-only surface was added
   (`/v1/storefront/geography/places/**`, GET only, Contracts-only, no authorizer, no service areas).
   Live: IR resolves to a stable country id, `alborz` to a province id whose `parentId` is that country,
   and `کرج` to a city id whose `parentId` is that province; the area-scoped city call (4) is a strict
   subset of the country-wide call (47).
2. **The ids reach the existing projection body without touching the legacy quote.** Via the canonical
   flow (register → login → guest cart → real offer line), the projection returned the same 5 real
   methods with the same prices (`post:standard:120000`, `tipax:express:260000`,
   `tipax:standard:180000`, `store_courier:75000`, `in_person:0`) and the same `8702542` subtotal with
   and without `cityName` + `destination` — `identical projection payload: True`.

## Fail-closed behaviour

Exact matching only; ambiguity collapses to `null`; a city whose parent is not the selected area, an
area from another country, or a city without a parent area all produce no identifiers. A malformed city
id in a live request neither errored nor became a trusted identity (the gate is off, so metadata is
inert). Old clients sending only `provinceName` are unchanged. If the public read fails, the screen
falls back to the legacy static city catalogue.

## Safety

* Exactly one process restarted: the positively identified Development host (the only `Tooba.Host` on
  the machine), because it locked the Host output DLLs. Graceful stop; relaunched with the recorded 065
  command; `health 200`. Frontend not restarted; no other process killed.
* Development `tooba_alpha` only. Additive task-owned disposable customers/carts; no pre-existing row
  modified; no order/payment; no Shipping rate/coverage change; no migration/schema/seed/appsettings
  change. No scratch database, reset, stash, rebase or force push.

## Timebox note

The wall clock exceeded the 45-minute budget, mainly because the Host rebuild spent ~8.5 minutes in
`MSB3027` copy retries against the locked running host before the authorized restart. Work stopped
immediately after the live proof and evidence capture.
