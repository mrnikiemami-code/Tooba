# TB-296 P1-C Group F — ownership, deferred D2 and F-BENTRY execution

Status: 17 PASS / 0 FAIL / 1 NOT_RUN (OWN-10 precise edition dependency). Prior 7 PASS preserved.
No product, seed, guard, contract, test or matrix file was modified.

## Identity
- Source HEAD: `2ba30567e9392cb7035ec40b252dc46c5d5d8652` (no product edit in this work).
- Module: `docs/opencode/tasks/TB-296-P1-C-INTEGRATION/p1c-group-f-cases.cjs`
  SHA-256 `a1ce13fe14b377c5b520230eeb72ef0c9881e75576e9eee20a7b8edf35edac4c`.
- Owner-contract harness source: `docs/opencode/tasks/TB-296-P1-C-INTEGRATION/p1c-group-f-harness/Program.cs`
  SHA-256 `69306683f5f7d1cff611cdf744aa71ef6cff1b47bfe1f39bc29cb2947f7c0a00`.
- Executed harness DLL SHA-256 `c9bee91d34e04d40bf3676242a49ab7735f7a3c106558e2594e39057b43a3080`.
- Host: `http://127.0.0.1:5088`; Next: `http://127.0.0.1:3000`.
- DB identity: `tooba_alpha|local|local`; external backup SHA-256
  `D5E2DF7B6F7AF59005EE966531D74A09187E08C9A1153EF2E752014BA477ADE5`.

## Results (new evidence, `p1c-group-f-final-evidence3/`)
| ID | Status | Evidence SHA-256 |
|---|---|---|
| OWN-01 | PASS | `7e1c80f86bfbe915faf24d3bcf8f241956667c3caf33d5ae5e4c83aebf412938` |
| OWN-02 | PASS | `3e0b2657b79bc35e1db6f8fe18bd72707f1f694f0b3355dd7fffea39fd00bbd8` |
| OWN-03 | PASS | `d3c7ddb734d367048b771f0212058caf607ae0895eb0012fe60898f0dee39e2e` |
| OWN-04 | PASS | `ef5dad77a2aaad5efc52f0eac609edddf26830cee0c8e3b5dfcd9a5779e05117` |
| OWN-05 | PASS | `1447f5924b105aacf3fc3f8961da8ed1d8feec73903988e56f930aa1bbe6a0dc` |
| OWN-06 | PASS | `e5b105fdf924ca51685e213e821f0ef99b8e2d5213817541f2be39f9579cefd2` |
| OWN-07 | PASS | `cdab3f11ee1f4da2eb6278b5b21b4c519c01f00c429fdfec4cf8ff0e87f8d094` |
| OWN-08 | PASS | `f005e8528ab05c8f69e0295855e5a2696dc763296488d21415b023f0799de104` |
| OWN-09 | PASS | `dcb412565d4ada605bc6beca900d1926093805d116581c09723c64ce9d787dad` |
| OWN-10 | NOT_RUN | `11050437ecf6b4bb3d009f54e90dcbaeb28f7dbdbd60b8a245e46a3d3c6e8471` |
| D2-ORDER | PASS | `1ff173bebc745bc6c7a343474e42caeebc7e041ad0cca66ac75a85c0186ecdb3` |
| D2-HOLD | PASS | `c05d50d654fbd5e0456a27752a7a5d7736d91e79ef40f16f64f84736180827fb` |
| D2-ALLOCATION | PASS | `6daec2cbeec2089e14aa631c295bb3eac50cbb51de6a5d4e5886983bf1375deb` |
| D2-PARTIAL | PASS | `e9171d947c6ed674d34256994357cd5ac371fcdd0dba26c36ac5db652f68fb62` |
| D2-RECOVERY | PASS | `f2471f93fb5dc7970619bb7590f4c14aed9c7e86186ae7b07bdc975f80d12bea` |
| D2-HTTP | PASS | `31cc051005c7ffe1c7599611ac5ba1e52cd20e94f5dbbba6aae4256f0edaae1b` |
| F-BENTRY-01 | PASS | `6ebe9a7b74a2032cc30028e0a89927bc39d8febd7b6cfc6d94b13efd780210e7` |
| F-BENTRY-02 | PASS | `ea4de4b690ceaf18f0cb89b86912216a667e8a6e7488f3764ee821c47c4bd9ff` |

Machine-readable exact hashes: `p1c-group-f-summary.json`.
Merged 60-ID ledger preserving the seven prior PASS: `p1c-executed-ledger4.json`
SHA-256 `d4083adc694dde1df90fcd1870106c75ff58c39f0c4ca28d10f9145f319d9efb`
(PASS 24, FAIL 0, NOT_RUN 36).

## What was actually exercised
- OWN-01/02/03: real admin/seller HTTP ownership and capability checks plus the actual
  `InventoryDirectory` seller-physical, provisioning and eligibility contracts over PostgreSQL.
  Seller B role/ceiling was granted only through the owner access-control APIs. The evidence records
  the exact before/after snapshot: ceiling 0 rows, legacy roles 51 -> 51, assignments 0 -> 0,
  created role and assignment removed, ceiling snapshot matched, seller B capabilities empty after.
- OWN-04..09: real `InventoryDirectory` allocation/reservation/release/consume/replacement/retry
  and pre/post-commit fault injection over PostgreSQL, including request-correlated concurrency and
  exact before/after independent SQL snapshots.
- OWN-10: DB registry (`fa-IR` default RTL Jalali, `en-US`/`ru-RU` LTR Gregorian, distinct prefixes),
  real Chromium at 360/768/1440 on the admin offer workspace (no overflow, seven tabs), and the
  configured third-locale (`ru`) shell. NOT_RUN for the Marketplace-only
  "platform physical read-only / allocation editable" clause and the in-app locale switch with draft
  preservation: the authorized Host runs SingleStore and exposes
  `canEditPhysicalInventory=true, canEditOfferedQuantity=false` for the admin platform offer.
  No edition switch, invented control or inferred PASS was used.
- D2-ORDER: isolated real `order.checkouts`/`order.seller_orders`/`order.order_lines` fixtures
  (Paid and restorable Cancelled) plus the actual Offer assignment + Order commitment bridge:
  typed `offer.fulfillment.change_protected` 409 with no offer/history mutation; free offer
  assignment/reassignment/clear persisted with exact history.
- D2-HOLD/ALLOCATION/PARTIAL/RECOVERY: real Inventory owner contracts with real HTTP assignment
  races, reused-hold acceptance, allocation/assignment first-writer races, partial release/consume,
  paid supply, damaged replacement, unpaid retry and consume replay.
- D2-HTTP: real admin/seller routes, owner revisions, operation replay before/after subsequent
  mutation, changed-intent denial and exact owner-fact target/location.
- F-BENTRY-01: real availability projection excluded ineligible Store/foreign/other-seller rows
  and their labels/physical quantities; unassigned Marketplace offer returned no rows.
- F-BENTRY-02: actual checkout reservation adapter rejected wrong-offer reuse and duplicate reuse
  with `inventory.reservation.conflict`, released nothing and added no reservation; a single valid
  reuse returned the exact hold.

## Cleanup and restoration
Independent SQL after the run: `P1C-F-*` locations 0; P1C offers 0; P1C order fixtures 0;
reservations/allocations on P1C offers 0; seller B ceiling rows 0; seller B `p1c-f-*` roles 0;
seller B assignments 0. The 51 legacy seller B roles predate this run and were preserved exactly.

## Evidence
- New evidence directory: `p1c-group-f-final-evidence3/`
- Raw group ledger: `p1c-group-f-final-ledger-raw3.json`
- Merged ledger: `p1c-executed-ledger4.json`
- Summary: `p1c-group-f-summary.json`
- Run log: `p1c-group-f-final-run3.log`
