# analyze — TB-TMAR-HOST-MULTITENANCY-AMC-001

## Mode

ANALYSIS_ONLY — production change ZERO.

## Exact tree

```text
src/backend/Host/Tooba.Host/MultiTenancy/
  TenantResolutionMiddleware.cs
```

| Metric | Value |
|---|---|
| Production `.cs` count | 1 |
| Production types | 2 |
| Namespace (current) | `Tooba.Host` |
| Path-derived namespace | `Tooba.Host.MultiTenancy` |
| Path↔namespace | VIOLATION |

## Types

| Type | Visibility | Interfaces |
|---|---|---|
| `HttpCommerceContextAccessor` | internal sealed | `ICurrentCommerceContext`, `ICurrentEdition`, `ICurrentTenant`, `ICommerceContextAssigner` |
| `TenantResolutionMiddleware` | internal sealed | ASP.NET middleware |

## Verdict (summary)

- Host retains HTTP host→tenant resolution middleware and HttpContext-backed commerce accessor.
- StoreContext owns store-commerce contracts + non-HTTP store commerce accessor; MultiTenancy only assigns via `IStoreCommerceContextAssigner`.
- Co-located types MUST_SPLIT; namespace MUST become `Tooba.Host.MultiTenancy`.
- RequestServices lookup for scoped assigner is lifetime-required, not free-form service locator debt.
- Fail-closed / Foundation presentation path from Errors W1 is CURRENT and NOT MultiTenancy certification.
- Recommended: W1 migrate (split + namespace + optional constructor clarity) → W2-CERT.
- Protected: HOST_ERRORS / HOST_SECURITY / HOST_ADMIN certifications PRESERVED.
- Implementation SHA unchanged: `e190e213c491fd530d86c7e5680cb0607b5e98d3`
