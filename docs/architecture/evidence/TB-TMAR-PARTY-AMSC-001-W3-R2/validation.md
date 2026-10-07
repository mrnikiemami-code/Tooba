# TB-TMAR-PARTY-AMSC-001-W3-R2 — Bounded validation

Scope: recovery/SoT/evidence reconciliation only. Validation commands executed at the precheck/commit HEAD.

## SoT / manifest JSON parse

- `node -e "JSON.parse(...tmar-current-state.json)"` → parse OK (after W0/R1/R2 field edits).
- `docs/architecture/tmar-module-structure-manifests.json` NOT touched this wave (forbidden by task; `git diff` scope proof below).

## Exact SHA / parent-chain assertions

All 8 parent links verified via `git log --format='%H %P'` (see `lineage-reconciliation.md`):
29012df0<-2477bbb3 ✓ · ee9ba997<-29012df0 ✓ · ffff7100<-ee9ba997 ✓ · f0621ca6<-ffff7100 ✓ · d1cc2f48<-f0621ca6 ✓ · 548a7829<-d1cc2f48 ✓ · a75e3bf4<-548a7829 ✓ · HEAD==origin/main==a75e3bf4 ✓

- `partyAmsc001W0.commit` before = `PENDING_THIS_COMMIT` → after = `2477bbb3` (+full) ✓
- `partyAmsc001W3R1.commit` before = absent → after = `a75e3bf4` (+full) ✓; `certifiedCommit d1cc2f48` preserved ✓
- `partyAmsc001W3R2` block present with required fields; no self-referential PENDING commit ✓

## Stable error catalog counts

- `PartyErrorCodes` constants = 11 (`rg -c "public const string"`) ✓
- `PartyErrorCatalogContributor` descriptors = 11 (`rg -c "D\(PartyErrorCodes\."`) ✓
- W1 added constants = 0 (`git show 29012df0 … | Select-String '^\+.*public const string'` count = 0) ✓
- W3 guard asserting 11 still passes (below) ✓

## Promotion → Party.Application coupling

- `Tooba.Promotion.Infrastructure.csproj` contains only `Tooba.Party.Contracts.csproj` Party reference; zero `Party.Application` reference ✓ (guard `PartyModuleAmsc001W1MigrateGuardTests.Foreign_development_seeds_consume_party_contracts_only` also machine-enforces this and passes).

## Focused Party guards (no guard modified)

`dotnet test --filter "FullyQualifiedName~PartyModuleAmsc001"` → **Passed! 13 / Failed: 0** (W3 cert 5 + W2 structure 5 + W1 migrate 3) at the precheck HEAD; re-verified post-commit implicitly via no production change.

## git diff scope proof (production/manifest/schema untouched)

- `git diff a75e3bf4 HEAD --stat` over `src/` = **empty** (zero production drift between the task starting head and the pre-R2 HEAD).
- This wave's commit touches only: `docs/architecture/tmar-current-state.json`, `docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md`, `docs/architecture/evidence/TB-TMAR-PARTY-AMSC-001-W3-R2/*`, `docs/ai/tasks/TB-TMAR-PARTY-AMSC-001-W3-R2.task.md`.
- Forbidden files untouched: production files, Promotion production, csproj, Host production, frontend, `tmar-module-structure-manifests.json`, routes/DTO/error behavior, validators/resources, schema/migrations.
- Global recovery lock preserved: `lastAcceptedTask`/`lastAcceptedCommit`/`latestAcceptedImplementationWave`/`currentHostCheckpoint`/`nextHostFolder`/global `workflowStop`/global `automaticNextImplementationTask` untouched in SoT.

## Structure spot checks

- Manifest Party entry still `structureCertified: true` / `ARCH-COMPLETE-002` (untouched).
- Application capability axes remain flat (`Admin/Sellers/…`, `Seller/…` with request files directly on axes); W2/W1 guards passing confirm zero per-use-case request leaves.
- Validator matrix unchanged: 2 required present + 2 NO_VALIDATOR (`PartyModuleAmcW3CqrsGuardTests` + W3 cert guard pass).

## Commit/push

Exactly one R2 reconciliation commit; pushed to `origin/main`; `HEAD == origin/main` verified post-push.
