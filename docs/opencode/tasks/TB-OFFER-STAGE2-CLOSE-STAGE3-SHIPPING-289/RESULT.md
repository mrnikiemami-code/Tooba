# RESULT — TB-OFFER-STAGE2-CLOSE-STAGE3-SHIPPING-289

Outcome: **BLOCKED** — Stage 2 connected run executed on `tooba_alpha`; crash fixed and idempotence/roles proven, but
**C01 "Offer linked to valid Catalog variant" is not met**, and C04/C07 remain NOT_RUN. Stage 3 design plan preserved;
no Stage-3 product code. Worker never claims ARCHITECT_ACCEPT.

## Stage 2 connected run (detail: stage2-proof/STAGE2-CONNECTED-RUN.md)
Candidate 287+288 `Tooba.Host.dll` SHA `F96A79B7…` deployed on existing 5088 Host vs `tooba_alpha`; verified backup
revalidated (SHA256 4839D664…07FB9C). Executed: 287 additive migration (one nullable column) → controlled Catalog
reset → boot#1 → boot#2 → restore → original Host restart.

| Test | Status | Evidence |
|---|---|---|
| C01 288 S02 bootstrap-after-reset | **PARTIAL** | Host healthy after reset+boot, **no** `inventory.catalog_variant.missing`; but post-reset `published_products=0` and no live template → gateway fail-closes and does **not** relink the stale OOS Offer. "linked to valid variant" NOT met. |
| C02 288 S03 repeat bootstrap | **PASS** | boot#2 healthy; OOS rows=1 distinct=1; no duplicates. |
| C03 288 S05 auth across boots | **PASS** | role assignments identical (platform-admin=1, seller-owner=1, mobile-order-op=1); no escalation. |
| C04 287 T01–T14 | **NOT_RUN** | 287 harness not executed on this dev DB this session. |
| C05 287 T15 verifier | **PARTIAL** | 3/3 mutations rejected; `realOk=false`. |
| C06 T16 cleanup/identity | **PASS** | backup preserved; dev DB restored to baseline; 5088/3000 healthy. |
| C07 285 UI/Playwright | **NOT_RUN** | not executed. |
| C08 integration scope | **PASS** | only 287 additive nullable column applied then removed by verified restore; no root AGENTS/.cursor/Host/Inventory/API change. |

S01 negative control reproduced (OOS `01a0ba10…` → VARIANT_MISSING after reset; variants 1361→884).

## Exact blocker (C01)
Post-reset catalog on `tooba_alpha` has `published_products=0` and no live template variant, so the 288 gateway's
`ResolveLiveVariantIdAsync` finds no target and returns null (no crash, but the stale OOS row is not relinked/removed).
Bounded repair for architect decision: (a) remove/archive the stale marked dev-seed row when no live target; and/or
(b) permit any existing live Catalog variant as rebuild target; and investigate the 0-published-products reset state.

## Environment
Restored to healthy baseline: `tooba_alpha` offers=3223, variants=1361, products=567, parties=10, 287 col removed;
original 5088 Host `/health`=200 (SingleStore/dev-singlestore/store-alpha/tenant-alpha); frontend 3000 `/fa`=200.

## Stage 3 (C09–C11) — PLAN PRESERVED
`stage3-design/STAGE3-ARCHITECTURE.md`, `STAGE3-DECISION-GATES.md` (DG-01..07), `STAGE3-ACCEPTANCE-MATRIX.json`
(D01–D13; D07/D09/D13 BLOCKED_ON_DECISION); OpenSpec `offer-stage3-shipping-assignment-289` strict valid. No Stage-3
product code written (gated on Stage 2 accept).

## Governance (C12)
No fabricated PASS; different runs not merged; no Stage-3 code; no root AGENTS/.cursor/Host/Inventory/API/287-fingerprint
change. Verified backup retained. Cost/time NOT_AVAILABLE. `STOP_FOR_ARCHITECT`.
