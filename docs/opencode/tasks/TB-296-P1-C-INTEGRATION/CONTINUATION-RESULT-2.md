# P1-C fixture-release continuation

Status: BLOCKED — current DEV fixture was safely exercised and restored exactly; full 42+10/D2 matrix remains NOT_RUN/partial, not PASS. No product edits.
Source: f90a9f3b3d6af8dcd37d037d835bf9eb18123c46; product b0c6467249bb945d661490a0eab407ec487e735c.

## Fixture authority and restoration
- Verified Host 5088, Next 3000, sellerB 01a030d1-40db-7000-b90c-a0705133f0eb / actor 01a03628-3ff0-7000-a04e-6420c2e76f72.
- Trusted current DB backup: p1c-baseline.dump (3,256,887 bytes).
- Captured row-level current before-state: p1c-current-access-before.json; ceiling 32 all disabled, roles [], assignments [].
- Temporarily created role 9de2593b-ae2f-4d0f-bc04-deac599ee5fe and assignment 0624064a-0e1b-4e9c-89f1-53d2114f171f with only product.create/view/edit; effective capabilities verified.
- In finally removed assignment/role and restored ceiling to exact before rows. p1c-current-access-after.json matchesBefore=true: 32 ceiling rows, 0 enabled, 0 roles, 0 assignments, missing=[], extra=[]. Historical pre-run state beyond this captured current baseline remains UNKNOWN and not claimed repaired.

## Independent connected observations
- Fresh isolated admin offer 6f156160…: create→edit price 5000; `PUT /price` 200, Toast visible at 250ms, no dialog/error; independent GET price 5000 IRR with nonempty revision. r902-action-trace.log. R9-02 price feedback passes on fresh fixture; not a full R9-02 ID claim.
- Fresh isolated owner-contract fixture c4e9ab10… on tooba_marketplace_tb239: allocate 20→available 20; hold 5→held5/reserved5/available15; release→0/0/20; hold/consume→physical95/allocation15/held0/available15; consume replay unchanged; stale allocation revision denied with no mutation. p1c-owner-lifecycle.log. Exact fixture cleanup completed; zero remaining offers with P1C-HARNESS-FINAL.
- Existing unchanged evidence reused: typecheck/build PASS, architecture 27 baseline, i18n, gate self-tests, strict OpenSpec, Impeccable []; no screenshots.
- Original 42 IDs: only fresh feedback observation and owner-lifecycle clauses observed; all 42 as complete IDs NOT_RUN, no name-only mapping. Additive OWN-04/05/08 portions observed; remaining OWN and D2 ORDER/HOLD/ALLOCATION/PARTIAL/RECOVERY/HTTP NOT_RUN. Unrun connected locale/grid/geometry/order/returns/fulfillment also NOT_RUN.
- No unsafe broad cleanup; no old runner execution. Exact isolated cleanup SQL retained.
