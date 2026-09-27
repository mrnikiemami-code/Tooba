# W17 — Response-contract map

## By route

| Route | HTTP response | Shape | Catalog-only? | Post-write Get? |
|---|---|---|---|---|
| GET `/` | 200 | `AdminProductListItem[]` | NO (Offer/Price/Inv) | n/a |
| GET `/brand-options` | 200 | `AdminBrandOption[]` | YES | n/a |
| POST `/query` | 200 | `GridPageResponse<AdminProductListItem>` | NO | n/a |
| POST `/` | 201 | `ProductWorkspaceView` | NO | YES |
| GET `/{id}` | 200 / 404 | `ProductWorkspaceView` / `{title,errorCode}` | NO | n/a |
| PATCH `.../catalog-title` | 200 | `ProductWorkspaceView` | NO | YES |
| PATCH `.../core` | 200 | `ProductWorkspaceView` | NO | YES |
| PATCH `.../quantity-policy` | 200 | `ProductWorkspaceView` | NO | YES |
| PUT `.../category` | 200 | `ProductWorkspaceView` | NO | YES |
| POST `.../categories/additional` | 200 | `ProductWorkspaceView` | NO | YES |
| DELETE `.../categories/additional/{id}` | 200 | `ProductWorkspaceView` | NO | YES |
| PUT `.../brand` | 200 | `ProductWorkspaceView` | NO | YES |
| POST `.../publish` | 200 | `ProductWorkspaceView` | NO | YES |
| POST `.../unpublish` | 200 | `ProductWorkspaceView` | NO | YES |
| POST `.../archive` | 200 | `ProductWorkspaceView` | NO | YES |
| POST `.../restore` | 200 | `ProductWorkspaceView` | NO | YES |
| DELETE `/{id}` | 204 / 4xx | **NoContent** (or error JSON) | write Catalog; Offer check Contracts | NO |
| POST `.../variants` | 201 | `ProductWorkspaceView` | NO | YES |
| PATCH `.../variants/{id}` | 200 | `ProductWorkspaceView` | NO | YES |

**Full-workspace post-write route count: 15** (all writes except DELETE product).

## Moving write-only into Catalog — required strategy

For every route returning `ProductWorkspaceView`, moving **only** the write into Catalog would force one of:

| Option | Verdict |
|---|---|
| **A.** Catalog composes Offer/Pricing/Inventory/Tax/Party for response | **FORBIDDEN** |
| **B.** Catalog command succeeds; lawful composition owner re-reads aggregate for HTTP body | **REQUIRED** |
| **C.** Change HTTP response to non-aggregate / NoContent without Architect decision | **NOT ALLOWED** |

W17 selects **B** for all 15 post-write aggregate routes.

DELETE product already returns NoContent → Catalog may own HTTP write + Offer.Contracts reference check without post-write composition (**not A**).

## ProductWorkspaceView foreign fields (prove cross-module)

From `ProductWorkspaceModels.cs` + `GetAsync` composition:

- `Offers` / `Prices` / `TaxClassifications` / `Stock` ← Offer + Pricing + Tax + Inventory Contracts (+ Party display names)
- Catalog fields: identity, attributes, variants, media refs, SEO seams, publication Catalog checks, Activity/Audit, quantity policy, category assignments
- `Publication.PurchasableHint` mixes Catalog status with Offer/Price/Stock presence

Therefore aggregate HTTP ownership cannot be Catalog alone.
