# Host delta — TB-TMAR-ORDER-GOLDEN-001-R4

## Removed (business authority left Host)

| File | Role |
|------|------|
| `Host/Admin/OrderInventoryRecoveryComposer.cs` | Recovery assess/audit/recover orchestration |
| `Host/Admin/OrderSupplyComposer.cs` | Supply status/ensure orchestration |
| `Host/Admin/AdminOrderInventoryRecoverySupplyEndpoints.cs` | HTTP routes for recovery/supply |
| `Host/Admin/HostAdminOrderOperationsInventoryRecoveryAdapter.cs` | Ops → Host recovery composer |
| `Host/Admin/HostAdminOrderOperationsSupplyAdapter.cs` | Ops → Host supply composer |
| `Host/Admin/HostAdminOrderSupplyStatusReader.cs` | OrdersGrid → Host supply composer |

Program.cs: removed scoped composer registrations and `MapAdminOrderInventoryRecoverySupplyEndpoints()`.

## Moved to Order

| Surface | Location |
|---------|----------|
| Queries | `Order.Application/Admin/InventoryRecovery/Queries/{Audit,Assess}…`, `Admin/Supply/Queries/GetOrderSupplyStatus` |
| Commands | `RecoverOrderInventoryReservation`, `EnsureOrderSupply` |
| Services | `OrderInventoryRecoveryService`, `OrderSupplyService` |
| HTTP | `Order.Endpoints/AdminOrderInventoryRecoverySupplyEndpoints` via `ISender` |
| Ops/Grid adapters | `Order.Infrastructure` implements recovery/supply Ops ports + `IAdminOrderSupplyStatusReader` |

Inventory.Contracts + Fulfillment.Contracts expanded for reservation/supply/rebind/shipped batch; owning adapters updated.

## Still remaining for Order (Host / deferred)

- Admin order **detail** (`GET /orders/{id}` + AdminViewAck) still Host (`AdminPanelComposer`)
- Storefront checkout / pending payment / shipping (Checkout **PAUSED_AT_SAFE_W5_CHECKPOINT**)
- Host consumers that **call** Order-owned `OrderSupplyService` (not owning authority): `AdminPanelComposer`, `CustomerPanelComposer`, `ReservationCycleCoordinator`, `AdminReservationCycleMapper` (MessageFa)

## Allowed adapters (Host)

- `HostOrderAdminAuthorizer`
- `HostOrderAdminEffectiveAccessReader` (AccessControl thin)
- Other auth-only / panel composers unchanged by R4

## nextTask (evidence only — not started)

`TB-TMAR-ORDER-GOLDEN-001-R5` — Storefront Order surfaces (evidence-driven). Checkout remains paused at W5.
