# TB-296 P1-C — group A/B/C executable results

Authoritative run: run-2 (post test-harness corrections). Test-only scope; no product source, architecture, guard, contract or immutable-matrix changes.

- Module: `p1c-group-abc-cases.cjs` (composes group A handlers with the real group B `p1c-edit-history.cjs` and group C `p1c-pricing-fulfillment.cjs`).
- Coverage: `implemented 22 / 60 immutable` (exit 1 expected; 38 out-of-scope IDs intentionally unimplemented).
- Ledger: `p1c-group-abc-ledger-r2.json`; evidence: `p1c-group-abc-evidence-r2/`; run log: `p1c-group-abc-run-r2.log`.
- Scoped counts: PASS 20, FAIL 1, NOT_RUN 1 (1 scoped dependency + 38 out-of-scope).
- Cleanup: residual P1C offers 0, positions 0; every evidence `afterCleanup` empty.
- Mounted regression: pass 28, fail 0 (`p1c-group-abc-evidence-r2/p1c-mount-regression.log`).
- Provenance: `p1c-group-abc-ledger.json` / `p1c-group-abc-evidence/` is the pre-correction diagnostic run (5 FAILs: 4 test-harness defects now fixed, R10-08-N previously masked). `p1c-group-abc-ledger-r2.json` / `p1c-group-abc-evidence-r2/` is authoritative.

## Per-ID results

| ID | Status | Evidence | SHA-256 (12) |
| --- | --- | --- | --- |
| R10-01-P | PASS | `R10-01-P.json` | 6efc583da7f7 |
| R10-01-B | NOT_RUN | `R10-01-B.json` | 9d82f6efd3fa |
| R10-02-P | PASS | `R10-02-P.json` | 0bad69e80ec6 |
| R10-02-N | PASS | `R10-02-N.json` | ccb67246283d |
| R10-02-B | PASS | `R10-02-B.json` | cc597807f1c6 |
| R10-03-P | PASS | `R10-03-P.json` | 987a11441727 |
| R10-03-N | PASS | `R10-03-N.json` | 8dfa88baeda1 |
| R10-03-B | PASS | `R10-03-B.json` | fcd1f5ada991 |
| R10-04-P | PASS | `R10-04-P.json` | f84287c1c0b0 |
| R10-04-N | PASS | `R10-04-N.json` | 7153c565b5ef |
| R10-05-P | PASS | `R10-05-P.json` | 7c89434ca967 |
| R10-05-N | PASS | `R10-05-N.json` | 86174880c699 |
| R10-05-B | PASS | `R10-05-B.json` | 659f2927556b |
| R10-06-P | PASS | `R10-06-P.json` | 15841ea5163b |
| R10-06-N | PASS | `R10-06-N.json` | 079701fa4677 |
| R10-06-B | PASS | `R10-06-B.json` | b5c0767d4a53 |
| R10-07-P | PASS | `R10-07-P.json` | 0c61d5920438 |
| R10-07-N | PASS | `R10-07-N.json` | f3d792abbef4 |
| R10-07-B | PASS | `R10-07-B.json` | e73a1823d006 |
| R10-08-P | PASS | `R10-08-P.json` | 2fb5f976726f |
| R10-08-N | FAIL | `R10-08-N.json` | fec82900ebcd |
| R10-08-B | PASS | `R10-08-B.json` | 548bf2310017 |

## Genuine FAIL — R10-08-N

Blank price is not a typed failure and causes an unintended write:
- amount=100000 -> HTTP 200 persisted=100000
- amount=null -> HTTP 500 `platform.unexpected`
- amount="<omitted>" -> HTTP 200 persisted=0
- Persisted price facts after the case: 2 [[null,100000],[100000,0]]
- Expected per R10-08-N: typed error, no unintended write. Recorded as FAIL with raw evidence `R10-08-N.json`.

## Dependency NOT_RUN — R10-01-B

- Served SingleStore DEV tenant `store-alpha` exposes only `[Marketplace]`; two independently allowed channels need a second authorized StoreCommerce context/edition switch (not authorized).
- A changed channel is denied earlier with `offer.channel.not_allowed` (observed). Raw evidence `R10-01-B.json`.

## Preserved existing PASS (not re-executed, integrity re-verified)

- R10-01-N: unchanged=true (cb655450390f)
- R10-04-B: unchanged=true (4f1a7021d9c8)
- R10-10-P: unchanged=true (807c768a1416)
- R10-10-N: unchanged=true (735c324f3802)
- R10-10-B: unchanged=true (7fddcf4a6f05)
- R10-12-N: unchanged=true (ed722d0004ae)
- R10-13-N: unchanged=true (ed722d0004ae)

## Test-only harness corrections (recorded for review)
- p1c-pricing-fulfillment.cjs createOffer: isolate only the new candidate offer id (S.snapshot([offerId])) instead of all accumulated ids in the same case; the prior breadth wrongly failed the second fixture in R10-07-P/N.
- p1c-pricing-fulfillment.cjs: normalize API-omitted null optional fields with '?? null' (minimumOrderQuantity, maximumOrderQuantity, customReturnWindowDays, fulfillmentResponsibility, fulfillmentShippingTypeId); the served detail contract omits null-valued optional fields. No invariant weakened.
