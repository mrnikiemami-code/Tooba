# di-lifetime — TB-TMAR-HOST-MULTITENANCY-AMC-001-W1

## Preserved Program registrations (Scoped)

- `HttpCommerceContextAccessor`
- `ICurrentCommerceContext` → accessor
- `ICurrentEdition` → accessor
- `ICurrentTenant` → accessor
- `ICommerceContextAssigner` → accessor
- `app.UseMiddleware<TenantResolutionMiddleware>()`

## RequestServices removal

| Before | After |
|---|---|
| `httpContext.RequestServices.GetRequiredService<IStoreCommerceContextAssigner>()` | `InvokeAsync(HttpContext, IStoreCommerceContextAssigner storeCommerceAssigner)` |

- RequestServices in MultiTenancy folder = **ZERO**
- IServiceProvider injection = **ZERO**
- No custom factory
- No scoped service in middleware constructor
- Conventional middleware lifetime preserved; scoped assigner resolved per request via InvokeAsync parameter

## Context assignment order (HTTP success)

1. Resolve CommerceContext + StoreCommerceContext
2. Write CommerceContext to HttpContext.Items
3. `storeCommerceAssigner.Assign(storeCommerce)`
4. Activity tags
5. BeginScope
6. `_next`
