# TB-TMAR-HOST-AUTHORIZATION-AMC-001 — Analyze

Skill: `.cursor/skills/tooba-architecture-analyze/SKILL.md`
Transport: `ARCHITECT_DIRECT_ANALYZE_MIGRATE_CERTIFY` (no Bridge claim)
Scope: `full` (Analyze + Migrate + Certify)

## Active Host surface: `Authorization/` + authorization slice of `Health/`

| Question | Finding |
| --- | --- |
| Host-owned authorization folder | `src/backend/Host/Tooba.Host/Authorization/` — **6 production files** |
| Host-owned SpiceDB probe | `src/backend/Host/Tooba.Host/Health/SpiceDbHealthProbe.cs` (same slice, misfiled under `Health/`) |
| Namespace | all files `namespace Tooba.Host` (Host root dump) |
| SpiceDB SDK | `Authzed.Net 1.6.0` referenced directly by `Tooba.Host.csproj` |
| Role | platform-wide ReBAC authorization: schema provider, adapters, guard, registration, hosted service, options, instrumentation |
| Endpoint ownership | **NONE** — the folder owns zero HTTP routes |
| Persistence ownership | **NONE** — no DbContext, no migration, no table |
| Consumers | `Program.cs` (DI + options), `Health/HostReadinessEvaluator.cs` + `Health/HostHealthEndpoints.cs` (readiness probe + options bind) |

## Host files in scope (before)

| File | Responsibility |
| --- | --- |
| `Authorization/AuthorizationHostOptions.cs` | `Tooba:Authorization` option model + production validator |
| `Authorization/AuthorizationAdapters.cs` | `FoundationAuthorizationSchemaProvider`, `ConfiguredAuthorizationSchemaBootstrapper`, `InMemoryAuthorizationAdapter`, `FailClosedAuthorizationAdapter`, `AuthorizationGuard`, `InMemoryAuthorizationSecurityEventSink` |
| `Authorization/SpiceDbAuthorizationAdapter.cs` | real SpiceDB adapter on `Authzed.Api.V1` |
| `Authorization/AuthorizationRegistration.cs` | `AddToobaAuthorization` + `AuthorizationSchemaHostedService` |
| `Authorization/AuthorizationInstrumentation.cs` | authorization metrics |
| `Authorization/authorization-foundation.zed` | v3 foundation schema (ops artifact) |
| `Health/SpiceDbHealthProbe.cs` | SpiceDB readiness probe (`IDisposable`, `IOptions<AuthorizationHostOptions>`) |

## Contract surface already in BuildingBlocks

`Tooba.BuildingBlocks.Authorization.cs` already declares the neutral, host-agnostic contracts:
`IAuthorizationService`, `IAuthorizationTupleWriter`, `IAuthorizationGuard`,
`IAuthorizationSchemaProvider`, `IAuthorizationSchemaBootstrapper`,
`IAuthorizationSecurityEventSink`, `AuthorizationDecision`.

These contracts carry **no** Host type, so the implementation slice can move without changing the
platform seam consumed by other modules.

## Ownership decision

The authorization implementation is AccessControl's capability, not Host's:

- it implements AccessControl's ReBAC `capability`/`category` foundation schema;
- it is registered by `AccessControlModule.AddServices` (the module that already owns roles,
  permissions, assignments, ceiling and the capability gate);
- `Tooba.AccessControl.Infrastructure` is the natural implementation owner (SDK stays in
  Infrastructure, never in Domain/Application/Endpoints).

`Tooba.Host` keeps only its legitimate composition/readiness consumption, because readiness must
report authorization configurement without re-owning the adapter.

## Blocker classification

| Concern | Verdict |
| --- | --- |
| Host business authority | `ILLEGAL_BUSINESS_AUTHORITY` → must reach ZERO |
| Host endpoint ownership | already ZERO (no route) |
| Host persistence authority | already ZERO |
| Message-parsing fault classification | `SpiceDbAuthorizationAdapter.cs:170` classified failure by `ex.Message ==` sentinel → non-canonical |
| Schema/migration risk | NONE — no migration, no table, no route change |

## Migration shape (planned)

1. Move the 6 `Authorization/` files + `SpiceDbHealthProbe.cs` to
   `Modules/AccessControl/Tooba.AccessControl.Infrastructure/Authorization/`.
2. Move the `Authzed.Net` package reference from `Tooba.Host.csproj` to
   `Tooba.AccessControl.Infrastructure.csproj`.
3. Register options + `AddToobaAuthorization()` inside `AccessControlModule.AddServices`.
4. Replace string-message fault matching with a typed `SpiceDbUnavailableException`.
5. Repoint Host readiness/health to `SpiceDbAuthorizationOptions` through a public contract
   (`AppliedVersion` on `IAuthorizationSchemaBootstrapper`) instead of Host-owned state.
6. Delete `Host/Authorization/` and `Health/SpiceDbHealthProbe.cs`.
7. Add a durable `HostAuthorizationEvacuationGuardTests` guard and update path-pinned guards.
