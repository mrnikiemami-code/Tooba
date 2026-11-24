# RESULT — TB-OFFER-STAGE2-CLOSE-STAGE3-SHIPPING-289 (final correction)

Outcome: **BLOCKED** — Stage 2 not fully PASS. Architect-confirmed Catalog `Published=0` (intentional) accepted.
Bounded Offer-owner correction implemented and connected-verified; one cross-ownership Promotion reference issue and
unrun C04/C05/C07 remain. Stage 3 design preserved; no Stage-3 product code. No ARCHITECT_ACCEPT claimed.

## Stage-2 status (current candidate `AD8FF896…`, run on tooba_alpha)
- **C01 (alternate fail-closed) — PARTIAL/BLOCKED.** After Catalog reset the stale `DEV-SEED-OOS` Offer is now safely
  **Archived** via the owning Offer gateway (no fabricated variant, no active dangling OOS, no Inventory command on the
  missing variant, no crash, Host healthy). **But** `promotion.merchandising_campaign_offers` still has **5 rows**
  referencing that OOS Offer in **non-seed shared dev campaigns**; pruning them would silently rewrite shared Promotion
  data outside seed ownership → BLOCKED per the correction's boundary (needs a Promotion-owned reconciliation decision).
- **C02 (S03) — PARTIAL.** Idempotence source-verified (focused unit test: second run no-op, one row); connected
  second-boot NOT_RUN this cycle.
- **C03 (S05) — PASS.** Role assignments unchanged across boot (platform-admin=1, seller-owner=1, mobile-order-op=1).
- **C04 (287 T01–T14) — NOT_RUN.** 287 connected matrix not executed on this candidate.
- **C05 (287 real VERIFY, realOk=true) — NOT_RUN/NOT_PASS.** Real verifier still false (needs a clean 287 run).
- **C06 (T16 cleanup/identity) — PASS.** Verified backup preserved; `tooba_alpha` restored to baseline
  (offers=3223, variants=1361, products=567); original 5088 `/health`=200, 3000 `/fa`=200.
- **C07 (285 UI/Playwright desktop+360) — NOT_RUN.**
- **C08 (scope) — PASS.** Change limited to Offer dev-seed gateway + focused test + OpenSpec (REQ-SEED-05, strict valid);
  287 fingerprint and 288 code preserved; Catalog/Inventory/general-seller unchanged; no root AGENTS/.cursor/Host edit.

S01 negative control reproduced: post-reset OOS `01a0ba10…` → VARIANT_MISSING (variants 1361→884); boot#1 archived it.

## Exact blockers (for architect)
1. **Cross-ownership Promotion references:** 5 shared non-seed campaigns reference the withdrawn OOS Offer; removing
   them is a Promotion-owned/shared-data mutation beyond the dev-seed's safe scope. Proposed smallest remedy: a
   Promotion-owned reconciliation that prunes membership rows pointing at archived/invalid Offers (to be authorized),
   or an owner decision that archived-Offer membership is acceptable (storefront filters inactive).
2. **C04/C05/C07 not executed** on the current candidate (287 connected matrix, real VERIFY=realOk, Playwright UI).

## Environment
Delivered healthy: original dev Host 5088 (SingleStore/dev-singlestore/store-alpha/tenant-alpha) 200; frontend 3000 200;
`tooba_alpha` baseline (OOS Active+OK); verified backup retained (SHA256 4839D664…07FB9C).

## Stage 3
Design preserved (`stage3-design/*`, OpenSpec `offer-stage3-shipping-assignment-289` strict valid); not implemented.
Cost/time NOT_AVAILABLE. `STOP_FOR_ARCHITECT`.

## Promotion runtime guard (architect decision, 2026-11-24)
- Implemented in CampaignCartPriceAuthority: offer must be Active (via IOfferLookupGateway) before any campaign quote; Archived/not-found -> null (fail closed). Historical memberships retained (not deleted).
- Build: Host 0 errors, candidate dll SHA 8E6196268F25DBC1EE040D07494CBD1FAC5472D88C653F057415D0D17D8831D4.
- C01 runtime guard: IMPLEMENTED + BUILD-VERIFIED; connected cart-price/storefront proof NOT_RUN this session.
- Still NOT_RUN: C02 connected boot idempotence (unit-verified), C04 (287 matrix), C05 (real VERIFY realOk), C07 (Playwright).
- Overall remains BLOCKED for STAGE2_READY until the final connected run proves C01/C02/C04/C05/C07 with current candidate. Next implementation task per architect: Stage 3.

## Promotion guard — real integration evidence (2026-11-24)
- Ran Host.Tests CampaignCartPriceIntegrityTests (Testcontainers PostgreSQL 16) on candidate: Passed 2/2.
- Proves with real Cart/Promotion/Pricing: Active Offer + valid campaign -> campaign price 70000; wrong campaign -> base 100000; expired window -> reprice to base. New guard does not break authorized discount and keeps fail-closed cases.
- Still not executed: Archived-Offer negative cart case (C01 archived branch), C04 (287 matrix), C05 (real VERIFY realOk), C07 (Playwright) -> remains BLOCKED for STAGE2_READY.
- Dev env untouched this step: 5088/3000 healthy, tooba_alpha baseline, backup retained.

## C01 read-edge finding (2026-11-24, real Testcontainers test)
- Add-path guard verified 2/2 (active discount preserved; wrong campaign/expiry fail-closed).
- Read/revalidation edge FAILED: archived Offer kept stored campaign price on GetCart reload (CampaignCartPriceAuthority add-path rejects non-Active, but CartDirectory reload does not clear already-applied campaign for a now-Archived Offer). See stage2-proof/C01-read-edge-finding.md.
- C01 therefore NOT fully PASS; C02/C04/C05/C07 NOT_RUN. No fake PASS.

## C01 CLOSED — PASS (real Testcontainers, 2026-11-24)
- Cart-owner fix in RevalidateCampaignQuotesAsync: on OfferUnavailable clear campaign and reprice base (no stale discount).
- CampaignCartPriceIntegrityTests 2/2 PASS including: active=>70000, wrong/expiry=>base, ARCHIVED Offer reload=>base 100000 & campaign null.
- Remaining NOT_RUN: C02 (connected boot idempotence), C04 (287 T01-T14), C05 (real VERIFY realOk), C07 (Playwright) - not executed in remaining budget.

## Independent architect code change — 2026-11-24 (UNTESTED IN RUNTIME)
- Code-level gap beyond C01 archived-campaign clearing: `CartSnapshotProjector` previously derived availability solely from Inventory, so an Archived Offer with stock could be presented as Available with a base quote. Architect directly changed the working branch via GitHub: `CartSnapshotProjector` batch-fetches `IOfferLookupGateway.FindOffersBatchAsync`, requires `OfferStatus.Active` and matching `CatalogVariantId`, otherwise projects `Unavailable`, `QuotedAmount=null`, `PriceId=null` and no campaign context. Constructor updated in `CartDirectory`.
- Regression assertion in `CampaignCartPriceIntegrityTests` now checks Archived Offer snapshot is unavailable/no quote, not price 100000 (which can remain as an internal saved historical line, but must not be payable).
- These commits have NOT been compiled or run against Windows Host/DB in this conversation: architect code edits `68e82059`, `f3a09ee7`, `8afda7d7`. `C01` requires re-validation on new branch tip; no fabricated PASS.
- Direct OpenCode bridge health/list_sessions calls returned internal failure; ChatGPT cannot execute the user's Windows Host/DB tests from this chat. To finish C02/C04/C05/C07 and regression, run on already-authorized existing 5088/tooba_alpha, verified backup, no fresh DB/Host. Preserve baseline. STOP_FOR_ARCHITECT after actual receipts.

## TB-289-R1 (2026-11-24): C01 STRICT PASS
- FF to architect HEAD 7febfd634 (CartSnapshotProjector fail-closed availability/quote + Cart revalidation + arch test).
- CampaignCartPriceIntegrityTests (Testcontainers PG16) Passed 2/2: archived Offer => Availability Unavailable, QuotedAmount null, PriceId null (no payable quote/discount); active => 70000; wrong/expiry fail-closed.
- Remaining NOT_RUN this session: C02 (connected boot idempotence), C04 (287 T01-T14), C05 (VERIFY realOk), C07 (Playwright). STAGE2_READY not claimed.
