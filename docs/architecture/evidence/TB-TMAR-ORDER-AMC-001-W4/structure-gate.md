# TB-TMAR-ORDER-AMC-001-W4 — Structure gate for Certify

## Mandatory Structure-Gate states (W4 invocation)

| Gate | State |
|---|---|
| `Structure-State` | `READY_FOR_CERTIFY` |
| `Folder-Granularity-State` | `PROFESSIONAL_SHALLOW` |
| `Solution-Explorer-State` | `CANONICAL` |
| `Path-Namespace-State` | `EXACT` |
| `Physical-Copy-State` | `CLEAN` |
| `Root-Allowlist-State` | `ENFORCED` |
| `File-Cohesion-State` | `COHESIVE` |
| Host final closure | `PRESERVED` |

## 1. Root allowlists (measured on disk, W4)

| Project | Root `.cs` files | Verdict |
|---|---|---|
| `Tooba.Order.Contracts` | *(none)* | ENFORCED (empty allowlist) |
| `Tooba.Order.Domain` | `GlobalUsings.cs` | ENFORCED |
| `Tooba.Order.Application` | `GlobalUsings.cs` | ENFORCED |
| `Tooba.Order.Infrastructure` | `GlobalUsings.cs`, `OrderModule.cs` | ENFORCED |
| `Tooba.Order.Endpoints` | `OrderEndpointModule.cs` | ENFORCED |

No capability/implementation file sits at any project root.

## 2. Capability-first shallow grouping

`Tooba.Order.Application` top-level axes are business capabilities:
`Admin/{Completeness,Customers,Dashboard,Detail,InventoryRecovery,LegacyList,Operations,OrdersGrid,Sellers,Settings,Supply}`,
`Checkout/{Abuse,Contracts,Policies,Process}`, `Customer`, `ReservationCycle/{Contracts,Policies,Services}`,
`Seller/{Models,Policies,Ports,Queries}`, `Storefront/{Checkout,Geography,Models,PendingPayment,Ports,Services,Shipping}`,
`Validation`.

Technical axes (`Commands`, `Queries`, `Models`, `Ports`, `Validators`, `Services`, `Policies`) exist **only as
secondary axes under a capability**. No `Application/Commands/…` or `Application/Queries/…` root exists.

## 3. Single-file Command/Query leaf folders — ZERO

Every `Commands/<UseCase>` / `Queries/<UseCase>` leaf holds **2** production source files
(request + handler) or more:

- `Admin/Operations/Commands/*` (29 leaves), `Admin/Operations/Queries/GetAdminOrderOperations`
- `Admin/Completeness/Commands/*`, `Admin/Completeness/Queries/*` (5 leaves)
- `Admin/InventoryRecovery/{Commands,Queries}/*` (3 leaves), `Admin/Supply/{Commands,Queries}/*`
- `Admin/Detail/Queries/GetAdminOrderDetail`, `Admin/Customers/Queries/QueryAdminCustomersGrid`
- `Admin/OrdersGrid/Queries/QueryAdminOrdersGrid`, `Admin/Settings/ReservationPolicy/{Commands,Queries}`
- `Customer/{Commands,Queries}/*` (3 leaves), `Seller/Queries/{GetSellerOrderDetail,ListSellerOrders}`
- `Storefront/Checkout/{Commands,Queries}/*` (3 leaves), `Storefront/PendingPayment/*` (2 leaves)
- `Storefront/Shipping/{Commands,Queries}/*` (3 leaves)

Capability-level `Commands`/`Queries` folders that hold request files **directly** (no per-use-case leaf) are the
canonical shallow form: `Admin/Customers/Queries`, `Admin/Dashboard/Queries`, `Admin/LegacyList/Queries`,
`Admin/Sellers/Queries`, `Admin/Settings/ReservationPolicy/{Commands,Queries}`, `Customer/Queries`,
`Seller/Queries`, `Storefront/PendingPayment/Queries`.

Measured violations: **0**.

## 4. New durable structure guard

`Tooba.Order.Tests/Architecture/OrderApplicationFolderGranularityGuardTests.cs`:

- `No_unjustified_single_file_command_or_query_leaf_folders` — walks every `Commands`/`Queries` folder under
  `Tooba.Order.Application` and fails on a leaf containing exactly one `.cs` file and no nested folder.
- `No_technical_axis_first_command_or_query_root` — fails if `Application/Commands` or `Application/Queries`
  is resurrected.

## 5. Path ↔ namespace

324 production `.cs` files scanned across the five Order projects (excluding `bin`/`obj`/`artifacts` and
EF `Persistence/Migrations`):

- mismatches: **0**
- namespace-less files: 3, all `GlobalUsings.cs` (global-using compilation unit — no namespace by design)

## 6. Physical copies

- Duplicate file names within Order: only the four per-project `GlobalUsings.cs` (expected, one per project).
- The pre-W3 Application path `Tooba.Order.Application/PurchaseVerification/` is **absent**; the contract lives
  only at `Tooba.Order.Contracts/PurchaseVerification/OrderPurchaseVerificationContracts.cs`.

## 7. Solution Explorer

`src/backend/Tooba.slnx` groups all six Order projects under `/Modules/Order/`:
`Tooba.Order.Domain`, `Tooba.Order.Contracts`, `Tooba.Order.Application`, `Tooba.Order.Endpoints`,
`Tooba.Order.Infrastructure`, `Tooba.Order.Tests`. No stale entry, no missing project.

## 8. File cohesion

Largest Order production files after the W3 splits:

| File | LOC |
|---|---|
| `Admin/Operations/Services/AdminOrderOperationsOrchestrator.Rules.cs` | 560 |
| `Storefront/Services/StorefrontShippingService.cs` | 470 |
| `Admin/OrdersGrid/AdminOrdersGridReader.cs` | 469 |
| `Checkout/Persistence/CheckoutDirectory.cs` | 449 |
| `Admin/Operations/Services/AdminOrderOperationsOrchestrator.Projection.cs` | 433 |

All single-responsibility and under the ceiling; no multi-responsibility god file, no cosmetic over-split.

## 9. Host final closure

`HOST_ROOT_FINAL_CERTIFIED` preserved. W3/W4 touched **zero** Host production files. Host owns only the
composition reference to `Order.Infrastructure`/`Order.Endpoints`/`Order.Contracts` and maps the module.

## Focused validation

```text
dotnet build src/backend/Tooba.slnx
Build succeeded. 0 Error(s)

dotnet test src/backend/Modules/Order/Tooba.Order.Tests
Passed! - Failed: 0, Passed: 139, Skipped: 0, Total: 139
```

The gate is `READY_FOR_CERTIFY`; certification verdict is recorded separately in the W4 certification report.
