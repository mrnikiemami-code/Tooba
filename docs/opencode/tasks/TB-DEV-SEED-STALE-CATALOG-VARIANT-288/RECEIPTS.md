# RECEIPTS — TB-DEV-SEED-STALE-CATALOG-VARIANT-288

## Base / code
- origin/main at issuance: `4042306c259ed8ca0c9c56a050191c40e9c31d70`.
- Worker branch `opencode/iso-agents-002` prior commit (287): `ae7d7dbba265a8b58dd32c169f7af0f42f15ef4c`.

## Changed (bounded)
- `src/backend/Modules/Offer/Tooba.Offer.Infrastructure/Adapters/OfferDevelopmentSeedGateway.cs` (variant-validated reuse + rebuild)
- `src/backend/Modules/Offer/Tooba.Offer.Tests/Infrastructure/OfferQueryGatewayTests.cs` (constructor call site)
- `src/backend/Modules/Offer/Tooba.Offer.Tests/Infrastructure/OfferDevelopmentSeedGatewayStaleVariantTests.cs` (new focused tests)
- `openspec/changes/dev-seed-stale-catalog-variant-288/**` (strict change)
- `docs/opencode/tasks/TB-DEV-SEED-STALE-CATALOG-VARIANT-288/**` (this task's evidence)
No change to: Offer 287 fingerprint files, Inventory, Pricing, public API, Host, `.cursor`, `AGENTS.md`, schema.

## Commands / results
- `dotnet test Tooba.Offer.Tests --filter "~OfferDevelopmentSeedGatewayStaleVariantTests|~OfferQueryGatewayTests"`
  → `Passed! - Failed: 0, Passed: 4` (evidence/focused-test.log).
- `dotnet build src/backend/Host/Tooba.Host -c Debug --no-restore` → `0 Error(s)` (evidence/host-build.log).
- `openspec validate dev-seed-stale-catalog-variant-288 --strict` → `Change ... is valid` (evidence/openspec-validate.txt).
- `node evidence/producer-288.cjs` → `evidence/observed-288.json`; producer SHA-256 recorded inside it
  (`6135089919d7d8e9750a19532bb0c747672af469d1723263fdb64896291a45be`).

## Ports / data safety
- Canonical Host 5088 `/health`=200; frontend 3000 `/fa`=200 (probed by producer).
- No task DB created or dropped; `taskDbCreated=false`. No shared dev DB writes by this task.

## Owner authorization + backup (2026-11-23)
- Identity: 5088 Host exe `...\.tmp-dev-host-out-5088\Tooba.Host.exe` SHA-256 `41EF43E2…` (non-candidate); tenant
  `store-alpha` / connectionReference `tenant-alpha` ⇒ DB `tooba_alpha` (PG 17.10, 53,327,539 bytes).
- Verified backup: `pg_dump -U admin -Fc -d tooba_alpha` → `/tmp` in container → `docker cp` to
  `C:\Users\User\AppData\Local\Temp\opencode\dev-db-backups\tooba_alpha_288backup_20261123_133446.dump`;
  bytes 3,091,575; SHA-256 `4839D6649C4E4066A404FA73EA95BFF60AE67343EEA4A74BF81F2C582B07FB9C`; `pg_dump` exit 0;
  `pg_restore --list` exit 0 (TOC 1476). Metadata: `evidence/dev-backup-metadata.txt`. **Not committed to git.**
- Candidate scope check: the worker build still contains the BLOCKED 287 migration
  `AddOfferCreationIntentFingerprint`, so it must NOT be deployed to `tooba_alpha`; a 288-only candidate from
  latest `origin/main` is required before any destructive run. Destructive deploy/reset therefore NOT executed
  (safety); S02/S03/S05 remain NOT_RUN. Restore remains available from the verified dump.

## Environment note (why S02/S03/S05 NOT_RUN)
5088 runs the MAIN worktree build (not the candidate) against shared `tenant-alpha`; the Catalog reset is
destructive; no explicitly authorized isolated fixture exists and reprovision is outside this task's budget, so the
connected reset→bootstrap replay is NOT_RUN (see RESULT.md blocker). `STOP_FOR_ARCHITECT`.
