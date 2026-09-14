# TB-P10-T006 — Runtime A–O

Host `:5088` recycled to load Admin appearance endpoints (PID 35708). FE `:3000` unchanged. Actor `01a036c2-970e-7000-8eb7-94bf5cc2d8db` via `GET /v1/admin/dev-context`. No manual DB write.

| Step | Proof |
| --- | --- |
| A open Appearance | `GET /v1/admin/settings/appearance` 200, 7 presets |
| B current tooba-blue | `paletteKey=tooba-blue`, tokens `37 99 235` |
| C choose alternate | preview uses registry (`forest-green` → `21 128 61`) |
| D preview before save | local CSS vars; storefront still tooba-blue until PUT |
| E Cancel | `onCancelAppearance` resets draft to saved key (source + helper test) |
| F choose again | draft `forest-green` |
| G Save | one PUT `{ paletteKey: "forest-green" }` |
| H one successful write | PUT 200 once for the alternate |
| I Storefront fresh | `GET /v1/storefront/appearance` + `/fa` |
| J SSR marker | `data-storefront-palette="forest-green"` scope `tenant:store-alpha` |
| K tokenized surfaces | `--color-primary:21 128 61` on first HTML |
| L status colors | danger not in appearance style; `globals.css` unchanged |
| M Admin persist | GET appearance still `forest-green` |
| N restore | PUT `tooba-blue` 200 |
| O fresh Storefront | marker `tooba-blue`, `--color-primary:37 99 235` |

Also: PUT without actor = 401 `admin.actor.missing`. PUT `#ff00aa` = 400 `appearance.palette.invalid`. ThemeMode stayed `Light`.
