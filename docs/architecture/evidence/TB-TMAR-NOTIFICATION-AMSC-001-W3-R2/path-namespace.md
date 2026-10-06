# TB-TMAR-NOTIFICATION-AMSC-001-W3-R2 — path-namespace

Rule: every production `.cs` namespace equals the path-derived namespace of its project
(`PATH_NAMESPACE_ALIGNMENT = EXACT`); EF `Persistence/Migrations` + `ModelSnapshot` keep their
locked exemption.

## Request-axis namespaces (repaired)

| Path | Namespace |
| --- | --- |
| `Application/Customer/Commands/*.cs` | `Tooba.Notification.Application.Customer.Commands` |
| `Application/Customer/Queries/*.cs` | `Tooba.Notification.Application.Customer.Queries` |
| `Application/Seller/Commands/*.cs` | `Tooba.Notification.Application.Seller.Commands` |
| `Application/Seller/Queries/*.cs` | `Tooba.Notification.Application.Seller.Queries` |

Before the repair these files carried leaf-qualified namespaces such as
`Tooba.Notification.Application.Customer.Commands.MarkCustomerNotificationRead` (namespace deeper
than path after flattening would have left `MISMATCH`), so the namespace of all 16 moved files was
rewritten to the exact axis namespace. The stale same-namespace `using` self-imports inside the six
validator files were removed (no longer needed — validators and requests share the axis namespace).

## Verification

- `NotificationModuleAmsc001W3R2StructureRepairGuardTests.Request_axis_namespaces_are_exactly_the_axis_namespaces`
  asserts the exact namespace per file across all 16 axis sources.
- `NotificationModuleAmsc001W3CertGuardTests.Path_namespace_alignment_is_exact_for_all_production_files`
  (exact path-derived equality, all five production projects, migrations exempt) — PASS after repair.
- `Tooba.Notification.Tests` `NotificationArchitectureGuardTests` path/namespace alignment — PASS.
- No namespace alias, no `TypeForwardedTo`, no flattened-namespace shim introduced.
