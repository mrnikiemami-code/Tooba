# TB-TMAR-HOST-ROOT-GLOBAL-BOUNDARIES-001-R3 — Structural Repair

Skill: `tooba-architecture-migrate`.

## 1. Blocker 1 repair — exact path/namespace

Capability folder `Access/` kept (preferred per task).

Before:

```csharp
namespace Tooba.AccessControl.Contracts;
```

After:

```csharp
namespace Tooba.AccessControl.Contracts.Access;
```

No compatibility namespace alias, no shim, no duplicate contract file.

### Consumers repointed

| File | Change |
| ---- | ------ |
| `src/backend/Modules/Order/Tooba.Order.Infrastructure/Admin/Fulfillment/AdminOrderFulfillmentPermissionGate.cs` | `using Tooba.AccessControl.Contracts;` → `using Tooba.AccessControl.Contracts.Access;` |
| `src/backend/Modules/AccessControl/Tooba.AccessControl.Infrastructure/Adapters/AccessControlEffectiveAccessReader.cs` | `using Tooba.AccessControl.Contracts;` → `using Tooba.AccessControl.Contracts.Access;`; fully-qualified `AccessOwnerScope`/`AccessOwnerScopeKind` repointed to `…Contracts.Access.…` |
| `src/backend/Modules/AccessControl/Tooba.AccessControl.Infrastructure/AccessControlModule.cs` | DI registration `Tooba.AccessControl.Contracts.IAccessControlEffectiveAccessReader` → `Tooba.AccessControl.Contracts.Access.IAccessControlEffectiveAccessReader` |

`Tooba.Order.Infrastructure.csproj` and `Tooba.AccessControl.Infrastructure.csproj` keep the project
reference to `Tooba.AccessControl.Contracts.csproj` (assembly name unchanged — correct).

## 2. Blocker 2 repair — structure manifest

Added exactly one project entry inside the **existing single** `AccessControl` module entry in
`docs/architecture/tmar-module-structure-manifests.json`:

```json
{
  "projectName": "Tooba.AccessControl.Contracts",
  "rootAllowlist": [],
  "rootAllowlistJustification": "AccessControl.Contracts intentionally has no root .cs file: the capability folder Access/ carries the cross-module effective-access port and value types, and namespace alignment is exact path-derived equality.",
  "forbiddenRootFiles": [
    "AccessControlContracts.cs",
    "AccessControlEffectiveAccessContracts.cs"
  ],
  "forbiddenTopLevelFolders": []
}
```

- Module entry count for `AccessControl` remains **1** (no duplicate module entry).
- `rootAllowlist` reflects actual disk (`[]`, no root `.cs`).
- Capability-first structure matches disk (`Access/` only).
- Path/namespace exact.
- No forbidden-root workaround.

## 3. AccessControl.Contracts boundary

`Tooba.AccessControl.Contracts.csproj` references only
`BuildingBlocks/Tooba.BuildingBlocks`; foreign Application/Infrastructure/Domain = ZERO.

## 4. No behavior change

No AccessControl redesign, no authorization/SpiceDB change, no route/schema/frontend change.
