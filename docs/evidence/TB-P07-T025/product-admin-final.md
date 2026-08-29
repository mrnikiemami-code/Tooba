# TB-P07-T025 — Product Admin final

## Routes checked

- `/fa/admin/products` (list)
- `/fa/admin/products/{id}?scope=edit` (EDIT)
- Tabs: General, Translations, Media (also available: Attributes, Variants, SEO, Publishing, History)
- `/en/admin/products` (HTTP 200)

## Locales

- fa: RTL shell + Product workspace
- en: route reachable (Admin chrome locale via resolveAdminChromeLocale)

## VIEW / EDIT

- EDIT confirmed live for `گوشی نمونه schema`
- Save / Cancel / End-edit present
- Dirty-state Dialog present

## Reference

- Opened: `SarvNewVerRequirment/reference/Image/ChatGPT Image Aug 29, 2026, 06_33_46 PM.png`
- Live side-by-side: yes (browser + reference)

## Composition vs reference

| Area | Live | Notes |
|------|------|-------|
| Header / breadcrumbs / status badges | present | Draft/Published + Edit badge |
| Summary cards | present | status, readiness, translations, SEO, variants, media |
| General | language-neutral | Category, Brand assign, global slug, catalog-code handoff; **no** fixed NameFa/NameEn |
| Translations | locale editor | fa/en buttons with ناقص/کامل; name/summary/full |
| Media | real gallery empty-state | primary/readiness messaging; no به‌زودی / dead DAM CTA |
| Price/Stock cards | absent | correctly not copied from reference |

## Remaining visible mismatch

- Reference mock shows Persian title/summary/description editors on General; Tooba intentionally keeps those in Translations (T024-R1 accepted).
- Reference shows dense side media/history inspectors; Tooba uses summary cards + dedicated tabs (accepted Product Admin composition).
- No material incompleteness / stub UX found in scoped Product Admin.

## Incomplete UX found

- none in scoped Product surfaces (no به‌زودی, no Coming soon, no fake GTIN/Model, no fixed English-name field)

## Architecture regression

- none: Product≠Offer; no Product.Price/Stock; Brand assign canonical; Variant CatalogCodeSeam; no fake identifiers

## Screenshots

- `screenshots/product-edit-general.png` (captured from live EDIT)
