# TB-TMAR-CONTENT-AMSC-001-W3 — Certify

## Verdict

`COMPLETE_REFERENCE_PATTERN` / `ARCH-COMPLETE-002 STRUCTURE_CERTIFIED`

Module: `src/backend/Modules/Content/Tooba.Content.*` — 5 projects, 89 production files.

## Structure gate (W2)

`Structure-State = READY_FOR_CERTIFY` with `Folder-Granularity-State = PROFESSIONAL_SHALLOW`,
`Solution-Explorer-State = CANONICAL`, `Path-Namespace-State = EXACT`, `Physical-Copy-State = CLEAN`,
`Root-Allowlist-State = ENFORCED`. Evidence: `docs/architecture/evidence/TB-TMAR-CONTENT-AMSC-001-W2/structure.md`.

## Verification results

| Gate | Result |
|---|---|
| Ownership | correct (module-owned domain/application/infrastructure/endpoints) |
| Endpoint ownership | `MODULE_OWNED`, Host HTTP ownership ZERO |
| CQRS | `COMPLIANT`, MediatR 12.5.0, `ISender` dispatch |
| Endpoint-reachable requests | **51** (17 `VALIDATOR_REQUIRED` / 34 `NO_VALIDATOR_REQUIRED`) |
| Validators | 17 concrete validators, all discoverable via `AddToobaCqrsFoundation` |
| API result pattern | `CANONICAL` — `ApiResponseFactory` only; zero `Results.Json/BadRequest/Problem/Ok` |
| Message classification | `ZERO` (`ex.Message` never parsed for routing/classification) |
| Localization | `CANONICAL` — 76 stable codes → 76 descriptors → 76 keys in `ContentErrors.resx` + `ContentErrors.fa.resx` |
| Hard-coded fault text | `ZERO` (14 Persian literals removed in W1) |
| Error descriptor ownership | unique owner `ContentErrorCodes`; zero duplicates, zero suppression |
| Typed faults | `ContractOperationException` + `SemanticException`, composed via `ContentOperation` |
| Logging / sensitive data | `CANONICAL` / `NONE` |
| OpenTelemetry / correlation | `CANONICAL` / `CANONICAL` |
| Cross-module boundary | `CONTRACTS_ONLY` (Localization.Contracts, Media.Contracts) |
| Foreign App/Infra/Domain coupling | `ZERO` |
| Cross-module joins / persistence | `ZERO` / `ZERO` |
| Persistence ownership | `CORRECT` (own `content` schema, own `ContentDbContext`) |
| Schema/migrations | `UNCHANGED`, 0 migration files changed |
| Alias / shim / `TypeForwardedTo` | `NONE` / `ZERO` |
| Host final closure | `PRESERVED` (no Host `Content` folder; composition seams only) |
| Sink-folder regression | `ZERO` |
| Microservice extractability | `true` |
| Blocking residual debt | `ZERO` |

## Durable guards

`ContentModuleAmsc001W3CertGuardTests`, `ContentModuleAmsc001W2StructureGuardTests`,
`HostContentAmcR1GuardTests`, `HostContentAmcR2GuardTests`, `HostContentAmcR3GuardTests`,
`HostContentAmcR4GuardTests`, `TmarCompleteReferenceStructureGateTests`, `TmarSourceSizeGuard`.

## Focused validation

- `dotnet build src/backend/Tooba.slnx` → succeeded, 0 errors.
- `dotnet test Tooba.Host.Tests --filter ~Content` → **54 passed / 0 failed / 14 skipped**
  (skips are Postgres Testcontainers integration tests).
- `ContentModuleAmsc001W2StructureGuardTests` → 8 passed.
- `ContentModuleAmsc001W3CertGuardTests` → passed.

## Non-blocking watch

- `ContentDirectory.cs` (721 LOC) and `ContentDevelopmentSeed.cs` (551 LOC) are single-responsibility
  and below the 800 LOC `ARCH-SIZE-001` ceiling; no baseline entry.
- The 15 transport validation codes stay declared in `Application/Validators/ContentValidationCodes.cs`
  and are intentionally not error-catalog descriptors (canonical `validation.failed` foundation path),
  matching the Offer/AccessControl/Cart/Fulfillment precedent.
- `Tooba.Content.Domain` keeps a legitimate self-module `Contracts` reference for stable codes and the
  boundary enum (established precedent across Cart/AddressBook/ProductQnA/PageComposition/Wishlist).

## Guards weakened / baselines widened

`NONE`.

## Manifest & SoT

`tmar-module-structure-manifests.json` Content entry promoted with the AMSC-001 lineage;
`tmar-current-state.json` records `contentModuleAmsc001W0/W1/W2/W3`.

## Wave commits

| Wave | Commit |
|---|---|
| W0 Analyze | `702537be` |
| W1 Migrate | `deb13ae8` |
| W2 Structure | `ce7b3938` |
| W3 Certify | *(this wave)* |
