# Endpoint ownership — AccessControl (W3)

## Ownership

| Aspect | State |
| --- | --- |
| HTTP ownership | `MODULE_OWNED` |
| Host-owned AccessControl routes | `ZERO` |
| Module composition entry | `Endpoints/AccessControlEndpointModule.cs` |
| Duplicate route mapping | `ZERO` |
| Endpoint direct persistence / directory access | `ZERO` |
| `ISender` dispatch references | 60 |
| Ad-hoc `Results.Json` / `Results.BadRequest` / `Results.Problem` | `ZERO` |
| Local `ProblemDetails` builder | `ZERO` |
| Endpoint `catch`-and-map for expected failures | `ZERO` |
| `AccessControlException` referenced from Endpoints | `ZERO` |

## Audience / capability folders

```text
Endpoints/
  AccessControlEndpointModule.cs   (composition only)
  Admin/    AccessControlAdminEndpoints.cs         AccessControlAdminSellerEndpoints.cs
  Seller/   AccessControlSellerEndpoints.cs        Development/SellerDevContextEndpoints.cs
  Errors/   AccessControlErrorCatalogContributor.cs  AccessControlHttpErrors.cs
  Resources/ AccessControlErrorResources.cs  AccessControlErrors.resx  AccessControlErrors.fa.resx
```

No capability `*Endpoints.cs` file sits at the Endpoints root.

## Route inventory

### Admin surface (`/admin/access…` group)

| # | Method | Route |
| --- | --- | --- |
| 1 | POST | `/bootstrap` |
| 2 | GET | `/me/capabilities` |
| 3 | GET | `/permissions` |
| 4 | GET | `/roles` |
| 5 | GET | `/roles/{roleId:guid}` |
| 6 | POST | `/roles` |
| 7 | PUT | `/roles/{roleId:guid}` |
| 8 | POST | `/roles/{roleId:guid}/clone` |
| 9 | DELETE | `/roles/{roleId:guid}` |
| 10 | GET | `/roles/{roleId:guid}/permissions` |
| 11 | PUT | `/roles/{roleId:guid}/permissions` |
| 12 | GET | `/assignments` |
| 13 | POST | `/assignments` |
| 14 | DELETE | `/assignments/{assignmentId:guid}` |
| 15 | GET | `/users/{userId:guid}/effective` |
| 16 | GET | `/users` |
| 17 | GET | `/categories` |
| 18 | GET | `/brands` |
| 19 | GET | `/products` |
| 20 | GET | `/warehouses` |
| 21 | GET | `/stores` |
| 22 | GET | `/order-segments` |

### Admin-over-seller surface

| # | Method | Route |
| --- | --- | --- |
| 23 | GET | `/roles` |
| 24 | POST | `/roles` |
| 25 | PUT | `/roles/{roleId:guid}` |
| 26 | POST | `/roles/{roleId:guid}/clone` |
| 27 | DELETE | `/roles/{roleId:guid}` |
| 28 | GET | `/roles/{roleId:guid}/permissions` |
| 29 | PUT | `/roles/{roleId:guid}/permissions` |
| 30 | GET | `/ceiling` |
| 31 | PUT | `/ceiling` |
| 32 | GET | `/assignments` |
| 33 | POST | `/assignments` |
| 34 | DELETE | `/assignments/{assignmentId:guid}` |
| 35 | GET | `/users/{userId:guid}/effective` |

### Seller surface

| # | Method | Route |
| --- | --- | --- |
| 36 | GET | `/me/capabilities` |
| 37 | GET | `/permissions` |
| 38 | GET | `/roles` |
| 39 | POST | `/roles` |
| 40 | GET | `/roles/{roleId:guid}` |
| 41 | PUT | `/roles/{roleId:guid}` |
| 42 | POST | `/roles/{roleId:guid}/clone` |
| 43 | DELETE | `/roles/{roleId:guid}` |
| 44 | GET | `/roles/{roleId:guid}/permissions` |
| 45 | PUT | `/roles/{roleId:guid}/permissions` |
| 46 | GET | `/ceiling` |
| 47 | GET | `/assignments` |
| 48 | POST | `/assignments` |
| 49 | DELETE | `/assignments/{assignmentId:guid}` |
| 50 | GET | `/users/{userId:guid}/effective` |
| 51 | GET | `/users` |
| 52 | GET | `/categories` |
| 53 | GET | `/brands` |
| 54 | GET | `/products` |
| 55 | GET | `/warehouses` |
| 56 | GET | `/stores` |
| 57 | GET | `/order-segments` |

### Seller development surface

| # | Method | Route |
| --- | --- | --- |
| 58 | GET | `/dev-contexts` |

## Notes

- 58 `Map*` registrations across 4 files, all inside the module-owned endpoint module.
- The `/categories`, `/brands`, `/products`, `/warehouses`, `/stores`, `/order-segments` scope-resource
  lookups delegate to `ListScopeResourcesQuery` via `ISender` (see `cqrs-request-matrix.md`); they are
  reachability carriers for that query, not separate Host-owned routes.
- `Endpoints/Errors/AccessControlHttpErrors.cs` (17 LOC) is **not referenced by production code**
  (only by a negative guard assertion). It is recorded as non-blocking residual debt, not as a
  parallel mapping path in use. See `residual-debt.md`.
- `Host/Tooba.Host/AccessControl/AccessControlEndpoints.cs` (984 LOC in the size baseline) **does not
  exist** on disk — Host HTTP ownership is already `ZERO`.
