# Storefront Pages / Home / Landing Extension

Status: CONTRACT ONLY — TB-P10-T005. No Page tables, routes, or composer UI.

## Future model

A Store may own many Landing Pages. Each Page is a database row, not a Next.js file:

- Store ownership
- Locale
- Title
- Slug
- publish state
- SEO metadata
- TemplateKey
- approved `PageSection` composition (`SectionType` registry, `SortOrder`, enabled, controlled config)

Product/data sources allowed on sections: Manual / Category / Brand / Newest / BestSelling / Featured / Discounted. Authors never write SQL.

Menu/MenuItem remain an independent tree. A Page or Section may reference an approved Menu or MenuGroup.

`Store.HomePageId` (or equivalent) selects exactly one Home Page.

Resolution is `Store + Locale + Slug` at runtime. Adding or publishing a page must not add a physical App Router file and must not require a Storefront redeploy.

Reserved routes cannot be shadowed: system, product, category, cart, checkout, account, admin, API.

Future slug history / 301 and cache versioning remain extension points.

## Future Admin (not built)

- Appearance: palette preset, theme mode, product-card skin, preview
- Pages: list, create/edit, slug/SEO, section composer, preview, publish, choose Home
- Menus: Menu/MenuItem management and Page/Section reference pickers

Permissions stay backend-authoritative. No placeholder Admin chrome in T005.
