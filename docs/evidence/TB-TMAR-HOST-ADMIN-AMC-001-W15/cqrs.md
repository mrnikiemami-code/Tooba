# CQRS — W15 Product History read

| Request | Handler | Port | Endpoint |
|---|---|---|---|
| `GetProductHistoryQuery` | `GetProductHistoryHandler` | `IProductHistoryReader.ListAsync` | `GET .../history` |

Flow:

```
HTTP → ICatalogAdminAuthorizer → ISender → GetProductHistoryQuery
  → IProductHistoryReader → Result<ProductHistoryPageView> → ApiResponseFactory
```

Endpoint does **not** inject Composer, ICatalogDirectory, IProductHistoryReader, or CatalogDbContext.

No Commands in W15 (read-only).
No actor binding on GET.
