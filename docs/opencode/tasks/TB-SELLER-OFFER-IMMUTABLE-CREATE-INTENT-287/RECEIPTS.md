# RECEIPTS — TB-287

## Code
- Base product code (origin/main at issuance): `d3c2e5ef1016fd2dbde6db0e1ab33d95ff41a121`; product code in this
  worktree is parity-identical to origin/main (git diff of `src` vs origin/main empty apart from this task's Offer edits).
- Changed (Offer module only):
  - `src/backend/Modules/Offer/Tooba.Offer.Application/Offers/Commands/CreateOffer/CreateOfferCommand.cs`
  - `src/backend/Modules/Offer/Tooba.Offer.Application/Offers/Commands/CreateOffer/CreationIntentFingerprint.cs` (new)
  - `src/backend/Modules/Offer/Tooba.Offer.Domain/Aggregates/SellerOffer.cs`
  - `src/backend/Modules/Offer/Tooba.Offer.Infrastructure/Persistence/Configurations/SellerOfferConfiguration.cs`
  - `src/backend/Modules/Offer/Tooba.Offer.Infrastructure/Persistence/Migrations/20261123120000_AddOfferCreationIntentFingerprint.cs` (new)
  - `src/backend/Modules/Offer/Tooba.Offer.Infrastructure/Persistence/Migrations/OfferDbContextModelSnapshot.cs`
- Build: `dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj -c Debug --no-restore` → 0 errors.

## Environment
- Disposable PostgreSQL: `tooba_offer287_idem` in container `postgres-db` (PostgreSQL 17), 127.0.0.1:5432.
- Test Host: worktree build `bin/Debug/net8.0/Tooba.Host.dll`, SingleStore Development, `http://127.0.0.1:5094`.
- Recovered legitimate auth fixture (disposable DB only): Seller A / Seller B organizations + actor identities +
  `seller-owner` role assignments (product.create/edit/view/publish) + platform admin — seeded via the module-owned
  Marketplace Development seeds (`MarketplaceSellableSeed`, `MarketplaceSellerAccess`), then SingleStore boot.
- Canonical `5088` Host and `3000` frontend were never used for writes; both remained 200 throughout.

## Harness & checker
- Producer: `evidence/connected-287.cjs` (writes `evidence/connected-287.json` directly; records its own SHA-256).
- Checker: `VERIFY.mjs` → `evidence/verify-287.json` (recomputes producer hash; mutation self-test).
- Crash evidence: `evidence/fixture-crash-normal-boot.err.log`, `evidence/fixture-crash-normal-boot-2.err.log`,
  `evidence/skip-host-auth-recovery.log`.

## Commands / observed
- Producer run r2 (normal SingleStore 5094): T01–T16 PASS except T15 delegated; elapsed ≈ 19054 ms.
- Producer run r4 (teardown attempt): T01–T12, T14 PASS; T13/T16 hit harness teardown-probe/restart defects (since fixed); elapsed ≈ 139029 ms; JSON retained as-is.
- `node VERIFY.mjs` → `realOk=false` (stale r4 evidence + hash), `t15Pass=true` (all mutations rejected),
  `decision=BLOCKED_EVIDENCE`; corroboration `databaseStillExists=false`, `canonicalHost5088=200`, `canonicalFrontend3000=200`.

## Blocker
Normal SingleStore Host boot crashes on Promotion `MerchandisingCampaignDevelopmentSeed` →
`inventory.catalog_variant.missing` after the catalog demo reset made the OOS fixture inconsistent.
Skip-bootstrap recovers admin only (partyMembers=0). Full re-provision is forbidden by the final directive,
so a single consolidated matrix run is not producible. See RESULT.md.

## Safety
No root AGENTS, .cursor, AMSC, Host, FE, Pricing/Inventory code, or live/canonical DB change. Disposable DB dropped;
test Host 5094 closed; canonical 5088/3000 healthy. 285/286 evidence untouched.
`STOP_FOR_ARCHITECT`.
