# Home selection contract

- `StoreAppearanceSettings.HomePageId` nullable, same Catalog/Store only.
- Eligible Home = Published page in the same catalog. Missing id → 404 `landing.page.missing`. Draft → 400 `landing.home.ineligible`.
- Unset / ineligible → `UsesCanonicalHome=true`; current Home UI (`StorefrontHomePage` / PageComposition) is unchanged.
- Public identity: `GET /v1/storefront/home-selection`. Admin: `GET/PUT /v1/admin/pages/home`.
- Unpublish of the selected Home clears the reference.

Later T011/T012 migration: when a composer exists, `/` may render the selected Published page instead of the current Home composition. T010 does not replace Home UI.
