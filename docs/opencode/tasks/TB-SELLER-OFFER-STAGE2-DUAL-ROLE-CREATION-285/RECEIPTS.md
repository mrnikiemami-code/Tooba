# RECEIPTS — TB-285

## Identity

- Repository: `mrnikiemami-code/Tooba`, `main`.
- Expected base: `26f9a7a4d24d009f25b66b4ea589df884c2a1aa1` (ancestor).
- Execution base: `2d3230e2fbfb50b0931f4e716e300628747ce112`.
- Plan commit: `0109f550e22835570410db79fd67957fa465a9ae`.
- Final source commit: `02abb9d4b69f2bb7ebb023c657d62f203ab95475`.
- Served Next: `UYWZYt9rgDXhubR29wG8o`, 3101.
- Host: 5091 / 5092, DLL SHA256 `E2DEE16EE9AE582E9D983321FD6ACF98818997637354E56FAF202F5466444ABE`.
- DB: `tooba_offer285_connected`, PostgreSQL 17.10, dropped after proof.

## Commands and results

- `node docs/opencode/tasks/.../evidence/connected-285.cjs`: exit 0; 23 checks, all PASS; scenarios REQ01–REQ08.
- `node docs/opencode/tasks/.../VERIFY-OPEN-CODE.mjs`: exit 0; 8 requirements, 23 assertions, `selfTest=PASS`.
- `openspec validate seller-offer-dual-role-creation-285 --strict`: valid, exit 0.
- `impeccable detect` on the served admin/seller create pages: completed; no new form placeholder/control-size finding. The 12 remaining findings are pre-existing shared-shell nav/label 10px and two advisories; see `IMPECCABLE-CRITIQUE.md`.
- `npx tsc --noEmit`: 0.
- `next build`: success `UYWZYt9rgDXhubR29wG8o`.
- `dotnet build src/backend/Tooba.slnx -c Release`: 0 errors.
- `npm run test:seller`: 12/12 pass.

## Evidence facts

- REQ-01: admin create `ba254d78-07aa-fda0-4ef0-745d41142c08`, seller A GET 200, B 404, admin price 200, seller price 1,500,000.
- REQ-02: view-only create 403, view-only price 403, view-only list 200, B price 403, B read 404, view-only persisted rows 0.
- REQ-03: `d89dbc89-13c9-3550-e26a-403e5ffb93f2`, two POST attempts with the same requestId, one row, amount 1,300,000, stock 2.
- REQ-04: `TB285-OLD-VAR`, old product UpdatedAt year 2020, selection retained across server search/page.
- REQ-05: seller/admin page 2 requests pageSize 10; `TB285-SKU-137` search returns 1 row.
- REQ-06: cancel business writes 0; wrong response no notice/no row; partial Draft `41099284-7965-c920-d319-a44abb25e0c4` amount 1,600,000 stock 1.
- REQ-07: 1440/360 fa/en, zero horizontal overflow, primary controls 44px.
- REQ-08: 220 offers, seller A 116, seller B 101, old product 2020, source diff after code SHA empty, Cursor diff empty, no migration.

## Hashes

- `evidence/connected-285.json`: see `EVIDENCE-MANIFEST.json`.
- `evidence/connected-285.cjs`: see `EVIDENCE-MANIFEST.json`.
- `IMPECCABLE-AFTER.log`: see manifest.
- `IMPECCABLE-SOURCE.log`: see manifest.
- `OPENSPEC-STRICT.log`: see manifest.
- Evidence commit: final docs/evidence commit; see BRIDGE envelope.

## Safety

No Cursor file, AGENTS.md, roadmap, migration, DDL, grant, seed, reset, live canonical business write, provider, payment, order or notification effect. The disposable database and test processes were removed; canonical 5088 and 3000 remain healthy.
