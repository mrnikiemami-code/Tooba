# TB-P10-T005 — Appearance architecture

Canonical owner: `StoreAppearanceSettings` (Catalog schema, singleton per Store database — same pattern as `StoreCheckoutIdentitySettings`).

Persisted now:

- `PaletteKey` (curated; default `tooba-blue`)
- `ThemeMode` (`Light` applied; `Dark` reserved, not painted on storefront)
- `UpdatedAt`

Not persisted: ProductCardSkin, token JSON blobs, executable CSS.

Projection: `StoreAppearanceProjector` → `GET /v1/storefront/appearance`.

Scope key:

- Single-store: `tenant:{TenantId}`
- Marketplace: `marketplace:{ConnectionReference}`

Marketplace and Single-store share the same contract. Cache is per scope. Store A cannot read Store B.

Future fields (HomePageId, skins, custom tokens) stay documented, not added.
