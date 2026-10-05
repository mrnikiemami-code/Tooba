# TB-TMAR-CUSTOMERPROFILE-AMSC-001-W3 — Certify

## Mode

`ARCHITECT_DIRECT_AMSC` — Certify. No production behavior change: this wave records and locks the
ARCH-COMPLETE-002 certification state produced by W0→W2 and adds the durable certification guard.

Baseline: `HEAD == origin/main == 4902fca6` (W2).

## Verdict

| Field | Value |
|---|---|
| Verdict | `COMPLETE_REFERENCE_PATTERN` |
| Lock | `ARCH-COMPLETE-002` |
| Structure-State | `CERTIFIED` (W2 `READY_FOR_CERTIFY` preserved as historical W2 truth) |
| Folder-Granularity-State | `PROFESSIONAL_SHALLOW` |
| SolutionExplorer-State | `CANONICAL` (`/Modules/CustomerProfile/`, 5 projects) |
| PathNamespace-State | `EXACT` |
| RootAllowlist-State | `ENFORCED` |
| PhysicalCopy-State | `CLEAN` |
| File-Cohesion-State | `COHESIVE` |
| Foreign App/Infra/Domain coupling | `ZERO` |
| Endpoint-Ownership-State | `MODULE_OWNED` (4 routes) |
| Blocking residual debt | `ZERO` |
| `microserviceExtractable` | `true` |
| `automaticNextImplementationTask` | `NONE` |

## Wave lineage (accepted)

| Wave | Skill | Commit |
|---|---|---|
| W0 Analyze | `tooba-architecture-analyze` | `39a5de09` |
| W1 Migrate | `tooba-architecture-migrate` | `4599c97f` |
| W2 Structure | `tooba-architecture-structure` | `4902fca6` |
| W3 Certify | `tooba-architecture-certify` | *(this commit)* |

## ARCH-COMPLETE-002 certification checklist

| Requirement | Evidence | State |
|---|---|---|
| Application capability folders | `Profile/{Commands,Queries,Validators}` + `Account/{Queries,Models}` + shared `Composition`/`Ports`; no `Commands`/`Queries`/`Models`/`Validators` root axis | PASS |
| Endpoints capability folders | root = `CustomerProfileEndpointModule.cs` only; `Customer/`, `CustomerDashboard/`, `Errors/`, `Resources/` | PASS |
| Infrastructure capability folders | `DependencyInjection/`, `Directories/`, `Messaging/`, `Persistence/Migrations/`, `Development/`; no root `.cs`, no top-level `Migrations/` | PASS |
| Path ↔ namespace exact equality | all five projects, path-derived equality | PASS |
| Root allowlists | per-project allowlists match disk exactly; forbidden roots absent | PASS |
| CQRS / MediatR | 3 endpoint-reachable `IRequest<Result<T>>` + real `IRequestHandler<,>`, `ISender`-only endpoints, MediatR 12.5.0 | PASS |
| Validator coverage | exhaustive: 3 endpoint-reachable = 1 `VALIDATOR_REQUIRED` (present) + 2 `NO_VALIDATOR_REQUIRED_NO_TRANSPORT_INPUT` | PASS |
| API result pattern | `ApiResponseFactory.From(Result)`; zero `Results.Ok/Json/BadRequest/Problem` for business paths | PASS |
| Stable error-code owner | `Contracts/Errors/CustomerProfileErrorCodes.cs` (7 codes); 6 registered descriptors; `customer.session.required` consumed from Foundation | PASS |
| Localization | `CustomerProfileErrors.resx` + `.fa.resx` for every registered module code; zero hard-coded user-facing fault text | PASS |
| Typed faults | Domain/Infrastructure throw `ContractOperationException(<code>)`; `Application/Composition/CustomerProfileOperation.cs` maps to `Result` failures | PASS |
| Logging / telemetry / correlation | canonical; no ad-hoc pipeline; `traceId`/`correlationId` through the canonical provider | PASS |
| Contracts-only boundaries | foreign seams are Contracts-only; `ICustomerProfileDirectory` stays in Contracts (consumed by `Tooba.Identity.Application`) | PASS |
| No cross-module persistence/join | own `customer_profile` schema only | PASS |
| Schema preservation | 0 migration files touched; migration IDs, `Up`/`Down`, designer + snapshot entity identity byte-identical after the `Domain/Aggregates` move | PASS |
| Durable guards | W2 structure guard + W3 cert guard + inherited guards | PASS |
| Manifest + SoT | manifest `modules` entry `structureCertified: true`; `structureLock.certifiedModules` includes `CustomerProfile`; AMSC W0→W3 SoT records; Master Recovery checkpoint | PASS |
| Host closure | Host `CustomerProfile` folder absent / zero production `.cs`; composition-only seams preserved | PASS |

## Deliberate non-gap (validator classification)

W0 raised the two GET requests as a possible validator gap. W1 investigated and the W3 certification
upholds the result: the canonical repository rule is **transport-shape** validation, not "one validator
per request". Both GETs carry only a server-trusted `ActorUserId` and remain
`NO_VALIDATOR_REQUIRED_NO_TRANSPORT_INPUT`; the equivalent protection lives at the Application seam as a
defensive `ActorUserId != Guid.Empty` guard returning `customer.profile.actor_required`. No fabricated
`Guid.Empty` transport rule was added, and no ceremonial validator exists.

## SoT / manifest changes in this wave

- `docs/architecture/tmar-module-structure-manifests.json` — `CustomerProfile` moved from
  `preCertModules` to the certified `modules` array with `structureCertified: true` and the
  certification note updated; exactly one CustomerProfile entry remains.
- `docs/architecture/tmar-current-state.json` — `structureLock.certifiedModules` gains `CustomerProfile`;
  AMSC records `customerProfileModuleAmsc001W0` … `customerProfileModuleAmsc001W3` added with their own
  commit SHAs; root checkpoint (`lastAcceptedTask`, `workflowStop`, `automaticNextImplementationTask`)
  left untouched.
- `docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md` — CustomerProfile AMSC module recovery checkpoint
  with the accepted lineage and the `USER_REVIEW_CUSTOMERPROFILE_AMSC_001_W3` stop gate.
- `src/backend/Host/Tooba.Host.Tests/Architecture/CustomerProfileModuleAmsc001W2StructureGuardTests.cs`
  — the W2 guard's manifest source moved from `preCertModules` to the certified `modules` array; every
  structural assertion (allowlists, forbidden roots, namespaces) is unchanged, so no guard was weakened.
- `src/backend/Host/Tooba.Host.Tests/Architecture/CustomerProfileModuleAmsc001W3CertGuardTests.cs` —
  new durable certification lock (4 facts).

## Pre-existing drift (not CustomerProfile, not repaired here)

`TmarCompleteReferenceStructureGateTests` (3 facts: stale `Tooba.Catalog.Contracts.Cart` namespace
expectation and the stale Catalog certified-module lists) and
`TmarDurableGuardTests.Recovery_current_state_is_fresh_and_machine_readable` (its certifiedModules
equality list omits the already-certified `Catalog`) fail identically at the W3 starting HEAD
`4902fca6`. CustomerProfile appears in none of them; they are out of scope for this task and were left
untouched rather than weakened.

## Behavior preservation

| Surface | State |
|---|---|
| Routes / methods / response DTO shape | `UNCHANGED` |
| Session + actor resolution semantics | `UNCHANGED` |
| Domain invariants and derived names | `UNCHANGED` |
| Schema, table, columns, keys, migration identity | `UNCHANGED` |
| DI lifetimes / outbox registration | `UNCHANGED` |
| Host composition seams | `UNCHANGED` |

## Handoff

`Structure-State = CERTIFIED`. No next implementation task is authorized:
`automaticNextImplementationTask = NONE`, stop gate `USER_REVIEW_CUSTOMERPROFILE_AMSC_001_W3`.
