# ownership-boundary — TB-TMAR-HOST-MULTITENANCY-AMC-001

## Host may own

- HTTP Host header → tenant allowlist resolution
- Edition branching (Unset / Marketplace / SingleStore)
- Fail-closed HTTP presentation for resolution failures
- HttpContext.Items-backed `CommerceContext` accessor implementation
- Request-scoped bridge assigning StoreCommerce into StoreContext accessor

## StoreContext owns

- `StoreCommerceContext` contracts
- `ICurrentStoreCommerceContext` / `IStoreCommerceContextAssigner` + Infrastructure accessor
- Non-HTTP store commerce semantics

## BuildingBlocks owns

- `CommerceContext` / `EditionContext` / `TenantContext` models
- `ICurrentCommerceContext` / `ICurrentEdition` / `ICurrentTenant` / `ICommerceContextAssigner`
- `HostNormalizer` (neutral host-header normalization primitive)
- `IDatabaseConnectionResolver` interface
- Foundation error codes used on resolution failures

## Decision

Do **not** move HTTP middleware or HttpCommerceContextAccessor into StoreContext. Host remains the HTTP composition edge; StoreContext remains contracts + neutral store-commerce accessor.

## Dependency audit (MultiTenancy file)

| Dependency | Classification |
|---|---|
| ASP.NET Core middleware / HttpContext | Host platform |
| BuildingBlocks Presentation / Errors / CommerceContext / HostNormalizer / IDatabaseConnectionResolver | BuildingBlocks neutral |
| `IStoreCommerceContextAssigner` / `StoreCommerceContext` | StoreContext.Contracts — allowed |
| `ControlPlaneRegistry` / `TenantRecord` | Host Configuration platform |
| Module Application/Infrastructure/Domain/DbContext | ZERO |

Business authority embedded: NONE — pure platform routing/context population.
