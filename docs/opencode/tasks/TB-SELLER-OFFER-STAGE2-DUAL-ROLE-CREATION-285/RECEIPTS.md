# RECEIPTS — TB-285

## Identity

- Repository: `mrnikiemami-code/Tooba`, `main`.
- Expected base: `26f9a7a4d24d009f25b66b4ea589df884c2a1aa1` (ancestor).
- Execution base: `2d3230e2fbfb50b0931f4e716e300628747ce112`.
- Plan commit: `0109f550e22835570410db79fd67957fa465a9ae`.
- Final source commit: `507d7933a54fe067c240beb6aeb62b63f4b110c0`.
- Served Next: `xkvDCJaFrJJymAOIruba0`, 3101.
- Host: 5091 / 5092, DLL SHA256 `143AF9C5DC7459A8E5833B9A35F1ABAECBEEF2689B704492C9F88461CB3CB007`.
- DB: `tooba_offer285_repair`, PostgreSQL 17.10, dropped after proof.

## Commands and results

- `node docs/opencode/tasks/.../evidence/connected-285.cjs`: exit 0; 23 checks, all PASS; scenarios REQ01–REQ08.
- `node docs/opencode/tasks/.../evidence/request-id-285.cjs`: exit 0; same request/same id; changed intent 409; admin changed intent 409; concurrent identical same id/one row.
- `node docs/opencode/tasks/.../VERIFY-OPEN-CODE.mjs`: exit 0; 8 requirements, 27 assertions, `selfTest=PASS`.
- `openspec validate seller-offer-dual-role-creation-285 --strict`: valid, exit 0.
- `impeccable detect` on the served admin/seller create pages: completed; no new form placeholder/control-size finding. The 12 remaining findings are pre-existing shared-shell nav/label 10px and two advisories; see `IMPECCABLE-CRITIQUE.md`.
- `npx tsc --noEmit`: 0.
- `next build`: success `xkvDCJaFrJJymAOIruba0`.
- `dotnet build src/backend/Tooba.slnx -c Release`: 0 errors.
- `npm run test:seller`: 12/12 pass.

## Evidence facts

- REQ-01: admin create `a4a743b1-1068-10ba-8355-4ceb9100f2e5`, seller A GET 200, B 404, admin price 200, seller price 1,500,000.
- REQ-02: view-only create 403, view-only price 403, view-only list 200, B price 403, B read 404, view-only persisted rows 0.
- REQ-03: lost-create `41c02a7e-f4db-4239-ccf5-bc0028c0d3ef`, two POST attempts with the same requestId, one row, amount 1,300,000, stock 2.
- REQ-03 repair: seller same `7ef06ad6-472b-4766-b484-9960a56c5371`; changed SKU 409 `offer.create.intent_conflict`, originalRows 1/secondRowCount 0. Admin same `6600ae07-e469-5321-61c9-c353e23e5cd4`; changed SKU 409, originalRows 1/secondRowCount 0. Concurrent identical `fc78d1b2-5576-4c21-ff5e-bd44a64836f2`, both 201, one row, zero errors.
- REQ-04: `TB285-OLD-VAR`, old product UpdatedAt year 2020, selection retained across server search/page.
- REQ-05: seller/admin page 2 requests pageSize 10; `TB285-SKU-137` search returns 1 row.
- REQ-06: cancel business writes 0; wrong response no notice/no row; partial Draft `30030036-21f1-9cbe-8837-a766322f1b3c` amount 1,600,000 stock 1.
- REQ-07: 1440/360 fa/en, zero horizontal overflow, primary controls 44px.
- REQ-08: 212 offers, seller A 108, seller B 101, old product 2020, source diff after code SHA empty, Cursor diff empty, no migration.

## Hashes

- `evidence/connected-285.json`: see `EVIDENCE-MANIFEST.json`.
- `evidence/request-id-285.json`: see `EVIDENCE-MANIFEST.json`.
- `evidence/connected-285.cjs` / `request-id-285.cjs`: see manifest.
- `IMPECCABLE-AFTER.log`: see manifest.
- `IMPECCABLE-SOURCE.log`: see manifest.
- `OPENSPEC-STRICT.log`: see manifest.
- Original 13e74 evidence: `evidence/connected-285-original.json` and `evidence/connected-285-original.log`.
- Evidence commit: final docs/evidence commit; see BRIDGE envelope.

## Safety

No Cursor file, AGENTS.md, roadmap, migration, DDL, grant, seed, reset, live canonical business write, provider, payment, order or notification effect. The disposable database and test processes were removed; canonical 5088 and 3000 remain healthy.
