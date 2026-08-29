# Product Variants (تنوع‌ها)

> **Task:** TB-P07-T013 · **Phase:** P07 Advanced Catalog
> **Depends on:** [PRODUCT-ATTRIBUTES.md](./PRODUCT-ATTRIBUTES.md), Category effective schema (`IsVariantAxis`)

## Product vs Variant

| Concept | Ownership |
|---------|-----------|
| **Product** | Canonical descriptive identity (category, product-level attributes, publish) |
| **CatalogVariant (تنوع)** | Canonical combination of variant-axis values under a Product |
| **Offer** | Seller listing against Product/Variant — owns Price / Inventory later |

**Product ≠ Offer.** Price and Stock are **not** stored on `CatalogVariant`.

## Axes

Source: effective Category schema where `IsVariantAxis == true`
(AND definition `IsVariantAxisAllowed`).

- Product Admin cannot invent local axes.
- Matrix generation prefers **Enumeration / option-backed** axes only.
- Free-text axes are rejected with a clear Persian error (architectural concern).

Empty state:

```text
برای این دسته‌بندی ویژگی تنوع تعریف نشده است.
```

## Uniqueness

Within a Product, one exact axis combination is unique via `CombinationFingerprint`
(`CatalogVariant.ComputeFingerprint` with option Guid `N` canonical form).

Backend is authoritative.

## Generation

- Cartesian of selected option IDs per axis
- Deterministic order: axis `DisplayOrder`, then option `DisplayOrder`
- Safe maximum: **200** combinations — preview sets `capped=true` + `warningFa`; apply rejects

## Reconciliation

Desired vs existing:

| Action | Behavior |
|--------|----------|
| Unchanged | Keep |
| New | `CreateVariant` |
| Removed | **Archive** (`SetStatus(Archived)`) — never hard-delete when Offer refs exist; prefer archive even without refs |

`ProductVariantAxes` synced to the applied axis definition set.

## Default / status / code

- `SortOrder`, `IsDefault` on `CatalogVariant`
- Exactly one default among non-archived when set
- Optional `CatalogCodeSeam` (catalog identity ≠ Seller Offer SKU)
- Status: Draft / Published / Archived (UI: فعال / غیرفعال / بایگانی‌شده)

## Offer handoff

Seller later: select Product → select Variant → create Offer → set Price / Inventory.

Reference safety via `IOfferLookupGateway.CountOffersByCatalogVariantIdsAsync` (Offer DbSet only; no Catalog→Offer SQL join).

## Category / schema safety

Category-change preview reports impacted variant counts and does **not** auto-delete variants.

## Readiness

`ProductVariantReadiness`: `IsValid`, `MissingAxes`, `InvalidVariants`, `DuplicateCombinations`, `NoDefaultVariant?`

## Admin UX

Product Workspace tab **تنوع‌ها**:

- VIEW: readable cards (labels, not raw IDs), status, default badge, code, offer count
- EDIT: axis multi-select, preview + impact summary, Save/Cancel/dirty, default/status/code
- No price/stock controls · no raw `AgGridReact`

## APIs

| Method | Path |
|--------|------|
| GET | `/v1/admin/catalog/products/{id}/variants/editor?locale=` |
| POST | `/v1/admin/catalog/products/{id}/variants/preview` |
| PUT | `/v1/admin/catalog/products/{id}/variants/apply` |
| GET | `/v1/admin/catalog/products/{id}/variants/readiness` |

Legacy create/patch variant workspace routes remain for compatibility.

## Migration

`20260829050000_AddCatalogVariantSortOrderAndDefault` — adds `sort_order`, `is_default` on `catalog.variants`.

## USER_VISUAL_ACCEPTED

```text
NO
```
