# TB-TMAR-HOST-ADMIN-CANON-005 — Analyze

## Scope

Re-home the Order admin effective-access cross-boundary contract consumed by the Host adapter:

- `IOrderAdminEffectiveAccessReader`
- `OrderAdminEffectiveAccess`
- `OrderAdminPermissionGrant`

from `Tooba.Order.Application.Admin.Operations.Ports` into `Tooba.Order.Contracts`.

## Discovery (pre-change state)

| Consumer | Pre-change reference |
| --- | --- |
| `src/backend/Host/Tooba.Host/Admin/HostOrderAdminEffectiveAccessReader.cs` | `using Tooba.Order.Application.Admin.Operations.Ports;` |
| `src/backend/Host/Tooba.Host/Program.cs` | `Tooba.Order.Application.Admin.Operations.Ports.IOrderAdminEffectiveAccessReader` |
| `Tooba.Order.Application/Admin/Operations/Ports/AdminOrderOperationsPorts.cs` | authoritative definitions |
| `Tooba.Order.Application/Admin/Operations/Services/AdminOrderOperationsOrchestrator.cs` | same-namespace consumption |
| `Tooba.Order.Application/Admin/Operations/Policies/AdminOrderOperationsPolicy.cs` | same-namespace consumption |
| `Host/Tooba.Host.Tests/AdminOrderOperationsTests.cs` | `Ports` + `Policies` usings |

The Host adapter therefore depended on an `Order.Application` port/model triple — a
cross-boundary contract that belongs in `Tooba.Order.Contracts`.

## Ownership decision

`IOrderAdminEffectiveAccessReader` is a Host-implemented seam consumed by Order
Application. Its model records are pure data with no Domain/ORM dependency.
Authority was moved to a single shallow capability path:

```text
Tooba.Order.Contracts/Admin/Operations/AdminOrderEffectiveAccessContracts.cs
namespace Tooba.Order.Contracts.Admin.Operations;
```

## Behavior preservation

No semantic change:

- `OrderAdminPermissionGrant(string PermissionId, bool DeniedByCeiling)` unchanged;
- `OrderAdminEffectiveAccess(IReadOnlyList<OrderAdminPermissionGrant> Permissions)` unchanged;
- `IOrderAdminEffectiveAccessReader.GetAsync(Guid, CancellationToken)` unchanged;
- `AdminOrderOperationsOrchestrator.Has/Prefer` list-ordering semantics unchanged;
- no new error path, no message parsing, no new exception behavior.

## Deferred (out of scope)

AccessControl coupling on `HostOrderAdminEffectiveAccessReader` (`IAccessControlDirectory`,
`Tooba.AccessControl.Application/Domain`) is intentionally **unchanged** and deferred to the
next bounded task. `HostOrderAdminAuthorizer` and `Host/Admin` foldering are untouched.
