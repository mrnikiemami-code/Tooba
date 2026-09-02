# home-locale

`GET /v1/storefront/home?locale=` maps fa/en/fa-IR/en-US via `ContentTaxonomySeoRules.ResolveContentLocale`.
`StorefrontComposer.GetHomeAsync` passes resolved locale into `BuildLatestArticlesAsync` (no hard-coded fa-IR when locale known).
FE home (`app/page.tsx`) calls `loadStorefrontHome(contentLocale)`.
Empty locale rail uses empty state — no cross-language article fallback.
