# Behavior parity — TB-TMAR-HOST-DEVELOPMENT-PRODUCTWORKSPACE-CATALOG-SEED-REHOME-001

Every development value and ordering is preserved verbatim. Nothing was "simplified".

## Execution order (unchanged)

Host `ApplyCoreAsync(seedCatalog: true)` → same migration list (28 contexts) → then:

1. `IWorkspaceDemoSeed.IsLiveProductSeededAsync()` decides the branch, exactly matching the old
   `catalogDb.Products.AnyAsync(p => p.SlugSeam == "workspace-live-shirt")` check.
2. Existing-product branch: copy refresh → Admin R3 preview → module seeds (unchanged list/order).
3. New-product branch: product seed → module seeds (unchanged list/order) → Admin R3 preview.

`MigrateSchemaOnlyAsync` (`RunLegacyBootstraps=false`) still runs migrations only and never runs the
Catalog business seed — identical to the previous `seedCatalog: false` early return.

## Exact values pinned

### Catalog

| Item | Value |
| --- | --- |
| Product slug seam | `workspace-live-shirt` |
| Category hierarchy | `پوشاک` → `پوشاک مردانه` → `پیراهن مردانه` |
| Brand slug seam | `tooba-live` |
| Colour attribute code | `color` (enumeration, variant axis) |
| Black option code | `black` |
| Variant code seam | `LIVE-SHIRT-BLK` |
| Media ids / order | `aaaaaaaa…` (جلو), `bbbbbbbb…` (پشت), `cccccccc…` (یقه), `dddddddd…` (آستین) |
| SEO copy | `توضیح سئو پیراهن زنده Workspace` |
| Publish | category publish → product publish |
| R3 gallery | 5 images in order جلو/پشت/یقه/آستین/مانکن |
| R3 draft | `admin-r3-draft-scarf` + media `11111111-…aaa1` |
| R3 archived | `admin-r3-archived-hat`, cloned category, media `22222222-…bbb2`, publish → archive |

### Marketplace demo

| Item | Value |
| --- | --- |
| Seller A | `فروشگاه آرمان` / `Arman Store Legal` |
| Seller B | `دیجی‌استایل نمونه` / `Digistyle Sample Legal` |
| Seller SKUs | `ARM-LN-01`, `DGS-LN-01` |
| Offer channel | `SalesChannel.Marketplace` |
| Prices | 1,850,000 and 1,790,000 IRR, market `IR`, start `2026-01-01T00:00:00Z` |
| Tax category | `standard` / `استاندارد` |
| Tax rule | jurisdiction `IR-NAT`, market `IR`, kind Percentage, rate 0.09, specificity 10, override Disabled, activated |
| Inventory locations | `WH-THR`/`انبار مرکزی تهران`, `WH-ISF`/`انبار اصفهان`, `WH-KSH`/`انبار کاشان` |
| Stock | offer A @ THR 12, offer A @ ISF 7, offer B @ KSH 4, reason `seed-receipt` |
| Hold | offer A @ THR, qty 3, reference `workspace-live-hold`, idempotency key `workspace-live-hold` |

### Copy refresh legacy values

Catalog: `پیراهن Workspace زنده` / `Live Workspace Shirt` → product `پیراهن مردانه لینن` / `Men's Linen Shirt`.
Party: `فروشنده الف` / `Seller A` → `فروشگاه آرمان`; `فروشنده ب` / `Seller B` → `دیجی‌استایل نمونه`.

## Intentional behavior-equivalent idempotency strengthening

The former Host code wrote the demo data unconditionally in the new-product branch and relied on the
outer `AnyAsync(SlugSeam == …)` guard for the existing-product branch. Wave 1 keeps that exact outer
guard, and additionally makes the inner helpers reuse-safe:

| Operation | Former | Now | Equivalence |
| --- | --- | --- | --- |
| Seller organization | `CreateOrganizationAsync` (always create) | `EnsureDevelopmentOrganizationAsync` (find by display name, else create) | Identical on a fresh branch; stronger on replay |
| Media attach in R3 gallery | `AnyAsync` guard then attach | Same `AnyAsync` guard then attach | Identical |
| Location ensure | `CreateLocationAsync` (always create) | `EnsureDevelopmentLocationAsync` (find by code, else create) | Identical on a fresh branch; stronger on replay |
| Stock adjust | `OpenPosition` + `Adjust` | Same via `IncreaseDevelopmentStockAsync` | Identical |
| Hold | `ReserveAsync(stockA1, 3, ref, idem, null, ct)` | Same `ReserveAsync` through the Inventory gateway | Identical |
| Copy refresh writes | Unconditional `SaveChangesAsync` ×2 | `when (changed)` guards | Identical observable result |

No consumer of the removed Host types existed outside the file itself, so no external behavior changed.

## Non-parity notes

- The former Host code mutated `CatalogDbContext.LocalizedTexts` and `PartyDbContext.Parties` directly.
  Both are replaced by the owning module's Development mechanism; the persisted rows and values are the same.
- `ProductWorkspaceDevelopmentBootstrap.cs` still resolves the same 28 `DbContext`/migrator registrations
  for schema migration (Wave 2 debt, explicitly out of scope here).
