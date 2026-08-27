# 12 — Page composition locale proof (TB-P06-T016)

| Check | Result |
|---|---|
| `/fa` Home | Composition loaded with `localeToContentApi('fa')` → `fa-IR` |
| `/en` Home | Composition loaded with `en` |
| Single Home implementation | Same `app/page.tsx` + `StorefrontShopeivaHome` |
| Renderer reuse | Existing T015 `renderHomeSection` switch — no duplicate Home |
| Locale edition override | Admin composition `locale` scope still valid |
| Visual drift | Routing-only; no CSS/JS/carousel changes to section renderers |

Proof: `_locale-routing-api-proof.json` `faHome`/`enHome` = 200.
