# TB-P07-T020 evidence

## Reference inspected

Visual contract collage (aligned hierarchy — not pixel-perfect):

`D:\Users\User\source\repos\SarvNewVerRequirment\reference\Image\ChatGPT Image Aug 29, 2026, 05_39_35 AM.png`

Hierarchy cues used: clear workspace headers, status badges, horizontal tabs, card sections, shell Save/Cancel.

## Alignment changes

- **Edit across tabs:** Enter edit stays on current tab; leaving dirty General confirms discard of general draft only and keeps `formMode.mode === "edit"` so other panels get `mode="edit"`.
- **readOnly:** `readOnly={viewScope}` only — normal VIEW no longer shows «اجازه نیست».
- **Shell actions:** General edit → Save + Cancel; other edit tabs → «پایان ویرایش»; VIEW → Edit + Publish/Restore. In-card General Save/Cancel removed.
- **Header:** Mode badge «مشاهده» / «ویرایش»; subtitle prefers `categoryPath`; activity/audit actors use `item.actor` or «سیستم».
- **Translations:** Removed deferred «تسک بعدی» copy; fa-IR helper + locale completion chips.
- **WorkspaceShell:** Desktop tablist horizontal scroll (`flex-nowrap` + `overflow-x-auto` + `shrink-0`).
- **List:** Design-system `Button`; create Cancel; IRR column label ریال.
- **Publishing / Media / SEO / Attributes / History:** Date locale, accessible readiness badges, temporary media CTA + `<details>` advanced GUID, token SERP/link colors, history hierarchy polish.
- **host-client:** JSDoc terminology گونه → تنوع.

## Explicit non-claims

- `USER_VISUAL_ACCEPTED` = NO
- No AppDataGrid redesign; no Media DAM / Offer / Price / Stock invention
- Not pixel-perfect to collage

## PDP / storefront regression (Host)

- Published product detail: `GET /v1/storefront/products/schema-mobile-demo-phone` → 200 with localized title, seoTitle/seoDescription, variant axes identity
- Missing slug → 404
- Product Admin list/workspace: `http://localhost:3000/fa/admin/products` → 200
- Shopeiva structure not redesigned; integration via existing Storefront composer only
- Note: curl against locale-prefixed PDP may see middleware 308 + rewrite headers; browser rewrite path `/fa/products/{slug}` → internal `/products/{slug}` remains the canonical contract
