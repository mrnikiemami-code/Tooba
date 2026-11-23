# STAGE 2 CORRECTION RUN — TB-289 (architect final correction)

Candidate: combined **287+288+correction** `Tooba.Host.dll` SHA-256
`AD8FF896E98F51D2FBCC063CDD71A4AB3BCD6FF78D1F6580082A607683F12EE8` (Host build 0 errors).
Backup precondition revalidated: dump SHA256 `4839D664…07FB9C`; target DB `tooba_alpha`.

## Bounded owner fix (Offer)
`OfferDevelopmentSeedGateway.EnsureActiveCloneFromAnyActiveAsync`: when the marked dev-seed OOS Offer references a
deleted Catalog variant and there is **no eligible live target** (Catalog Demo is intentionally all-Draft → no
published variant, no live template), it now **archives (withdraws)** the stale dev-seed row via the aggregate and
returns null (no fabricated variant, no Inventory command, idempotent). Catalog, Inventory, Promotion schema, general
seller Offers untouched. OpenSpec `dev-seed-stale-catalog-variant-288` updated (REQ-SEED-05) and **strict valid**.
Focused tests: 5/5 pass (`correction-focused-test.log`).

## Connected run on tooba_alpha (candidate on 5088)
- Pre-state: offers=3223, variants=1361, OOS `01a0ba10…` Active+OK.
- Controlled Catalog reset: variants 1361→884, products 567→308; OOS `01a0ba10…` → Active + VARIANT_MISSING (S01).
- Boot #1: `/health`=200, **no** `inventory.catalog_variant.missing`, no unhandled exception.
  OOS Offer → **Archived**; active dangling OOS count = **0**; no Inventory stock command on the missing variant.
  Roles unchanged: platform-admin=1, seller-owner=1, mobile-order-op=1.
- **Remaining reference finding:** `promotion.merchandising_campaign_offers` still has **5** rows with
  `seller_offer_id = OOS offer` — located in **non-seed shared dev campaigns** (campaign ids `019a16a0-…`,
  `01a0ba54…`, `01a0ba55…`×2, `01a0ba56…`, none matching the merchandising seed campaign ids). Pruning them would
  silently rewrite shared Promotion data outside the seed's ownership → per the correction's boundary this is a
  BLOCKED point, not papered over.
- Restore: `pg_restore --clean --if-exists` exit 0 → offers=3223, variants=1361, products=567; OOS Active+OK
  (baseline consistent). Original dev Host restarted via `scripts/dev/host-stack.ps1` → 5088 `/health`=200
  (tenant-alpha), 3000 `/fa`=200.

## Result
- C01 (alternate fail-closed): **PARTIAL/BLOCKED** — Offer-side withdraw correct (no active dangling, no crash, no
  Inventory command), but 5 shared non-seed Promotion memberships still reference the withdrawn OOS Offer; pruning
  crosses Promotion ownership / shared dev data.
- C02 (S03): source-verified idempotent (unit test second-run no-op); connected second-boot NOT_RUN this cycle.
- C03 (S05): PASS (roles unchanged across boot).
- C04 (287 T01–T14): NOT_RUN. C05 (real VERIFY realOk): NOT_RUN this cycle. C06: PASS (restore + 5088/3000).
- C07 (285 Playwright): NOT_RUN. C08 (scope): PASS (Offer seed gateway + test + OpenSpec only; 287/288 preserved).
