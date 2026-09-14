# TB-P10-T009 — Admin skin UI

Appearance tab on existing Admin Settings:

- 4 skin options (`admin-settings-appearance-skins`)
- selected/current via `data-selected`
- dirty includes PaletteKey + ThemeMode + ProductCardSkin
- Cancel restores all three drafts
- one PUT `{ paletteKey, themeMode, productCardSkin }`
- preview uses `resolveProductCardSkinChrome` (same registry as storefront)
- no free-form color/HTML/CSS controls
