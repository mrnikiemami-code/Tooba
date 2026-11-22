# RESULT — TB-SELLER-OFFER-STAGE2-DUAL-ROLE-CREATION-285

Status: **PASS**
Readiness: **READY_FOR_ARCHITECT_REVIEW**
Protocol: TOOBA-OPENCODE-DELIVERY-V1@1.0.0
Code SHA: `507d7933a54fe067c240beb6aeb62b63f4b110c0`
Plan commit: `0109f550e22835570410db79fd67957fa465a9ae`
Source evidence commit: `507d7933a54fe067c240beb6aeb62b63f4b110c0`
Next build: `xkvDCJaFrJJymAOIruba0`
Host DLL: `143AF9C5DC7459A8E5833B9A35F1ABAECBEEF2689B704492C9F88461CB3CB007`
PostgreSQL: isolated `tooba_offer285_repair`, PostgreSQL 17.10.

## Decision

The architect-identified requestId/creation gap is repaired in this one allowed post-matrix round. A reused requestId with changed SKU/status/return-policy/limit intent now returns `offer.create.intent_conflict` (409) with zero second Offer and zero price/inventory writes. Simultaneous identical requests return the same OfferId with no duplicate and no accidental 500. All 8 REQs and the original 23 connected assertions were re-run on real Playwright→Host→isolated PostgreSQL; four new negative/concurrency assertions bring the total to 27. No Cursor artifact, AGENTS.md, roadmap, schema migration or canonical business data changed.

Worker PASS is not Architect ACCEPT or owner acceptance.

## Repair

- Durable intent comparison uses the first persisted Offer's own fields: seller, Catalog variant, channel, normalized SKU, effective status, return policy/custom window, and min/max order quantity.
- Same requestId + same intent returns the original OfferId.
- Same requestId + different intent fails closed with `offer.create.intent_conflict`; no second Offer and no price/inventory write.
- A primary-key race on simultaneous identical creates detaches the losing entry, re-reads the winner and re-applies the same intent check; it does not return a blind success.
- Seller and admin create entries both enforce the same contract.
- No migration was needed or added; the durable fingerprint is derived from existing Offer-owned state.

## Requirement matrix

| REQ | Result | Evidence |
|---|---|---|
| REQ-01 | PASS. Admin created Draft `a4a743b1-1068-10ba-8355-4ceb9100f2e5` for seller A with exactly one create request; A read the same id/status; B got 404; admin price edit returned 200 and seller read 1,500,000. | `connected-285.json` REQ01 |
| REQ-02 | PASS. View-only admin: create 403, price 403, list 200; seller B read 404, price 403; admin edit price 200; view-only create persisted 0 rows. | `connected-285.json` REQ02 |
| REQ-03 | PASS. Lost response after a real write returned original `41c02a7e-f4db-4239-ccf5-bc0028c0d3ef`; one row; price 1,300,000 and stock 2. Same request returns original `7ef06ad6-472b-4766-b484-9960a56c5371`. Changed SKU with that requestId returns 409 `offer.create.intent_conflict`, original row 1, second row 0. Admin same/different behaves identically (`6600ae07-e469-5321-61c9-c353e23e5cd4`). Two simultaneous identical creates both returned `fc78d1b2-5576-4c21-ff5e-bd44a64836f2`, one row, no error. | `connected-285.json` REQ03, `request-id-285.json` |
| REQ-04 | PASS. Server search returned `TB285-OLD-VAR` from a product UpdatedAt 2020, beyond the newest 100; selection remained the same across search/page while the page total changed 1→205. | `connected-285.json` REQ04 |
| REQ-05 | PASS. Seller and admin grids both requested page 2 with pageSize 10 and then searched `TB285-SKU-137`, returning 1 row; totals are server-derived. | `connected-285.json` REQ05 |
| REQ-06 | PASS. Cancel before send had zero business writes; the wrong-seller response showed no success and persisted nothing; a forced price failure showed the explicit partial card and the same Draft `30030036-21f1-9cbe-8837-a766322f1b3c` resumed to price 1,600,000 and stock 1. | `connected-285.json` REQ06 |
| REQ-07 | PASS. 1440 and 360, fa and en: scrollWidth equals innerWidth, dir is rtl/ltr as requested, admin and seller primary buttons are 44px. Impeccable critique and after log are present. | `connected-285.json` REQ07, `IMPECCABLE-CRITIQUE.md`, `IMPECCABLE-AFTER.log` |
| REQ-08 | PASS. Source diff after the code SHA is empty; Cursor paths are unchanged; no migration is added; strict OpenSpec and verifier negative self-tests pass; disposable cleanup is recorded. | `VERIFY-OPEN-CODE.mjs`, `OPENSPEC-STRICT.log`, `PREFLIGHT.md` |

## Verification

- `node .../VERIFY-OPEN-CODE.mjs`: exit 0, `ok=true`, 8 requirements, 27 assertions, `selfTest=PASS`.
- OpenSpec 2.1.1 strict: valid, `EXIT=0`.
- `impeccable detect` after the final UI build: completed; the same 12 pre-existing shared-shell findings and two advisory contrast notes remain. No form placeholder/control-size finding was introduced. Recorded in `IMPECCABLE-CRITIQUE.md`.
- `npx tsc --noEmit`: exit 0.
- `next build` (final): `BUILD_ID xkvDCJaFrJJymAOIruba0`.
- `dotnet build src/backend/Tooba.slnx -c Release`: 0 errors.
- `npm run test:seller`: 12/12.
- No migration, DDL, seed, grant, reset or live canonical write. Disposable DB removed; Hosts 5091/5092 and Next 3101 stopped; canonical 5088 `/health` 200 and 3000 `/fa` 200.

## Implemented

- Admin `POST /v1/admin/offers`, `GET /v1/admin/offers/{id}`, `PATCH`, price, inventory, and server query; explicit SellerPartyId input; product.view/create/edit capabilities.
- Seller create/edit capability checks use the existing seller scope plus `product.create`/`product.edit`; no seller header impersonation.
- Draft-first create with a stable Offer id derived from seller+variant+requestId; same intent returns the original Offer, changed intent fails closed.
- Concurrent identical create resolves through the primary-key race to the persisted winner and re-checks the full intent.
- Seller/admin Catalog picker server search/page with localized product name or Catalog code; admin picker route.
- Seller/admin Offer list server search/sort/page using bounded SQL count+skip+take; no full-array browser filtering.
- Seller create page uses a durable per-intent request key and an explicit partial/resume card; admin create/list/detail live in the existing AdminShell.
- Stage-1 price/inventory section safety and named StockItemId behavior retained.

## Evidence preservation

The original reviewed 23-assertion evidence and log at `13e74a303` remain available verbatim as `evidence/connected-285-original.json` and `evidence/connected-285-original.log`. The current `connected-285.*` files are the post-repair rerun; the original pass values were not reinterpreted as failures.

## Known limits / nonblocking backlog

- The shared AdminShell's 10px nav/label typography and two low-contrast selected-row advisories predate this task and are reported in `IMPECCABLE-CRITIQUE.md`.
- Error branches use controlled response substitution for wrong/lost/price-failure behavior; the first create/write in those branches is real and the success path is real.
- No screenshots; no live provider, payment, order, cart, settlement or Stage-3 shipping work.
- The seller test runner emits its pre-existing module-type warning; 12/12 tests pass.
- This is Stage 2 only and is not an owner visual acceptance or a full Marketplace certification.

## Cleanup and next action

Disposable DB removed, test ports closed, canonical services healthy. `STOP_FOR_ARCHITECT`; no automatic next task and no Stage 3 implementation.
