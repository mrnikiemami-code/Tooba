# TB-P10-T006 — Persistence API

`GET/PUT /v1/admin/settings/appearance`

Write body: `{ paletteKey }` only. Invalid / unknown / hex / empty → `400 appearance.palette.invalid`. No color hex persisted.

`StoreAppearanceSettingsComposer.SaveAsync`:

- `IsKnown` then `ResolveKey`
- upsert singleton `StoreAppearanceSettings`
- `Replace(canonical, row.ThemeMode, now)` — ThemeMode preserved
- `SaveChanges`
- `Invalidate` current commerce scope only
- return authoritative view + curated presets

Existing stores with no row stay `tooba-blue` on GET. Unknown persisted key reads as `tooba-blue` with `paletteKeyWasKnown=false`.
