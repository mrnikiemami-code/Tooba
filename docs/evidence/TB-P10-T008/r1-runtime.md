# TB-P10-T008-R1 — Runtime A–I

Host `:5088` rebuilt after `PrimaryOnDarkRgb`. FE `:3000`. Actor `01a036c2-970e-7000-8eb7-94bf5cc2d8db`. Raw: `r1-runtime-raw.json` (`ok: true`).

| Step | Proof |
| --- | --- |
| A wine-burgundy + DarkOnly | PUT 200 `paletteKey=wine-burgundy` `themeMode=DarkOnly` `primaryOnDarkRgb=189 91 118` |
| B Home / PDP / Cart / Shipping | all `scheme=dark` `class=dark` `themeMode=DarkOnly` |
| C brand-text contrast | Home `--color-primary=159 18 57` (CTA) `--color-primary-on-dark=189 91 118` (emphasis, 4.58:1 vs `12 12 14`) |
| D Product Card dark | Home HTML includes `data-storefront-media-well`; card chrome is `bg-surface` / `bg-surface-elevated/90` |
| E forest-green DarkOnly | `21 128 61` + on-dark `42 139 78` + dark class |
| F amber-gold DarkOnly | `180 83 9` + on-dark `187 98 31` + dark class |
| G violet-royal DarkOnly | `124 58 237` + on-dark `144 88 240` + dark class |
| H UserChoice dark persist | cookie dark → `scheme=dark`; cookie light → light |
| I restore | PUT tooba-blue LightOnly; Home light `37 99 235` |

Screenshots: `r1-home-wine-dark.png`, `r1-pdp-wine-dark.png`. CDP on Home: `class=dark` `themeMode=DarkOnly` `--color-background=12 12 14` `--color-primary-on-dark=189 91 118`. Restored tooba-blue LightOnly.
