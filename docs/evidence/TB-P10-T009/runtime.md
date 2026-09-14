# TB-P10-T009 — Runtime A–N

Host `:5088` + FE `:3000`. Actor `01a036c2-970e-7000-8eb7-94bf5cc2d8db`. Raw: `runtime-raw.json` (`ok: true`, 15/15).

| Step | Proof |
| --- | --- |
| A default + tooba-blue + LightOnly + classic | PUT 200 |
| B Home / listing / PDP related / Cart | `data-storefront-product-card-skin=classic` |
| C Admin 4 skins, current classic | GET skins.length=4 |
| D invalid skin | 400 `appearance.skin.invalid` |
| E–F save clean | PUT clean 200 |
| G fresh storefront clean | Home/listing/PDP/Cart skin=clean |
| H DarkOnly | themeMode=DarkOnly, skin preserved |
| I clean + dark | Home dark class + clean |
| J forest-green | palette=forest-green, skin=clean |
| K clean + dark + forest | all three compose |
| L elevated light/dark | skin=elevated |
| M glass light/dark | skin=glass |
| N restore default | tooba-blue LightOnly classic, Home light |

Screenshots: `home-classic.png`, `home-clean.png`, `home-glass-dark.png`, `admin-skins.png`.
