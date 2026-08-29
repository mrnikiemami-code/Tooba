# TB-P07-T022 / T022-R1 — Deferred gaps

## Hidden / deferred Category Workspace tabs

| Concept | Decision | Reason |
|---------|----------|--------|
| SEO tab | Hidden from Category tabs | SEO title/description/keywords already live inside **ترجمه‌ها**. A separate SEO tab would duplicate or invent a parallel Category SEO subsystem. |
| Settings tab | Hidden | No distinct Category settings domain beyond general/core fields already on **عمومی**. |
| History tab | Hidden | No clean reusable Category audit timeline without inventing event presentation architecture. |

These are **not** shown as به‌زودی stubs. Visible = functional.

## Category media picker (T022-R1)

- Fake «انتخاب رسانه — به‌زودی» CTA **removed**.
- UI shows **read-only** connected/not-connected status for تصویر/آیکن only.
- Functional Media DAM picker deferred until existing Media contract can be reused safely (no new DAM).

## Mega-menu standalone nav route

- No dedicated `/admin/catalog/mega-menu` route exists; mega-menu remains a Category Workspace tab.

## Add-product nav item

- Product creation remains on `/admin/products` create panel; no separate add-product route to list under Products group.
