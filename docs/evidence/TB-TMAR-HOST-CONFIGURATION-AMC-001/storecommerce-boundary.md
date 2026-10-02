# storecommerce-boundary — TB-TMAR-HOST-CONFIGURATION-AMC-001

## Ownership

| Layer | Role |
| --- | --- |
| Host Configuration | Binds raw `StoreCommerceOptions`; builds `StoreCommerceContext` in BuildRegistry |
| StoreContext.Contracts | Owns `StoreCommerceContext` immutable contract type |
| Modules | Consume via StoreContext assigners; must not invent commercial defaults |

Import: `Tooba.StoreContext.Contracts.Current` only — **Contracts-only**. No StoreContext Application/Infrastructure/Domain.

## ResolveStoreCommerce

- Market: explicit raw Market else `DefaultMarketReference` fallback (tenant path)
- Currency / SalesChannel: explicit only (no invented defaults)
- Normalize = trim; blank → null (fail-closed consumers)

## Production fail-fast

Marketplace: deployment `DeploymentStoreCommerce` complete.  
SingleStore: each **Active** tenant StoreCommerce complete + SalesChannel enum-parseable.
