# Host write inventory — TB-TMAR-HOST-W3

Live scan of `Tooba.Host` for SaveChanges*/BeginTransaction (excl. tests/obj/bin).

## Selected for migration
- `Admin/StoreAppearanceSettingsComposer.SaveAsync` — CatalogDbContext singleton StoreAppearanceSettings (DIRECT_DB_WRITE + validation decisions)

## Other production DIRECT_DB_WRITE (deferred)
Quantity/UoM/CheckoutAbuse/CheckoutIdentity/ReservationPolicy, ShippingService, SellerPanel, ProductWorkspace (too large), HoldPolicy (dual-context), Order/Checkout storefront composers.

## FALSE_POSITIVE / composition
Grid engines, Program.cs, MerchandisingCampaignAdminEndpoints (directory-backed), read composers.

See `host-write-inventory.json`.
