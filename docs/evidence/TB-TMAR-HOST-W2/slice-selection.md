# TB-TMAR-HOST-W2 Slice Selection

## Selected slice
`Admin/StoreMenuComposer.cs` — Store Menu CRUD + header menu reference writes.

## Exact write methods
- CreateAsync
- UpdateAsync
- SetEnabledAsync
- DeleteAsync
- AddItemAsync
- UpdateItemAsync
- SetItemEnabledAsync
- DeleteItemAsync
- ReorderItemsAsync
- SetHeaderAsync

## Owning DbContext / module
- `CatalogDbContext` (`catalog` schema)
- Transitional owning module: Catalog (same debt class as StoreLandingPage*)

## Write operations
- Add/Remove StoreMenus, StoreMenuItems
- Update menu/item fields
- SaveChangesAsync (10 call sites; no BeginTransaction)
- SetHeader writes StoreAppearanceSettings.HeaderMenuId

## Transaction behavior
- No explicit Host transaction today
- Single SaveChanges per use-case (atomicity per operation preserved in Directory)

## Why this outranks remaining candidates
1. Wave-1 candidate listed with Landing; Landing already migrated.
2. Highest SaveChanges count among remaining coherent Catalog storefront-settings composers (10 vs Appearance=1).
3. Isolated Catalog-only DbContext (not multi-module HoldPolicy/Payment coupling).
4. Clear capability boundary (menus + header selection).
5. Existing focused tests (`StoreMenuComposerT013Tests`).
6. Moderate regression radius (Admin menus + header projection cache invalidation).

## Why safe enough now
- Same Stage-A pattern as HOST-W1/R1
- No new App→App edges
- No Domain ownership move required for migration
- No BeginTransaction complexity

## Ownership caveat
`StoreMenu*` / header settings currently in Catalog.Domain — potentially StorefrontSettings/PageComposition later. Keep transitional Catalog ownership; document for Domain Ownership wave. Do not move Domain types in this task.

## Rejected for this wave (examples)
- StoreAppearanceSettingsComposer: smaller debt (1 SaveChanges); lower priority than Menu CRUD volume
- HoldPolicy/Checkout settings: cross-module Catalog+Payment coupling → Contracts risk
- Quantity/UoM endpoints: settings slices, lower extraction risk vs Menu tree writes
