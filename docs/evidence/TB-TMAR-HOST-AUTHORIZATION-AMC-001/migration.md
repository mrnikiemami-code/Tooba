# TB-TMAR-HOST-AUTHORIZATION-AMC-001 — Migration

Skill: `.cursor/skills/tooba-architecture-migrate/SKILL.md`
Destination: `src/backend/Modules/AccessControl/Tooba.AccessControl.Infrastructure/Authorization/`

## Move map

| Host source | Module destination | Change class |
| --- | --- | --- |
| `Host/Authorization/AuthorizationHostOptions.cs` | `…/Authorization/SpiceDbAuthorizationOptions.cs` | renamed + namespace `Tooba.AccessControl.Infrastructure.Authorization`; `AuthorizationOptionsValidator` → `SpiceDbAuthorizationOptionsValidator`; `SectionName` = `Tooba:Authorization` (unchanged) |
| `Host/Authorization/AuthorizationAdapters.cs` | `…/Authorization/AuthorizationAdapters.cs` | `internal` → `public`; `ConfiguredAuthorizationSchemaBootstrapper` split out to the bootstrapper file; option type rebound |
| `Host/Authorization/SpiceDbAuthorizationAdapter.cs` | `…/Authorization/SpiceDbAuthorizationAdapter.cs` | typed `SpiceDbUnavailableException` replaces `ex.Message` sentinel matching |
| `Host/Authorization/AuthorizationRegistration.cs` | `…/Authorization/AuthorizationRegistration.cs` | `public static AddToobaAuthorization`; `AuthorizationSchemaHostedService` re-homed here |
| `Host/Authorization/AuthorizationInstrumentation.cs` | `…/Authorization/AuthorizationInstrumentation.cs` | namespace only |
| `Host/Authorization/authorization-foundation.zed` | `…/Authorization/authorization-foundation.zed` | moved verbatim (BOM stripped; v3 text identical) |
| `Host/Health/SpiceDbHealthProbe.cs` | `…/Authorization/SpiceDbAuthorizationBootstrapper.cs` | folded together with `ConfiguredAuthorizationSchemaBootstrapper` into one cohesive bootstrapper/probe file |

`Host/Authorization/` and `Host/Health/SpiceDbHealthProbe.cs` are **deleted**.

## Package boundary

- `Tooba.Host.csproj`: `<PackageReference Include="Authzed.Net" Version="1.6.0" />` **removed**.
- `Tooba.AccessControl.Infrastructure.csproj`: the same pinned `Authzed.Net 1.6.0` **added**.
- `Tooba.Host.Tests.csproj`: keeps `Authzed.Net 1.6.0` for the existing Testcontainers-backed
  `SpiceDbIntegrationTests` (test-only reference; not Host production ownership).

## Composition

- `Program.cs`: removed the Host-owned
  `AddOptions<AuthorizationHostOptions>() / AuthorizationOptionsValidator / AddToobaAuthorization()`
  triple; added `using Tooba.AccessControl.Infrastructure.Authorization;` so readiness/health can
  bind the module option type. `AddToobaModules` still composes the module.
- `AccessControlModule.AddServices`: now binds `SpiceDbAuthorizationOptions` from
  `Tooba:Authorization` with `ValidateOnStart()` and calls `services.AddToobaAuthorization()`.
- `HostReadinessEvaluator` / `HostHealthEndpoints`: parameter type changed to
  `SpiceDbAuthorizationOptions`; the `SpiceDbHealthProbe` DI resolve is unchanged.

## Contract change (BuildingBlocks)

`IAuthorizationSchemaBootstrapper` gained `int? AppliedVersion { get; }`, so `ConfiguredAuthorizationSchemaBootstrapper`
can expose applied-schema state as a neutral contract instead of Host-owned state. No existing
member was removed or renamed.

## Typed fault

`SpiceDbAuthorizationAdapter` previously classified transport failure by comparing
`ex.Message` to a sentinel string. It now throws/catches `SpiceDbUnavailableException`
(`public const string UnavailableCode = "authorization.unavailable"`), and the adapter maps that
typed failure to `AuthorizationDecision.Unavailable(reason)`. Behavior (fail-closed) is unchanged.

## Behavior preserved

- `Tooba:Authorization` configuration section name and all keys/modes (`Disabled`, `InMemory`,
  `SpiceDb`) unchanged.
- Production validation rules unchanged (`InMemory authorization is not allowed in Production.`,
  `SpiceDB TLS must be enabled in Production.`).
- Foundation schema `SchemaVersion = 3`, `definition capability`, `definition category` unchanged.
- Readiness checks (`spicedb-endpoint-missing`, `spicedb-token-missing`, `spicedb-unreachable`)
  unchanged.
- Routes: **NONE** were owned or added by this slice.
- Schema/migrations: **NONE**.
- Frontend: **UNCHANGED**.
