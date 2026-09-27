# Analyze — W28 ProductWorkspace delete (W17-plan W23)

## Target
`DELETE /v1/admin/products/{productId}` — 204 hard-delete or 409 soft-archive when Offer references variants.

## Ownership (W17)
| Concern | Owner |
|---|---|
| Mutation + Offer.Contracts gate | Catalog Command + Infrastructure |
| HTTP 204/409 | **Catalog.Endpoints** (not ProductWorkspaceView) |
| Auth / edit scope | ICatalogAdminAuthorizer + CatalogWorkspaceScope |

## Disposition
| Member | Action |
|---|---|
| Host MapDelete product + DeleteAsync | MOVE → Catalog.Endpoints |
| Composer DeleteOrSoftArchiveAsync | DELETE |
| Host PW files | RETAIN_PARTIAL |
| Admin count | **31 unchanged** |

## Behavior
1. Missing product → `workspace.product.missing`
2. Any Offer on variant ids → Archive + `workspace.product.delete.referenced` (409)
3. Else cascade remove media/attrs/axes/categories/names/variant attrs/variants/product → 204
