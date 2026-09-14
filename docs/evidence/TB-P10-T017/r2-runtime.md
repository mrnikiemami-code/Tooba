# Runtime — TB-P10-T017-R2

Host `:5088` + FE `:3000`. Capture: `r2-capture.mjs`. Report: `r2-runtime-report.json` `ok=true`.

| Step | Result |
| --- | --- |
| A cold PLP | 200 |
| B PLP media well | 240×299, `aspect-ratio: 4 / 5`, Tooba placeholder visible |
| C cold Landing | 200 |
| D Landing product cards | wells visible with Tooba placeholder |
| E Home product-card | wells height ≥ 80 |
| F four skins | classic/clean/elevated/glass wells ≥ 200 |
| G PaletteTint | `data-storefront-background-style=PaletteTint` during inspection |
| H restore | tooba-blue / LightOnly / classic / Neutral |

No 500. No product-code retry/polling.
