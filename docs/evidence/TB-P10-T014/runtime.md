# Runtime — TB-P10-T014

Host `:5088`, FE `:3000`. `capture.mjs` A–K PASS. `runtime-report.json` ok=true.

- A Appearance opened; 7 palettes / 4 themes / 4 skins visible
- B forest-green + DarkOnly + glass saved
- C published `landing-demo` 200
- D selected as Home
- E `/fa` inherited palette/theme/skin
- F header menu used منوی دموی فروشگاه (usesFallback=false)
- G Draft Admin preview 200
- H public `/landing-demo-draft` and storefront pages API 404
- I appearance restored tooba-blue / LightOnly / classic
- J canonical Home restored
- K demo pages + menu remain seeded

No render-loop. Cross-store Host header did not 500. Ending Header fallback restored. Screenshots in `docs/evidence/TB-P10-T014/screenshots/`.
