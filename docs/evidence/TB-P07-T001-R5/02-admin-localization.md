# TB-P07-T001-R5 — Admin localization (operator-facing)

## Scope
UI/UX only. No backend/schema. Files touched for operator copy:

- `src/frontend/app/admin/admin-screens.tsx`
- `src/frontend/app/admin/product-list.tsx`
- `src/frontend/app/admin/product-workspace-screen.tsx`

## Replacements (samples)

| Before (operator-facing) | After |
| --- | --- |
| ارسال / fulfillment | ارسال و تحویل |
| جزئیات fulfillment / fulfillment خوانده نشد | جزئیات ارسال و تحویل / ارسال و تحویل خوانده نشد |
| مرجوعی … refund | مرجوعی و بازپرداخت |
| صف payout / رزرو payout | صف پرداخت به فروشنده / رزرو پرداخت به فروشنده |
| marketplace | بازارگاه |
| Host (titles/errors/banners) | فروشگاه / سامانه |
| GMV | مجموع فروش |
| Admin Dashboard API | داشبورد عملیاتی فروشگاه |
| Catalog / Product (create copy) | کاتالوگ / محصول |
| slug | نشانی صفحه |
| Workspace | فضای کار محصول |
| MediaAssetId (Guid) / Guid / DEFERRED / API path | شناسه دارایی رسانه / شناسه کوتاه / بارگذاری فایل هنوز فعال نیست |
| SEO title / SEO description | عنوان جستجو / توضیح جستجو |
| پرداخت (Host) | پرداخت (سامانه) |
| posted | ثبت‌شده |

## GUID presentation
- Fulfillment list primary cell is **گیرنده** with secondary `شناسه کوتاه: …`; raw GUID is not the sole title when a human name exists.
- Return / payout / media rows label short ids as **شناسه کوتاه** (not full GUID as primary chrome).

## Status `enumOptions` (filterKind: status)
| Grid | Source of FA labels |
| --- | --- |
| ارسال و تحویل | `formatFulfillmentStatus` |
| مرجوعی و بازپرداخت | `formatReturnStatus` |
| صف پرداخت به فروشنده | `formatPayoutStatus` |

Orders/products already had enumOptions (R3); unchanged pattern.

## Out of scope
- Seller panel
- Code identifiers / imports / comments that are not operator-visible
- Backend status enums (values remain English codes; UI shows FA)
