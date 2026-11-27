# TB-296 P1-C final connected report

Status: FAIL — safe fixture/authority work and one actual per-ID handler completed; 59 of 60 immutable IDs remain NOT_RUN.
Source: d22849096481f3f34af28aaa1c865edce163bd28; corrected product 58a83f7a59a3625c4a5c941a5a3e8dfbb008e6d1; frontend b0c6467249bb945d661490a0eab407ec487e735c.

## Build/served identity
- Inventory.Tests and Offer.Tests build PASS; suites PASS.
- Final Host publish PASS; DLL SHA-256 BC839DC66053490B1A1DE64DAA6D655A7C4BF912D4B4DDCB32C5E57ED347C932; Host 5088 ready; DB tooba_alpha; Next 3000 current frontend build 7tcRkdNm2wPHBfpPYVSvT.
- Scheduler self-test PASS; node --check for scheduler and feedback helper PASS.
- No product, test, guard or matrix changes in this round.

## Actual independent pass
- R10-12-N PASS via p1c-inventory-feedback.cjs on unique owned Draft `eecf9756…`, stock `01a21b0b…`: blank reason rejected with zero write; valid 12 persisted with exact owner revision/operationId; controlled precommit abort produced MessageBox; state remained 12; reload preserved 14/reason; retry persisted 14; independent SQL showed onHand14/reserved0/factCount3. Exact fixture cleanup: 0 owned offers/positions/facts.
- Fixture authority: current DB backup locally preserved outside Git, pg_restore list PASS; current access before/after matches row-for-row (32 ceiling rows, 0 enabled, 0 roles, 0 assignments). Synthetic role/assignment removed. Historical pre-run state remains UNKNOWN.
- DB dump is no longer tracked on branch; prior Git blob remains in history and is not claimed deleted.

## Ledger
`p1c-final-ledger.json`: 60 immutable IDs, PASS=1 (R10-12-N), FAIL=0, NOT_RUN=59. No name-only or inferred PASS. D2/F-BENTRY clauses have no actual final execution in this run.
- Original 42: 1 PASS, 41 NOT_RUN.
- Additive OWN-01..10: 0 PASS, 10 NOT_RUN.
- D2-ORDER/HOLD/ALLOCATION/PARTIAL/RECOVERY/HTTP + F-BENTRY-01/02: NOT_RUN.
- Prior price Toast and sequential lifecycle remain developmental support only; not repeated or promoted.

## Remaining
All independent handlers for admin/seller create/edit, registry/geometry, history, security, concurrency, order/cart/returns/fulfillment, D2 and B-entry controls are unfinished; NOT_RUN rows are not acceptance failures and no final ACCEPT exists. No screenshots.
