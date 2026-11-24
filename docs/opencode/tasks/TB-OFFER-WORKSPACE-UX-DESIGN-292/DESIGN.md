# DESIGN — Offer Workspace UX (TB-292, design-only)

Mode: **Operate** (admin/seller task UI). Restraint + density + native expectations; brand in precise details.
Interactive prototype: `mockups/offer-workspace.html` (4 views, RTL/Persian). Design system reuse; no product code.

## 1) Admin offers table (`/admin/offers`)
- Use the standard `ServerGridPage` (same as `/admin/orders`), columns via `createOfferColumns`.
- KPI strip (کل/فعال/پیش‌نویس/ناموجود) — scannable summary.
- Toolbar: search · advanced filter (drawer) · column manager · saved views · Excel · CSV · «+ آفر جدید».
- Columns: آفر/محصول (thumb+title+brand+variant) · فروشنده · SKU · وضعیت (colored Persian badge) · قیمت (Jalali
  amount) · موجودی (onHand/available) · مسئولیت ارسال (فروشنده/پلتفرم) · آخرین تغییر (شمسی) · عملیات (ویرایش/قیمت/
  موجودی/انتشار — row actions).
- Server-side pagination; sticky header; row hover; dense rows. Status colors: فعال=سبز، پیش‌نویس=خاکستری،
  معلق=کهربایی، بایگانی/ناموجود=قرمز/خنثی.

## 2) Admin offer form (`/admin/offers/new`, `/admin/offers/{id}`)
Two-column: sections (main) + sticky summary/actions (side). Sections map 1:1 to domain owners (labelled):
فروشنده/محصول/گونه (Party+Catalog) · قیمت و تخفیف (Pricing) · موجودی و انبار (Inventory) · ارسال و سرویس مرحله۳
(Shipping: responsibility toggle + published service + prep days + destination/window) · محدودیت تعداد خرید (Offer) ·
مرجوعی (Offer) · وضعیت و انتشار (Offer lifecycle) · تاریخچه تغییرات. Sticky action bar: ذخیره پیش‌نویس / انتشار /
ذخیره تغییرات / انصراف. Summary shows live price/available/responsibility/service/return.

## 3) Seller offers table (`/vendor-panel/products`)
Same standard grid, seller-scoped columns: آفر/محصول · SKU · وضعیت · قیمت من · موجودی · مرجوعی · آخرین تغییر ·
عملیات. Same toolbar (search/filter/columns/saved views/export/«+ آفر جدید»). Permissions: only own offers.

## 4) Seller offer form
Catalog product/variant read-only; editable: SKU, price, inventory, shipping service/prep, limits, return window,
status (Draft / ارسال برای انتشار). Sticky save; history.

## Quality choices
Minimum clicks (row actions + saved views), clear visual hierarchy (section cards + labels + domain captions),
RTL-first, Jalali dates + Persian grouped amounts, responsive (2-col → 1-col ≤960px, tables scroll), reuse existing
design-system grid/badges/forms (no bespoke table).

## Domain ownership (explicit, not merged)
Offer owns lifecycle/SKU/limits/return; Pricing owns price/campaign; Inventory owns stock; Shipping owns service/prep/
destination; Catalog owns product/variant; Party owns seller. The unified UI orchestrates, never merges domains.
