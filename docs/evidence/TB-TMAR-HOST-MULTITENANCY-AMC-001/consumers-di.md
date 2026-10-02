# consumers-di — TB-TMAR-HOST-MULTITENANCY-AMC-001

## Program DI

| Registration | Lifetime |
|---|---|
| `HttpCommerceContextAccessor` | Scoped |
| `ICurrentCommerceContext` → accessor | Scoped |
| `ICurrentEdition` → accessor | Scoped |
| `ICurrentTenant` → accessor | Scoped |
| `ICommerceContextAssigner` → accessor | Scoped |
| `ControlPlaneRegistry` via `PlatformOptionsValidator.BuildRegistry` | Singleton |
| `IDatabaseConnectionResolver` → `DatabaseConnectionResolver` | Singleton |
| `app.UseMiddleware<TenantResolutionMiddleware>()` | Conventional middleware (constructed once) |

## Production consumers

- Middleware populates `HttpContext.Items[ItemKey]` and assigns `IStoreCommerceContextAssigner`.
- Downstream Host/modules consume `ICurrentCommerceContext` / `ICurrentTenant` / `ICurrentEdition` (BuildingBlocks contracts) — not the concrete accessor type.
- Dev probe `MapGet("/__platform-commerce", (ICurrentCommerceContext commerce) => …)` in Program.

## Test-only consumers

- `TenantResolutionTests`, `TenantResolutionPlatformErrorTests`, `HostNormalizerTests`
- Test harnesses re-register accessor in: `MassTransitPostgresTests`, `OutboxTestSupport`, `PaymentFoundationTests`
- Errors W1/W2 guards source-scan MultiTenancy file (presentation seam assertions)

## Module consumers

Modules depend on BuildingBlocks / StoreContext.Contracts interfaces only — zero direct reference to Host MultiTenancy concretes.
