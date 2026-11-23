# RESULT — TB-SELLER-OFFER-IMMUTABLE-CREATE-INTENT-287

Status: **BLOCKED_FOR_ARCHITECT**
Readiness: **NOT_READY_FOR_ARCHITECT_ACCEPT** (final consolidated run not certifiable; see blocker)
Protocol: TOOBA-OPENCODE-DELIVERY-V1@1.3.0
Worker outcome: never ARCHITECT_ACCEPT.

## Implementation — COMPLETE
Durable immutable creation-intent fingerprint implemented inside the Offer owning module:
- `CreateIntentFingerprint` (new) canonicalizes the normalized create command (seller, variant, channel, SKU,
  effective status/policy/window/min/max, requestId) → SHA-256 hex (`ci1`).
- `SellerOffer.CreationIntentFingerprint` (nullable) + `SetCreationIntentFingerprint`; additive migration
  `20261123120000_AddOfferCreationIntentFingerprint` (`offer.offers.creation_intent_fingerprint varchar(64)`),
  EF snapshot + configuration updated. **No backfill; historical rows stay NULL.**
- `CreateOfferHandler`: replay compares the stored fingerprint (NULL ⇒ fail closed 409 `offer.create.intent_conflict`);
  the `PersistNewAsync` race branch compares the winner's fingerprint. The old mutable-field comparison is gone.
- `dotnet build src/backend/Host/Tooba.Host` → **0 errors**.
- Diff is scoped to `src/backend/Modules/Offer/**` only (no FE, Host, Cursor, root AGENTS, Pricing/Inventory).

## Connected test status (real observations; provenance per test in REQUIREMENTS.json)
T01–T12, T14 PASS across real runs r2/r4 against a real Tooba.Host + disposable PostgreSQL.
T13 PASS in r2 (real Host restart proved DB-backed durability). Redesigned fresh-context+SQL T13 = NOT_RUN in final.
T15 PASS — VERIFY.mjs rejected all three mutations (delete T05, flip T02, tamper hash).
T16 PASS — diff scoped to Offer only; disposable DB `tooba_offer287_idem` dropped (pg count 0); test Host 5094
stopped; canonical 5088 `/health`=200 and 3000 `/fa`=200.

## Environment blocker (why no single final consolidated run)
After the fixture was seeded, the Development catalog reset (`/v1/admin/catalog/demo/reset-and-seed`) left the
Promotion-owned `MerchandisingCampaignDevelopmentSeed` OOS fixture inconsistent. Every subsequent **normal**
SingleStore Host boot now crashes during `DevelopmentSchemaMigrator.MigrateOrderedAsync` with:
- `Tooba.BuildingBlocks.ContractOperationException: inventory.catalog_variant.missing`
  at `InventoryDirectory.OpenPositionAsync` ← `DrainDevelopmentStockAsync` ← `MerchandisingCampaignDevelopmentSeed.EnsureOosOfferAsync`.
Evidence: `evidence/fixture-crash-normal-boot*.err.log`.
The documented `TOOBA_SKIP_DEVELOPMENT_BOOTSTRAP=1` safe-refresh boots and recovers authorization, but recovers
**admin only** (`partyMembers=0`; `evidence/skip-host-auth-recovery.log`), so seller-auth cases (T05, T09 seller
half) cannot run on it. Reproducing a fresh, consistent disposable DB requires a full re-provision, which the
architect's final directive forbids. Therefore a single, clean, consolidated teardown matrix run is not
producible in the existing environment. No fake PASS is claimed; the r4 evidence file predates the T13/T16
harness fixes and is reported as-is.

## Timing / cost
Focused matrix harness elapsed: r2 ≈ 19s, r4 ≈ 139s (evidence fields). Whole task wall-clock was dominated by
environment provisioning/fixture recovery and is **NOT_AVAILABLE** as a precise instrumented figure.
Token usage / monetary cost: **NOT_AVAILABLE**.

## Cleanup
Task-owned disposable DB removed; test Host 5094 closed. Canonical 5088 / dev DB / shared data untouched.
285/286 evidence untouched.

`STOP_FOR_ARCHITECT` — automaticNextImplementationTask: NONE. Stage 3 NOT issued.
