# STAGE 2 CONNECTED RUN — TB-289 (executed on tooba_alpha, authorized)

Date: 2026-11-23. Candidate: combined **287 + 288** build `Tooba.Host.dll` SHA-256
`F96A79B78B2C7EAB4046EB093F9359CB0979FCC4DC99BBD5A665CC48717CFCB7`, deployed on the existing dev Host
(port 5088) against dev DB `tooba_alpha`. Verified backup pre-checked (SHA256 `4839D6649C4E…07FB9C`).

## Executed sequence
1. Applied the 287 additive migration to `tooba_alpha` (dev migrator): one nullable column
   `offer.offers.creation_intent_fingerprint varchar(64)`; pre-state col=0 → post col=1; offers stayed 3223.
2. Auth/roles baseline before reset: admin `01a036c2-970e…`; Seller A `01a030d1-40cb…` actor `01a03628-3f68…`;
   Seller B `01a030d1-40db…`; assignments `platform-admin=1, seller-owner=1, mobile-order-op=1`.
3. Controlled Catalog reset-and-seed (`POST /v1/admin/catalog/demo/reset-and-seed`, admin actor).
   Before: variants=1361, products=567, OOS offer `01a0ba10…` VARIANT_OK (variant `024ed01b…`).
   After: variants=884, products=308, OOS offer `01a0ba10…` **VARIANT_MISSING** (variant deleted) — S01 reproduced.
4. Boot #1 (restart candidate, normal Development): Host `/health`=200, **no** `inventory.catalog_variant.missing`,
   no unhandled exception (S02 stability). `published_products=0` after reset; OOS row remained `VARIANT_MISSING`
   (gateway fail-closed → returned null; did NOT invent a variant; DID NOT relink).
5. Boot #2 (restart candidate): `/health`=200, no crash, OOS rows=1 distinct=1, role assignments unchanged
   (`platform-admin=1, seller-owner=1, mobile-order-op=1`) — S03 idempotence + S05 roles stable.
6. Restored `tooba_alpha` from the verified dump (`pg_restore --clean --if-exists` exit 0) → offers=3223,
   variants=1361, products=567, parties=10; 287 column removed (col=0) — baseline restored.
7. Restarted the original dev Host via `scripts/dev/host-stack.ps1` → 5088 `/health`=200
   (provenance SingleStore/dev-singlestore/store-alpha/tenant-alpha), 3000 `/fa`=200.

## Results (truthful)
- C01 (S02 bootstrap after reset): **PARTIAL** — Host healthy and no `inventory.catalog_variant.missing` (crash
  fixed), but the OOS Offer is **not linked to a valid Catalog variant**: post-reset `published_products=0` and no
  live template exists, so the 288 gateway returns null instead of rebuilding. "Offer linked to valid variant" unmet.
- C02 (S03 repeat bootstrap): **PASS** — stable one OOS row, no duplicates, healthy.
- C03 (S05 auth across boots): **PASS** — role assignments identical before/after; no escalation.
- C04 (287 T01–T14): **NOT_RUN** this session (287 harness not executed on this dev DB).
- C05 (287 T15 verifier): **PARTIAL** — 3/3 mutations rejected, `realOk=false` (r4 evidence had T13/T16 fail).
- C06 (T16 cleanup/identity): **PASS** — backup preserved; dev DB restored to baseline; 5088/3000 healthy.
- C07 (285 UI/Playwright): **NOT_RUN** (not executed).
- C08 integration scope: **PASS** — only the 287 additive nullable column was applied to dev and then removed by the
  verified restore; no root AGENTS/.cursor/Host/Inventory/API change; 287/288 code/evidence preserved.

## Exact finding (blocker for C01)
After the Catalog demo reset on `tooba_alpha`, the re-seeded catalog has **0 published products** and no live
template variant, so the 288 gateway's `ResolveLiveVariantIdAsync` finds no valid target and fail-closes (returns
null). This prevents the crash (good) but leaves the marked dev-seed OOS Offer dangling (not relinked/removed).
Bounded repair candidates (architect decision): (a) always remove/archive the stale marked dev-seed row when no live
target; and/or (b) allow any existing live Catalog variant (not only published) as rebuild target; and investigate
why `reset-and-seed` yields 0 published products on this dev DB.
