# Path ↔ namespace — AccessControl (W2)

## Classification

| Axis | State |
| --- | --- |
| Path-Namespace-State | `EXACT` |
| Namespace alias workaround | `ZERO` |

## Method

For every production `.cs` under `src/backend/Modules/AccessControl/**` (excluding `obj/` and
`bin/`), the declared namespace was compared with the path-derived namespace

```text
<ProjectName>[.<folder>…]
```

The scan covered all **86** production `.cs` files in the module.

## Result

```text
namespace mismatches = 0
```

Every file matched its path-derived namespace exactly. Representative samples:

| File | Declared namespace |
| --- | --- |
| `Application/Models/AccessOwnerScope.cs` | `Tooba.AccessControl.Application.Models` |
| `Application/Roles/Models/CreateRoleRequest.cs` | `Tooba.AccessControl.Application.Roles.Models` |
| `Application/Validation/AccessControlException.cs` | `Tooba.AccessControl.Application.Validation` |
| `Contracts/Errors/AccessControlErrorCodes.cs` | `Tooba.AccessControl.Contracts.Errors` |
| `Domain/Aggregates/AccessRole.cs` | `Tooba.AccessControl.Domain.Aggregates` |
| `Endpoints/Admin/AccessControlAdminEndpoints.cs` | `Tooba.AccessControl.Endpoints.Admin` |
| `Infrastructure/Persistence/AccessControlDbContext.cs` | `Tooba.AccessControl.Infrastructure.Persistence` |
| `Infrastructure/Persistence/Migrations/20260827140753_InitialAccessControl.cs` | `Tooba.AccessControl.Infrastructure.Persistence.Migrations` |

Root project files are correct by construction:

| File | Namespace |
| --- | --- |
| `Endpoints/AccessControlEndpointModule.cs` | `Tooba.AccessControl.Endpoints` |
| `Infrastructure/AccessControlModule.cs` | `Tooba.AccessControl.Infrastructure` |

## Locked exemptions

- `Persistence/Migrations/` — EF migration/designer/snapshot namespace exemption follows the
  existing repository locks; the namespaces are nevertheless already path-exact.
- No `GlobalUsings.cs` exists in the module, so the repository's global-using namespace
  exemption is not exercised here.

## Independent enforcement

`TmarCompleteReferenceStructureGateTests.Certified_modules_satisfy_root_allowlists_and_namespace_alignment`
re-derives the expected namespace from disk for every AccessControl production file on every
test run, so this verdict is machine-rechecked rather than asserted by hand.
