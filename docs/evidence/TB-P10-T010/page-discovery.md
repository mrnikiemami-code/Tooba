# Page discovery

Existing surfaces inspected (read-only):

- **PageComposition / PageDefinition / PageSection / PageKeys.Home** — Home *section* catalog/order/visibility. Not locale+slug landing pages. Do not extend with a section builder.
- **ContentArticle** — blog/CMS; unique slug+locale; not a landing page.
- **StoreAppearanceSettings** — per Catalog/tenant singleton. Natural HomePageId owner (same isolation as palette/theme/skin).
- **Frontend locale** — `planLocaleMiddleware`; static app routes win over `app/[slug]/page.tsx`.
- **Public prefixes** — `src/frontend/lib/i18n/routing.ts`; admin/customer-panel/v1 excluded first.
- **SEO** — `canonicalForLocale` already used on storefront routes.
- **Admin auth** — `AdminPanelAccess.RequireAuthorizedAsync` (same as appearance).

Decision: new `StoreLandingPage` aggregate in Catalog. Do not reuse ContentArticle or PageComposition.
