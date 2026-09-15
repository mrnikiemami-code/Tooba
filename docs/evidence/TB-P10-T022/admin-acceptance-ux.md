# Admin acceptance UX — TB-P10-T022

## Ordinary-user flow (unchanged architecture)

Create page → blank یا قالب → افزودن بخش → انتخاب مدل بصری → تنظیمات مرتبط → ترتیب → فعال/غیرفعال → پیش‌نمایش → انتشار → اختیاری تنظیم به‌عنوان صفحه اصلی

## Polish applied

- Persian section/variant labels cleaned of technical loanwords (`کاروسل`/`HTML`/`مینیمال`/math mosaic jargon).
- Template picker help no longer promises English «صنعت» display; cards show FA name, description, section count, composition summary.
- Template selection no longer prefills English `templateKey` as page slug (operator sets address).
- Section card summaries include category/brand selection counts.
- Category/Brand settings show dashed empty-selection guidance (like products).
- Size preset chips remain FA (جمع‌وجور / متوسط / بزرگ / خیلی بزرگ).
- No UI labels for SectionType / VariantKey / ResponsiveContract / JSON / CSS / breakpoints.

## Guard

`admin-landing-pages.guard.test.ts` — TB-P10-T022 ordinary-user Admin UX assertions.
