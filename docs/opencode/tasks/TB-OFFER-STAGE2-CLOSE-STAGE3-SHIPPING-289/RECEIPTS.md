# RECEIPTS — TB-OFFER-STAGE2-CLOSE-STAGE3-SHIPPING-289

## Ancestry / preserved work
- origin/main at issuance read: f922eaa0923f01b013fe0f2fad667ff364d6fc4a.
- 287 commit ae7d7dbba265a8b58dd32c169f7af0f42f15ef4c; 288 commit bb6d94caabdcbcd55cdbbe4a245ca61e7c6224a9
  (worker branch `opencode/iso-agents-002`). 287/288 code and evidence preserved; not restarted.

## Safe Stage-2 evidence captured this task (no destructive run)
- C05: `stage2-proof/C05-verify-287.txt` — 287 VERIFY: mutations all REJECTED; `realOk=false`, decision BLOCKED_EVIDENCE.
- C06: `stage2-proof/C06-observation.txt` — 5088 `/health`=200, 3000 `/fa`=200; backup file + SHA256 preserved.
- C08: source scope reviewed (287 = Offer + additive nullable migration; 288 = Offer seed gateway + test; no root
  AGENTS/.cursor/Host touch).
- Development backup (owner-authorized, preserved, not in git):
  `...\dev-db-backups\tooba_alpha_288backup_20261123_133446.dump`, 3,091,575 B,
  SHA256 4839D6649C4E4066A404FA73EA95BFF60AE67343EEA4A74BF81F2C582B07FB9C, pg_dump exit 0, pg_restore --list exit 0.

## Stage-3 design artifacts (design only)
- `stage3-design/STAGE3-ARCHITECTURE.md`, `STAGE3-DECISION-GATES.md`, `STAGE3-ACCEPTANCE-MATRIX.json`.
- OpenSpec `openspec/changes/offer-stage3-shipping-assignment-289/**`:
  `openspec validate offer-stage3-shipping-assignment-289 --strict` → `Change ... is valid`.

## Boundaries honored
No Stage-3 product code; no Stage-2 acceptance claimed; no root AGENTS/.cursor/Host/Inventory/API/Pricing change; no new
DB/Host; no provisioning loop; no 287 migration deployed to dev DB. Canonical 5088/3000 healthy.

## Cost / time
NOT_AVAILABLE. `STOP_FOR_ARCHITECT`.

## Stage-2 connected run (2026-11-23, authorized on tooba_alpha)
- Candidate 287+288 Host dll SHA-256 F96A79B78B2C7EAB4046EB093F9359CB0979FCC4DC99BBD5A665CC48717CFCB7 deployed on 5088 vs tooba_alpha.
- 287 migration applied to dev: offer.offers.creation_intent_fingerprint varchar(64) nullable (pre col=0 -> post col=1); reverted by restore (col=0).
- Controlled catalog reset-and-seed: variants 1361->884, products 567->308; OOS offer 01a0ba10-5a31-7000-b733-4cd74ef3763a became VARIANT_MISSING (S01 reproduced).
- boot#1 healthy, no inventory.catalog_variant.missing; published_products=0 -> OOS not relinked (C01 PARTIAL).
- boot#2 healthy; OOS rows=1 distinct=1; role assignments unchanged (C02/C03 PASS).
- Restore: docker cp dump + pg_restore --clean --if-exists exit 0 -> offers=3223, variants=1361, products=567, parties=10.
- Original 5088 restarted via scripts/dev/host-stack.ps1; /health=200 (tenant-alpha); 3000 /fa=200.
- Backup retained: .../dev-db-backups/tooba_alpha_288backup_20261123_133446.dump SHA256 4839D6649C4E4066A404FA73EA95BFF60AE67343EEA4A74BF81F2C582B07FB9C (not in git).

## Stage-2 final correction run (2026-11-24)
- Candidate 287+288+correction dll SHA-256 AD8FF896E98F51D2FBCC063CDD71A4AB3BCD6FF78D1F6580082A607683F12EE8; Host build 0 errors.
- Offer fix: EnsureActiveCloneFromAnyActiveAsync archives the stale marked OOS when no eligible live target; focused tests 5/5 pass.
- OpenSpec dev-seed-stale-catalog-variant-288 updated (REQ-SEED-05) strict valid.
- Connected: reset -> OOS 01a0ba10 Active+VARIANT_MISSING (S01); boot1 -> OOS Archived, activeDangling=0, no crash, no inventory cmd; roles unchanged.
- Finding: 5 promotion.merchandising_campaign_offers rows (shared non-seed campaigns) still reference OOS -> cross-ownership BLOCKED.
- Restore: pg_restore --clean exit 0 -> offers=3223, variants=1361, products=567; original 5088 restarted 200; 3000 200.
- Backup retained .../tooba_alpha_288backup_20261123_133446.dump SHA256 4839D6649C4E4066A404FA73EA95BFF60AE67343EEA4A74BF81F2C582B07FB9C (not in git).

## Promotion guard (2026-11-24)
- CampaignCartPriceAuthority.cs: injected IOfferLookupGateway; fail-closed on offer==null||Status!=Active before price (lines ~79-83).
- Candidate Host dll SHA-256 8E6196268F25DBC1EE040D07494CBD1FAC5472D88C653F057415D0D17D8831D4; build 0 errors (promotion-guard-host-build-summary.txt).

## Cart-price integration (2026-11-24)
- dotnet test Host.Tests --filter CampaignCartPriceIntegrityTests -> Passed 2/2 (Testcontainers PG16). See stage2-proof/cartprice-integration-result.txt.
- Fixed test call-sites for new ctor params: CampaignCartPriceAuthority(+IOfferLookupGateway via OfferStore), OfferDevelopmentSeedGateway(+ICatalogVariantLookup via CatalogDirectory). No behavior change.
