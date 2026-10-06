# TB-TMAR-NOTIFICATION-AMSC-001-W2 — Structure evidence (tooba-architecture-structure)

- Starting HEAD: `c660b933` (W1), `HEAD == origin/main`
- Skill: `tooba-architecture-structure` (behaviour-preserving physical normalization)
- Scope: `Tooba.Notification.Application` capability-first normalization; all other projects
  already satisfy their layer rules.

## 1. Physical tree — before (Application, W0/W1 state)

```text
Application/
  Commands/<UseCase>/<UseCase>Command.cs(+Validator.cs)   ← 4 use-case leaves ×1-2 files
  Queries/<UseCase>/<UseCase>Query.cs(+Validator.cs)      ← 4 use-case leaves ×1-2 files
  Composition/  Models/  Ports/  Rendering/  Validators/
```

`Folder-Granularity-State = MIXED` (`TECHNICAL_AXIS_FIRST` primary axis `Commands/`|`Queries/` +
`OVER_FOLDERED` single-file request leaves) — exactly the non-canonical shape rejected by the
Structure skill for a module with two real capabilities.

## 2. Physical tree — after (W2)

```text
Application/
  Customer/
    Commands/  MarkCustomerNotificationRead/(cmd+validator)  DismissCustomerNotification/(cmd+validator)
               MarkAllCustomerNotificationsRead/MarkAllCustomerNotificationsReadCommand.cs
    Queries/   ListCustomerNotifications/(query+validator)
               GetCustomerUnreadNotificationCount/GetCustomerUnreadNotificationCountQuery.cs
  Seller/
    Commands/  MarkSellerNotificationRead/(cmd+validator)  DismissSellerNotification/(cmd+validator)
               MarkAllSellerNotificationsRead/MarkAllSellerNotificationsReadCommand.cs
    Queries/   ListSellerNotifications/(query+validator)
               GetSellerUnreadNotificationCount/GetSellerUnreadNotificationCountQuery.cs
  Composition/NotificationOperation.cs
  Models/(3)  Ports/INotificationDirectory.cs  Rendering/NotificationCopy.cs
  Validators/NotificationValidationCodes.cs
```

Capability axis (Customer/Seller — the module's real audiences, mirroring Endpoints) is now primary;
Commands/Queries are secondary technical axes under the capability; shared cross-capability
concerns (Composition/Models/Ports/Rendering/Validators) stay shallow.

## 3. Classification states (post-wave)

| State | Value |
| --- | --- |
| Folder-Granularity-State | `PROFESSIONAL_SHALLOW` |
| Solution-Explorer-State | `CANONICAL` (`/Modules/Notification/` group with all 6 projects verified in `src/backend/Tooba.slnx` — pre-existing, unchanged) |
| Path-Namespace-State | `EXACT` (23 Application files re-namespaced `Tooba.Notification.Application.{Customer|Seller}.{Commands|Queries}.<UseCase>`; repo-wide reference repoint: Endpoints ×2, Host `Program.cs` CQRS assembly-scan reference, tests) |
| Physical-Copy-State | `CLEAN` (no stale leaf left; empty `Commands/`/`Queries/` shells deleted) |
| Root-Allowlist-State | `ENFORCED` (Application root: folders only, zero root `.cs`; module guard allowlist updated to the new canonical set) |

## 4. Leaf-folder justification

Each `<UseCase>/` leaf carries ≥ 2 cohesive production files (request + handler in one file is
counted as one source; validator is a distinct responsibility) — e.g.
`MarkCustomerNotificationRead/` = command+handler file **and** validator file. The two mark-all and
two unread-count leaves carry exactly one file each; they are retained as grouping-by-use-case
inside the same capability/axis branch consistent with their sibling pairs (no top-level technical
axis remains; the tree is capability-first). `MarkAllCustomerNotificationsRead` /
`GetCustomerUnreadNotificationCount` single-file leaves mirror their sibling use-case folders so
audience/axis placement stays uniform — the alternative (placing the bare file directly under
`Commands/`) would mix leaf styles inside one branch.

## 5. Domain / Contracts / Infrastructure / Endpoints structure

- Contracts: `Commands/ Copy/ Dtos/ Errors/ Ports/ Resources/ Routes/` — boundary semantics only. `CLEAN`.
- Domain: `Aggregates/ ValueObjects/` — `CLEAN`.
- Infrastructure: `DependencyInjection/ Directories/ Handlers/ Messaging/ Observability/ Projectors/ Persistence/(Migrations)` — `CLEAN`.
- Endpoints: `Customer/ Seller/ Errors/` + root composition entry — `CLEAN`.
- No root dump in any project (module guard `AssertNoRootDump` green on all five).

## 6. Focused validation

| Run | Result |
| --- | --- |
| `dotnet build Tooba.Notification.Tests` (builds all 5 module projects) | 0 errors |
| `dotnet build Tooba.Host` | 0 errors |
| `dotnet test Tooba.Notification.Tests` | **18 passed / 0 failed** |
| `dotnet test --filter NotificationModuleAmsc001W1` (W1 guard, paths updated) | **11 passed / 0 failed** |

Pre-existing unrelated red (unchanged, disclosed at W1): `TmarDurableGuardTests.Recovery_*`,
`TmarCompleteReferenceStructureGateTests.Certified_modules_satisfy...` (Catalog Contracts namespace
debt + frozen 16-module list; not Notification).

## 7. Durable guard updates

- `NotificationArchitectureGuardTests.AllowedApplicationFolders` →
  `["Ports", "Models", "Rendering", "Validators", "Composition", "Customer", "Seller"]` — the
  technical-axis-first roots `Commands`/`Queries`/`Errors`/`Services` are now **forbidden** at
  Application root (stronger than before, where they were allowed).
- W1 migrate guard validator paths repointed to the capability-first homes (still passes 11/11).

`Structure-State = READY_FOR_CERTIFY`.
