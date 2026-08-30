# final-product-list.md — TB-P07-T035

`USER_VISUAL_ACCEPTED=NO`  
Screenshots: `screenshots/01-products-grid.png`, `01c-products-grid-paging.png`

## Checks

- Route `/fa/admin/products` loads with AppDataGrid (canonical).
- Columns: عملیات، رسانه، محصول، وضعیت، دسته، برند، تنوع، به‌روزرسانی — **no Price/Stock**.
- RTL; Draft («پیش‌نویس»); brandless rows show «بدون برند».
- Paging: ۱ تا ۲۰ از ۲۸۳ (page size selectable).
- Create CTA → `/admin/products/new`.
- Media column: after v2 seed, 320×320 patterned primary thumbs (not blank 48px).

## Notes

Prior audit FAIL on gray media cells was **pre-reseed**. Re-verify thumbs post Host restart + reset-and-seed.
