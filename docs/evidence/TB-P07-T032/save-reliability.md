# Save reliability — TB-P07-T032

## Product
- Global EDIT sticky: Save keeps EDIT via `formMode.clearDirty()` (not forced VIEW).
- General / Translations / Attributes / Variants / Media / SEO mutations map failures through `mapAdminErrorMessage`.
- Success toasts after confirmed mutations (product saved, variants updated, media attached).

## Category
- Global header Edit across mutable tabs; Save/Cancel/End Edit in header.
- General/Translations saves clear dirty and stay in EDIT until پایان ویرایش.
- Toast: «تغییرات دسته‌بندی ذخیره شد.»

## Busy
- Buttons disabled while busy; labels use «در حال ذخیره…» / «در حال افزودن…» where applicable.
- Dirty cleared only after backend success.
