# CORRECTION-ADDENDUM — TB-282-R5

## R4 proof strength corrections

- R4 mounted Story workspace proved shared edit Dialog **opens** and destination label renders; it did **not** capture the owning `update_item` PUT, media retention, or malformed-200 retention. Those are R5 mount facts.
- R4 `FakePublishedSearch` / SamplerUsed assertions prove adapter **delegation**, not `CatalogPublishedProductSearchLookup` filtering/page/count. R5 adds the owning Catalog query test.

## Architecture / i18n retained

- Title hydration uses existing `ICatalogAdminProductTitleIdLookup` from Catalog.Contracts (no Story→Catalog.Infrastructure join).
- Additive nullable DTO field only; ZERO schema.
- fa/en `storyCopy` catalogs and VendorShell/`story.view` unchanged; no parallel seller panel or grid rewrite.
