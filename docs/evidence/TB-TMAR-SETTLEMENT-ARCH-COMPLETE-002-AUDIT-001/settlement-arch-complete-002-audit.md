# TB-TMAR-SETTLEMENT-ARCH-COMPLETE-002-AUDIT-001 — Settlement ARCH-COMPLETE-002 readiness audit

AUDIT-ONLY. Zero Settlement / Host production change. Settlement is **NOT**
structure-certified by this task.

> Classification repair R1 (`TB-TMAR-SETTLEMENT-ARCH-COMPLETE-002-AUDIT-001-R1`):
> section B was corrected from an incorrect "all 10 requests VALIDATOR_REQUIRED"
> reading to the ARCH-COMPLETE-002 rule — 4 required / 6 no-validator-required
> (4 auth-scoped seller queries, 2 parameterless admin queries).

- Parent-Task: `TB-TMAR-PAYMENT-ARCH-COMPLETE-002-STRUCTURE-001` (ARCHITECT-ACCEPTED)
- Accepted certification commit: `3e403aaffb4e1f79f41bd7fd25de6fdaf0f708b0`
- Accepted SoT stamp: `0ae295e50b2e7adcefd6e2ca40001fbdb601d7d7`
- Protected: Checkout = `PAUSED_AT_SAFE_W5_CHECKPOINT`, `frontendFrozen = true`

## A. Endpoint + MediatR inventory

Exactly **10 endpoint-reachable** MediatR requests. Zero worker/internal-only requests.

| # | Request | Kind | Route | IRequest | Handler | ISender | Direct Application/Directory/DbContext call |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | `RequestSellerPayoutCommand` | Command | `POST /v1/seller/settlement/payout-requests` | yes | `RequestSellerPayoutCommandHandler` | yes | none |
| 2 | `GetSellerSettlementBalanceQuery` | Query | `GET /v1/seller/settlement/balance` | yes | `GetSellerSettlementBalanceQueryHandler` | yes | none |
| 3 | `ListSellerSettlementEntriesQuery` | Query | `GET /v1/seller/settlement/entries` | yes | `ListSellerSettlementEntriesQueryHandler` | yes | none |
| 4 | `ListSellerSettlementStatementsQuery` | Query | `GET /v1/seller/settlement/statements` | yes | `ListSellerSettlementStatementsQueryHandler` | yes | none |
| 5 | `ListSellerPayoutRequestsQuery` | Query | `GET /v1/seller/settlement/payout-requests` | yes | `ListSellerPayoutRequestsQueryHandler` | yes | none |
| 6 | `ProcessAdminPayoutCommand` | Command | `POST /v1/admin/settlement/payout-requests/{id}/process` | yes | `ProcessAdminPayoutCommandHandler` | yes | none |
| 7 | `RetryAdminPayoutCommand` | Command | `POST /v1/admin/settlement/payout-requests/{id}/retry` | yes | `RetryAdminPayoutCommandHandler` | yes | none |
| 8 | `ListAdminSettlementBalancesQuery` | Query | `GET /v1/admin/settlement/balances` | yes | `ListAdminSettlementBalancesQueryHandler` | yes | none |
| 9 | `ListAdminPayoutQueueQuery` | Query | `GET /v1/admin/settlement/payout-queue` | yes | `ListAdminPayoutQueueQueryHandler` | yes | none |
| 10 | `QueryAdminPayoutGridQuery` | Query | `POST /v1/admin/settlement/payout-queue/query` | yes | `QueryAdminPayoutGridQueryHandler` | yes | none |

- endpoint-reachable request count = **10**
- total Settlement MediatR request count = **10** (`IRequest`/`IRequestHandler` appear only under `Tooba.Settlement.Application`)
- worker/internal-only request count = **0**

Endpoints are thin: authorizer + `ISender` + `ApiResponseFactory` only; no direct
Application service, Directory, or DbContext call from any endpoint.

## B. FluentValidation coverage

| Classification | Count |
| --- | --- |
| endpoint-reachable requests | 10 |
| `VALIDATOR_REQUIRED` | 4 |
| validators present | 0 |
| validators missing | 4 |
| `NO_VALIDATOR_REQUIRED_<reason>` | 6 |

Transport/input validation belongs in FluentValidation; trusted
authorization-derived values and zero-input requests do not require ceremonial
validators (same rule as the accepted Offer precedent for auth-scoped seller identity).

### `VALIDATOR_REQUIRED` — exactly four

| Request | Untrusted input | Expected validator | Present |
| --- | --- | --- | --- |
| `RequestSellerPayoutCommand` | body `Amount`, `IdempotencyKey` | `RequestSellerPayoutCommandValidator` | missing |
| `ProcessAdminPayoutCommand` | route `PayoutRequestId` | `ProcessAdminPayoutCommandValidator` | missing |
| `RetryAdminPayoutCommand` | route `PayoutRequestId` | `RetryAdminPayoutCommandValidator` | missing |
| `QueryAdminPayoutGridQuery` | `GridQueryRequest` body | `QueryAdminPayoutGridQueryValidator` | missing |

- `RequestSellerPayoutCommand`: `Amount > 0`, `IdempotencyKey` non-blank.
- `ProcessAdminPayoutCommand` / `RetryAdminPayoutCommand`: `PayoutRequestId` non-empty.
- `QueryAdminPayoutGridQuery`: envelope/null/basic shape only (primitive grid envelope);
  grid field/operator/sort/connector policy remains owned by the existing
  `AdminPayoutGridQueryPolicy`.
- `ActorUserId` / `SellerPartyId` values supplied by trusted authorizers must **not** be
  the reason for creating validators.

### `NO_VALIDATOR_REQUIRED` — six

| Request | Reason |
| --- | --- |
| `GetSellerSettlementBalanceQuery` | `NO_VALIDATOR_REQUIRED_AUTH_SCOPED_QUERY` |
| `ListSellerSettlementEntriesQuery` | `NO_VALIDATOR_REQUIRED_AUTH_SCOPED_QUERY` |
| `ListSellerSettlementStatementsQuery` | `NO_VALIDATOR_REQUIRED_AUTH_SCOPED_QUERY` |
| `ListSellerPayoutRequestsQuery` | `NO_VALIDATOR_REQUIRED_AUTH_SCOPED_QUERY` |
| `ListAdminSettlementBalancesQuery` | `NO_VALIDATOR_REQUIRED_NO_INPUT` |
| `ListAdminPayoutQueueQuery` | `NO_VALIDATOR_REQUIRED_NO_INPUT` |

The four seller queries receive `SellerPartyId` only from
`ISettlementSellerAuthorizer.RequireAuthorizedAsync(...)` and have no untrusted request
payload. The two admin queries are parameterless and are created only after admin
authorization. No ceremonial empty validator is proposed for any of the six.

Current state: `Tooba.Settlement.Application` has **no `Validators/` folder**, no
`FluentValidation` package/reference anywhere in Settlement, and no validator type.
Existing `AddToobaCqrsFoundation` / `AddValidatorsFromAssembly` discovery is therefore
not exercised for Settlement. Missing validators are exactly the four above.

Transport shape only was considered; no business/domain rule is proposed as a
FluentValidation rule.

## C. Physical structure

`Tooba.Settlement.Application`: `Commands/` (3), `Queries/` (6), `Errors/`, `Models/`,
`Ports/`. Root `.cs`: `GlobalUsings.Domain.cs`, `GlobalUsings.Layout.cs`.

`Tooba.Settlement.Endpoints`: `Admin/`, `Seller/`. Root `.cs`:
`SettlementEndpointModule.cs`.

`Tooba.Settlement.Infrastructure`: `Adapters/`, `Bridges/`, `DependencyInjection/`,
`Directories/`, `Errors/`, `Gateways/`, `Handlers/`, `Messaging/`, `Observability/`,
`Persistence/`, `Persistence/Migrations/`, `Queries/`. Root `.cs`:
`GlobalUsings.Domain.cs`, `GlobalUsings.Layout.cs`.

`Tooba.Settlement.Domain`: `Aggregates/`, `Entities/`, `Events/`, `ValueObjects/`.
`Tooba.Settlement.Contracts`: `History/`, `Operations/`.

Grouping quality: already capability/integration grouped. `artifacts/` exists in three
projects but is empty and untracked (not source). No file was moved.

- likely Application root allowlist: `[]`
- likely Endpoints root allowlist: `[SettlementEndpointModule.cs]`
- likely Infrastructure root allowlist: `[]`
- forbidden flattened files: `SettlementDirectory.cs`, `SettlementDbContext.cs`,
  `SettlementModule.cs`, `SettlementContracts.cs`, `SettlementAdminModels.cs`,
  `SettlementErrorCodes.cs`, `SettlementOutboxRegistration.cs`,
  `SettlementEventHandlers.cs`, `AdminPayoutGridQueryEngine.cs`, `SettlementEndpointModule.cs`
  at Infrastructure/Application root, and flattened `SettlementAdminEndpoints.cs` /
  `SettlementSellerEndpoints.cs` / authorizers at Endpoints root.

## D. Exact path ↔ namespace

Scanned every production `.cs` across Application/Endpoints/Infrastructure/Domain/Contracts
(excluding `bin`/`obj`/`artifacts`, with the legitimate EF `Migrations` + `ModelSnapshot`
exemption). Every file matches its path-derived namespace **exactly**:

- exact match: all
- mismatch: **none**
- prefix-only (StartsWith) acceptance: **not** used as an accepted proof target

## E. Namespace alias / global using workarounds

| Artifact | Content | Classification |
| --- | --- | --- |
| `Application/GlobalUsings.Domain.cs` | Domain Aggregates/Entities/Events/ValueObjects | legitimate project-wide import |
| `Application/GlobalUsings.Layout.cs` | `Tooba.Settlement.Application.Ports` | legitimate project-wide import |
| `Infrastructure/GlobalUsings.Domain.cs` | Domain Aggregates/Entities/Events/ValueObjects | legitimate project-wide import |
| `Infrastructure/GlobalUsings.Layout.cs` | Application.Ports + Infrastructure Directories/DependencyInjection/Messaging/Handlers/Observability/Bridges/Gateways | legitimate project-wide import |

No namespace aliases, no `TypeForwardedTo`, no compatibility shims, no foreign-module
global alias. No workaround/debt requiring repair.

## F. Host Settlement residue

| Host artifact | Classification |
| --- | --- |
| `Admin/HostSettlementAdminAuthorizer.cs` | KEEP_AS_EXPLICIT_THIN_HOST_SECURITY_ADAPTER |
| `Seller/HostSettlementSellerAuthorizer.cs` | KEEP_AS_EXPLICIT_THIN_HOST_SECURITY_ADAPTER |
| `Development/MarketplaceDevelopmentBootstrap.cs` (`SettlementDbContext` migrate) | NOT_SETTLEMENT_AUTHORITY (dev bootstrap, allowlisted) |
| `Program.cs` (`AddToobaCqrsFoundation` assembly, authorizer registrations, `MapSettlementEndpoints`) | NOT_SETTLEMENT_AUTHORITY (composition root) |
| `Composition/ToobaModuleComposition.cs` (`new SettlementModule()`) | NOT_SETTLEMENT_AUTHORITY (module wiring) |
| `Tooba.MigrationRunner/ModuleMigrationRegistry.cs` | NOT_SETTLEMENT_AUTHORITY (migration registry) |
| `GlobalUsings.SettlementApp.cs` / `GlobalUsings.SettlementDomain.cs` | NOT_SETTLEMENT_AUTHORITY (unused import aggregation; no Host domain-type usage found) |

No Host Settlement endpoints, grid engine, panel composer, or settlement business logic.
`SettlementDbContext` in Host is limited to the `Program.cs` / `ModuleMigrationRegistry.cs` /
`MarketplaceDevelopmentBootstrap.cs` allowlist. No `MOVE_TO_SETTLEMENT_MODULE` and no
`REMOVE_DEAD_RESIDUE` items remain.

**Settlement → Host dependency = ZERO.**

## G. Cross-module boundaries

- `Settlement.Domain` → no foreign module dependency.
- `Settlement.Application` → `Settlement.Domain` + `Party.Contracts` + BuildingBlocks (allowed).
- `Settlement.Infrastructure` → Contracts-only: `Order.Contracts`, `Payment.Contracts`,
  `Returns.Contracts`, `Party.Contracts`, plus ModuleContracts/Persistence. No foreign
  Application/Domain/Infrastructure and no foreign DbContext.
- `Settlement.Endpoints` → `Settlement.Application` + BuildingBlocks only; no Infrastructure, no Host.

Violations: **none**.

## H. Structure guard quality

`SettlementArchitectureGuardTests` currently proves: no root dumping-ground
(folder-name allowlist), no `TypeForwardedTo`, selected Host/DbContext and route
ownership, exception-mapper stability, clock/id-generator discipline, no silent catch.
Namespace check uses a **loose `StartsWith` prefix**.

Gaps (not fixed in this audit):

1. no exact path-derived namespace equality
2. no explicit root allowlist / forbidden root file set
3. no alias-workaround (foreign global alias / shim) rejection
4. no exhaustive endpoint request inventory
5. no validator coverage guard
6. no MediatR 12.5.0 assertion
7. no ISender-only / no-direct-validator-invocation invariant
8. no explicit Settlement → Host ZERO and Host residue two-adapter guard

## I. Certification plan

**Decision: `NEEDS_PRECERT_REPAIR_THEN_STRUCTURE`**
Repair scope (do not execute here):

- `TB-TMAR-SETTLEMENT-ARCH-COMPLETE-002-PRECERT-REPAIR-001` — add exactly **four**
  primitive-shape transport validators under `Tooba.Settlement.Application/Validators/`
  (`RequestSellerPayoutCommandValidator`, `ProcessAdminPayoutCommandValidator`,
  `RetryAdminPayoutCommandValidator`, `QueryAdminPayoutGridQueryValidator`), discovered via
  the existing `AddToobaCqrsFoundation`/`AddValidatorsFromAssembly`, with a focused validator
  test and an exhaustive endpoint-validator coverage guard; no business rules, no grid
  policy duplication, and no validator for the six `NO_VALIDATOR_REQUIRED` requests.

Then `TB-TMAR-SETTLEMENT-ARCH-COMPLETE-002-STRUCTURE-001` adds exact namespace,
root allowlist, alias rejection, endpoint inventory and Host/cross-module guards plus
manifest/SoT certification.

Settlement is NOT added to `certifiedModules`, NOT marked `structureCertified`, and is
NOT removed from `uncertifiedHttpOwningModules`.

## J. Focused validation

- `SettlementArchitectureGuardTests` (focused filter) — PASS (3/3)
- `dotnet build src/backend/Modules/Settlement/Tooba.Settlement.Tests/Tooba.Settlement.Tests.csproj --no-restore` — 0 errors

No broad suite, no Host suite, no solution build, no Testcontainers, no DB tests.
