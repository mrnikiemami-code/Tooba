# P1-A architect recovery validation

Status: BLOCKED — no product modifications; A3 owner-contract lifecycle and A4 fault injection now executed against real PostgreSQL, but several A1/A2/A4 provider clauses and advanced A3 cases remain NOT_RUN.
Candidate: 9b33bdf154181ffd12c42173d41a32fca2e274ec; product 321e3a641a5f159d1af893dc222c8bce62094fab.

## Build/guards
- Host build PASS.
- Offer 136/136, Pricing 13/13, Inventory 53/53, Cart 29/29, Returns 191/191 PASS.
- Host architecture 82 failures vs baseline 82; 0 new/0 fixed. `recovery-host-arch-comparison.json`, baseline/recovery TRX. Unrelated failures untouched.
- Frontend typecheck/build PASS; test:i18n 19/19 PASS; frontend architecture 27 baseline size failures, none of the four P1-A compatibility files.
- Strict OpenSpec exit 0 with existing warnings.

## Relational
- Candidate Order 39 failed/730 passed/2 skipped; baseline 39/730/2.
- Candidate Fulfillment 28 failed/482 passed; baseline 28/482.
- Same failure counts on exact disposable DBs; no task-introduced relational regression. Baseline failures remain and were not repaired.
- Disposable DBs/roles/proxies dropped; Host restored to baseline PID/hash.

## A3 — actual Inventory owner contracts (tb239 PostgreSQL, test-only harness)
PASS:
- First allocation 20 on platform offer at Marketplace location; availability 20.
- Hold 5 -> allocationHeld 5/reserved 5/available 15.
- Release -> 0/0/20.
- Hold 5 -> consume -> physical 95, allocation 15, held 0, available 15.
- Same consumerKey consume replay -> no second debit.
- Cross-position shared cap: hold 10 on one position, 15 on second rejected `inventory.supply.unavailable`, 10 on second succeeds, held=20/available=0.
- Expiry reclaimer releases held 5 -> available20; paid commit keeps held5/available15; paid consume -> 95/15/0.
- Damaged replacement hold uses physical stock while new-sale allocation remains 19/held0; no new-sale bypass.
- Fault injection: throw before owner commit -> 0 allocation/0 facts; throw after durable commit -> exactly 1 allocation/1 fact; exact replay same operation/actor returns success with 1/1, no duplicate.
NOT_RUN:
- First-allocation concurrent race, concurrent release/consume, partial obligation release, protected-commitment fulfillment reassignment, and full order-service paid recovery/replacement paths.

## A1/A2/A4 provider
PASS/observed:
- New-SHA HTTP price mutation produces durable fact and invariant history key with targetId; `PriceChanged` summary is no longer raw English for new entries.
- New three-culture backend history test (fa-IR/fr-FR/en-US) passes.
- Prior validation evidence remains for owner scopes/unknown/inactive/foreign denial, stale/missing/replay and concurrent price/physical winners; fixtures were cleaned.
NOT_RUN on new SHA:
- Full repeated HTTP A1 owner-scope matrix and A2 concurrent physical/price matrix (source/unit coverage only in this recovery run).
- Inventory history API target/location after new write: admin inventory mutation returned 403 on selected seed fixture; no valid new inventory fact was created.
- Admin-vs-seller capability API matrix on the new SHA; fault injection only through test-only allocation decorator, not through Offer HTTP command.
- Full shared registry locale/shell proof remains P1-B/C.

## Cleanup
- P1A harness Offer/locations/allocations/facts/reservations removed from tb239; `P1A-%` locations/offers = 0.
- tooba_alpha seed price restored to 876543; test price fact removed.
- Temporary DB `tooba_p1a_mkt_validate` dropped; disposable test DBs/roles dropped; proxies stopped.
- Candidate Host stopped; baseline SingleStore Host restored with DLL SHA-256 1A9604BD1889294542854B31A0DECB0D6F285A8788E6028A03B2EE63A32B23C8.

No product, guard, contract, or test modifications were made in this validation turn.
