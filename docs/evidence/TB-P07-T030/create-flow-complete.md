# Create flow complete — TB-P07-T030-R1

Route: /admin/products/new (no ?create=1, no inline list panel)

## 8 stages (fa + en labels)
1. دسته اصلی / Primary Category — ProductCategoryPicker L3
2. اطلاعات پایه / Base structure — slug + optional brand; Product≠Offer note
3. ترجمه‌ها / Translations — fa+en TipTap rich description; Draft created on Continue
4. ویژگی‌ها / Attributes — real ProductAttributesPanel (schema-driven)
5. تنوع‌ها / Variants — real ProductVariantsPanel (T028 UX)
6. رسانه / Media — real ProductMediaPanel + MediaLibraryDialog DAM
7. SEO — real ProductSeoPanel
8. بررسی و ایجاد / Review — readiness summary + jump-back + open workspace

## Draft-first
- createAdminProduct once after translations
- banner: پیش‌نویس محصول ایجاد شد؛ می‌توانید اطلاعات را تکمیل کنید.
- productId retained; no duplicate create on step retry
