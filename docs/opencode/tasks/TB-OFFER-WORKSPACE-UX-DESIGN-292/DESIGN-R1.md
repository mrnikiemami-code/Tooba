# DESIGN (R1) — Offer workspace redesign (design-only, TB-292-R1)

Frontend Design tool = Impeccable (loaded skill) drove layout/priority; independent quality reviewed separately.
Prototype: `mockups/offer-workspace-r1.html` (4 views) + screenshots `r1-*.png` (incl. product picker, mobile 360).

## 1. Offer table (admin & seller) — exact order-grid pattern
- Uses the standard `ServerGridPage`/`AppDataGrid` pattern (same toolbar/actions/rows as `/admin/orders`): search,
  advanced filter, column manager, saved views, Excel/CSV, actions column, server-side pagination, row hover,
  colored Persian status badges, Jalali dates, grouped amounts. **No KPI cards above the table** (removed).
- Admin columns: محصول · گونه · فروشنده · کد فروشنده · کانال فروش · قیمت/ارز · تخفیف · موجودی قابل فروش ·
  مسئولیت ارسال · وضعیت · آخرین تغییر · عملیات. Seller table drops فروشنده.

## 2. Seller & product picker (no plain dropdowns)
- Dedicated picker modal (server-side pagination, search, brand/category filters) sized for 1M products / 10k sellers.
- Product rows show image + name + brand + variant specs; variant selection via real attribute chips (color/size).
- Nothing auto-selected; after create, seller+variant are locked (badge «قفل‌شده پس از ایجاد»).

## 3. Multi-currency pricing (not a single field)
- Price rows table: ارز (allowed currencies) · مبلغ (money input, thousands sep) · بازار · کانال (matches offer) ·
  شروع اعتبار (Jalali datetime) · پایان (optional) · وضعیت (پیش‌نویس/فعال/بازنشسته) · نوع (پایه/کمپین).
- Add row, edit amount, retire. Saved price currency is locked (disabled) — must define a new price.
- IRR no decimals; other currencies follow Pricing precision.

## 4. Discounts (Promotion-owned)
- Dedicated section: percent / fixed(+currency), start/end, min qty, min amount, coupon, customer/org restriction,
  market/channel, priority, status, related campaigns + live preview (base → discount → final).
- Active existing discount not economically editable (view/retire only). «افزودن تخفیف» flagged as needing Promotion
  contract completion (not presented as ready op).

## 5. Inventory (multi-warehouse)
- Table: انبار (allowed) · موجودی فیزیکی (editable) · رزروشده (read-only) · قابل فروش (read-only) · دلیل تغییر ·
  عملیات (per permission). Total shown. «افزودن موقعیت انبار» present but flagged «نیازمند تکمیل قرارداد اجرایی».

## 6. Quantity & unit
- Sales unit read-only from product; shown next to every quantity field; integer/decimal per product policy (0–6 dec),
  step + rounding enforced by product; min/max + «برداشتن محدودیت خرید».

## 7. Shipping
- Responsibility segmented: فروشنده / پلتفرم / **تعیین‌نشده**. Shipping type + service chosen ONLY from published
  and owner-allowed services; scope/schedule/prep/conditions shown read-only (no redefining rate/scope/schedule in
  offer). SingleStore vs Marketplace respected. Note: number of assignable services NOT assumed final (Stage-3).

## 8. Returns
- Three real backend modes: پیش‌فرض / سفارشی(روز) / غیرقابل‌مرجوع (only with permission); min/max days + seller
  override reflected.

## 9. Status & operations
- Draft/Active/Suspended/Archived; stock/buyability shown independently; change history; actions by status+permission;
  archived cannot be re-activated (edit disabled in table). Unsaved-changes guard (beforeunload + indicator) and
  per-section save-error demo.

## 10. Professional criteria
- No long tedious single scroll: left section-nav + focused cards + sticky summary; single action bar (no duplicate
  buttons); internal domain names NOT shown to end users; frequent info actions accessible; mobile responsive (1-col);
  RTL/Persian/Jalali/design-system; features lacking full API (add-warehouse, add-discount) flagged, not faked.

## Delivered views
admin offers table · admin offer form · seller offers table · seller offer form — plus picker + mobile screenshots.
