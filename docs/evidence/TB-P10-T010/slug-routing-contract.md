# Slug + routing contract

- Normalize: trim, lower, hyphenate; strip `/` `\` and traversal.
- Unique per Store catalog + Locale + Slug (unique index + composer 409 `landing.slug.duplicate`).
- Reserved first segments centralized in `StoreLandingPageSlug.Reserved` and FE `RESERVED_LANDING_SLUGS` (cart, checkout, account, admin, api, products, product, categories, category, login, auth, v1, …).
- Reserved create → 400 `landing.slug.reserved`.
- Precedence: static Next routes / system prefixes → dynamic `[slug]` → 404.
- `isStorefrontLandingPath` allows locale middleware for `/summer-sale` without treating `/admin` as storefront (excluded first).
