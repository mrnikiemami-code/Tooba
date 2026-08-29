# TB-P07-T022 — Deferred gaps

## Hidden / deferred Category Workspace tabs

| Concept | Decision | Reason |
|---------|----------|--------|
| SEO tab | Hidden from Category tabs | SEO title/description/keywords already live inside **ترجمه‌ها**. A separate SEO tab would duplicate or invent a parallel Category SEO subsystem. |
| Settings tab | Hidden | No distinct Category settings domain beyond general/core fields already on **عمومی**. |
| History tab | Hidden | No clean reusable Category audit timeline without inventing event presentation architecture. |

These are **not** shown as به‌زودی stubs. Visible = functional.

## Progressive placeholders retained (out of tab nav)

- Category media pickers still show progressive «انتخاب رسانه — به‌زودی» (media upload not in this task scope).

## Mega-menu standalone nav route

- No dedicated `/admin/catalog/mega-menu` route exists; mega-menu remains a Category Workspace tab.

## Add-product nav item

- Product creation remains on `/admin/products` create panel; no separate add-product route to list under Products group.
