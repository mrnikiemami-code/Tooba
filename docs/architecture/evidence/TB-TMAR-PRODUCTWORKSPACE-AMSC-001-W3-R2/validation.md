# TB-TMAR-PRODUCTWORKSPACE-AMSC-001-W3-R2 — Validation

Starting HEAD: `e5575d68e72171f8bd09458e2fc91a4432de5cb5` (`main`, `HEAD == origin/main`, clean tree).

## Scope of validation (per task: focused only)

The task explicitly forbids a full-suite requirement for this wave and asks for the
ProductWorkspace AMSC guards plus `ErrorCatalogUniqueCodeGuard`, with a Host.Tests build if needed.

```powershell
dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj `
  --filter "FullyQualifiedName~ProductWorkspaceModuleAmsc001|FullyQualifiedName~ErrorCatalogUniqueCodeGuardTests"
```

Result:

```text
Passed!  - Failed:     0, Passed:    28, Skipped:     0, Total:    28, Duration: 13 s
```

The 28 passing facts are the ProductWorkspace AMSC wave guards
(`ProductWorkspaceModuleAmsc001W1MigrateGuardTests`,
`ProductWorkspaceModuleAmsc001W2StructureGuardTests`,
`ProductWorkspaceModuleAmsc001W3CertGuardTests`,
`ProductWorkspaceModuleAmsc001W3R1CertRepairGuardTests`) plus the new
`ProductWorkspaceModuleAmsc001W3R2CertGuardTests` (6 facts) and
`ErrorCatalogUniqueCodeGuardTests` (3 facts).

## Build

`Tooba.Host.Tests` builds clean for the new guard; the compile emits only the pre-existing
`xUnit2013` style warnings already present in the repository baseline (no new warning class,
no error).

## Independent certification checks (disk-derived)

The full check table, including the re-derivation of the 0/17 validator split, is recorded in
`certification.md`. Summary of the observed values at this HEAD:

```text
module-owned routes ................ 17
endpoint-reachable requests ........ 17  (3 queries + 14 commands)
IRequestHandler bindings ........... 17  (1:1, zero missing, zero extra)
orphan requests .................... 0
AbstractValidator / IValidator< .... 0
Results.Json / BadRequest / Problem  0
api.Created 201 paths .............. 2
solution projects .................. 5
.gitkeep files ..................... 0
foreign App/Infra/Domain edges ..... 0
DbContext / EF / Host usage ........ 0
ProductWorkspace migrations ........ 0
```

## Baseline integrity

- Guards weakened: `NONE`.
- Baselines widened: `NONE`.
- Production code changed: `false` — this wave touches SoT, the ProductWorkspace manifest
  `certificationNote`, the new cert guard, the Master Recovery checkpoint and R2 evidence only.
- No unrelated module touched; no structure, project, schema or migration change.
