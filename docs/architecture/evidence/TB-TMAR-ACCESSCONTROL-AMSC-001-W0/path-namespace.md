# Path ↔ namespace exactness — AccessControl (W0)

## Verdict

`Path-Namespace-State = EXACT`

Method: for every non-generated production `.cs` file under
`src/backend/Modules/AccessControl/**` (75 files, excluding `bin`/`obj`), the declared namespace was
compared against the path-derived namespace
`Tooba.AccessControl.<Layer>.<Directory.Path>`.

Result: **0 mismatches**.

The four `Persistence/Migrations/*.cs` files use the standard EF Core block-scoped namespace
(`namespace Tooba.AccessControl.Infrastructure.Persistence.Migrations` with no file-scoped
semicolon) and match their path exactly. This is the repository's locked exemption style for
generated migrations and the model snapshot; it is not a mismatch.

## Root files

| Project | Root `.cs` files | Manifest allowlist |
| --- | --- | --- |
| `Tooba.AccessControl.Application` | (none) | `[]` — enforced |
| `Tooba.AccessControl.Contracts` | (none) | `[]` — enforced |
| `Tooba.AccessControl.Domain` | (none) | `[]` — enforced |
| `Tooba.AccessControl.Endpoints` | `AccessControlEndpointModule.cs` | `["AccessControlEndpointModule.cs"]` |
| `Tooba.AccessControl.Infrastructure` | `AccessControlModule.cs` | `["AccessControlModule.cs"]` |

## Alias / shim audit

| Check | Result |
| --- | --- |
| `TypeForwardedTo` usage | none |
| `global using` hiding a foreign module | none |
| `using X = Y` alias hiding wrong placement | one **legitimate** alias only: `Infrastructure/Adapters/AccessControlEffectiveAccessReader.cs` aliases `AppScope = Tooba.AccessControl.Application.Models.AccessOwnerScope` and `ContractScope = Tooba.AccessControl.Contracts.Access.AccessOwnerScope` to disambiguate two same-named types of the **same module**. It does not hide foreign coupling or folder debt. |
| duplicate compatibility type | none |
| stale root copy / duplicate physical copy | none (see `stale-duplicate-copy.md`) |
| namespace alias hiding a wrong folder | none |

The `AppScope`/`ContractScope` alias is an intentional, documented mapping between the module's own
Contracts boundary DTO and its own Application-internal value. W1 relocates
`Application.Models.AccessOwnerScope` to `Application/Models/AccessOwnerScope.cs`, so the alias target
namespace is unchanged and no alias update is required.
