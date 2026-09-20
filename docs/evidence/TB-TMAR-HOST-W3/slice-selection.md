# Slice selection — TB-TMAR-HOST-W3

Selected: **StoreAppearanceSettingsComposer** write path.

| Field | Detail |
| --- | --- |
| Host file/member | `Admin/StoreAppearanceSettingsComposer.SaveAsync` |
| Owner | Catalog (transitional StorefrontSettings debt class) |
| DbContext | CatalogDbContext → StoreAppearanceSettings |
| Transaction | Single SaveChangesAsync; no BeginTransaction |
| Callers | `GET/PUT /v1/admin/settings/appearance` |
| Target | SaveStoreAppearanceSettingsCommand → Handler → IStoreAppearanceSettingsDirectory |
| Baseline | Remove `Admin/StoreAppearanceSettingsComposer.cs` from host-write baseline |
| Why | W2 deferred Appearance next; Catalog-only; same Landing/Menu pattern; existing StoreAppearanceAdminTests |

Rejected: HoldPolicy (multi-BC), ProductWorkspace (giant), Checkout/Order composers, ShippingService (later module wave).
