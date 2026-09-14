# Runtime A–I

Host `:5088` (recycled after T010 build) + FE `:3000`. Store `tenant:store-alpha` / `store-alpha`. Locale `fa`. Script `t010-runtime.mjs`. Raw: `runtime-raw.json` (`ok: true`, 10/10).

| Step | Result |
| --- | --- |
| A | `/fa` 200; home-selection `homePageId=null`, `usesCanonicalHome=true` |
| B | POST `/v1/admin/pages` slug `summer-sale` Draft |
| C | public API + `/fa/summer-sale` 404 while Draft |
| D | Published → API 200 + `/fa/summer-sale` 200 title shell |
| E | PUT home → `homePageId` set |
| F | Home identity via `/v1/storefront/home-selection`; `/fa` still canonical Home UI (no landing testid) |
| G | slug `cart` → 400 `landing.slug.reserved`; `/fa/cart` stays cart |
| H | foreign PageId → 404 `landing.page.missing` |
| I | clear home + unpublish; public 404; `usesCanonicalHome=true` |

Actor `01a036c2-970e-7000-8eb7-94bf5cc2d8db`. Temporary page unpublished; home cleared.
