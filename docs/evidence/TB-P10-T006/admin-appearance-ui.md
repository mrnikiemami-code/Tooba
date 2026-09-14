# TB-P10-T006 — Admin appearance UI

Surface: `/admin/settings` tab `admin-settings-tab-appearance`.

| Control | testid / behavior |
| --- | --- |
| Form | `admin-settings-appearance-form` |
| Preset cards | `admin-settings-appearance-preset-{key}` + `data-selected` |
| Preview | `admin-settings-appearance-preview` — CTA, link, card, muted, status chips |
| Save | `admin-settings-save-appearance` disabled unless dirty |
| Cancel | `admin-settings-cancel-appearance` restores saved PaletteKey |
| Error | `admin-settings-appearance-error` |
| Unknown key | `admin-settings-appearance-unknown` |
| Loading | shared settings `busy` / page load |

Preview style comes from `appearancePreviewStyle` → `appearanceCssVars(resolveBrandTokens(draft))` (same registry as Storefront). Preview is local until Save. No `type="color"`. ThemeMode not editable. Status chips use `bg-success` / `bg-warning` / `bg-danger`.
