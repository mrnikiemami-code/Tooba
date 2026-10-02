# context-assignment — TB-TMAR-HOST-MULTITENANCY-AMC-001

## Dual storage

| Mechanism | Role |
|---|---|
| `HttpContext.Items[ItemKey]` | Request truth set by middleware |
| `_assigned` field on accessor | Worker/non-HTTP override via `ICommerceContextAssigner.Assign` |

`Current` priority: `_assigned ?? Items[ItemKey]`

## Invariant (recommended)

1. HTTP requests: middleware owns Items population; do not call Assign on the same request unless intentional test override.
2. Workers: Assign is the supported non-HTTP path (documented on accessor).
3. Scoped lifetime: one accessor instance per scope — `_assigned` does not leak across requests.
4. StoreCommerce assignment is separate (`IStoreCommerceContextAssigner.Assign`) and must stay paired with technical CommerceContext population on HTTP success path.

## RequestServices

`GetRequiredService<IStoreCommerceContextAssigner>()` inside `InvokeAsync` is **ACCEPTABLE_REQUEST_SCOPE_BRIDGE_REQUIRED_BY_MIDDLEWARE_LIFETIME**: conventional middleware is constructed once; scoped assigner cannot be constructor-injected safely.
