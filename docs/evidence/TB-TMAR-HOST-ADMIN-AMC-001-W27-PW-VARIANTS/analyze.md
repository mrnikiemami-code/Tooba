# Analyze — W27 ProductWorkspace variants (W17-plan W22)

## Target
Host Admin ProductWorkspace variant routes:
- `POST /v1/admin/products/{productId}/variants` → 201 + `ProductWorkspaceView`
- `PATCH /v1/admin/products/{productId}/variants/{variantId}` → 200 + `ProductWorkspaceView`

## Ownership
| Concern | Owner |
|---|---|
| Mutation | Catalog `Variants` Commands + `IProductVariantDirectory` |
| HTTP + post-write composition | ProductWorkspace.Endpoints + `GetProductWorkspaceQuery` |
| Auth / CanEditCatalog | `IProductWorkspaceAdminAuthorizer` + permissions |

## Disposition
| Member | Action |
|---|---|
| Host MapPost/MapPatch variants + handlers | MOVE |
| Composer CreateVariant/PatchVariant | DELETE |
| Host AdminProductVariant* models | MOVE → Catalog.Application.Variants.Models |
| Host PW file trio | RETAIN_PARTIAL |
| Admin count | **31 unchanged** |
| StoreAppearance / delete / core writes | OUT |

## Codes preserved
`workspace.variant.axes.missing`, `workspace.variant.create.rejected`, `workspace.variant.missing`, `workspace.variant.status.invalid`, `workspace.permission.denied`
