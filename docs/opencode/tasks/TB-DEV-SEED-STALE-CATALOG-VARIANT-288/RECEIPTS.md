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

## Environment note (why S02/S03/S05 NOT_RUN)
5088 runs the MAIN worktree build (not the candidate) against shared `tenant-alpha`; the Catalog reset is
destructive; no explicitly authorized isolated fixture exists and reprovision is outside this task's budget, so the
connected reset→bootstrap replay is NOT_RUN (see RESULT.md blocker). `STOP_FOR_ARCHITECT`.
