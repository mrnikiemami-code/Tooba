# Discovery — TB-P09-T014

- Catalog nav: `admin-shell.tsx` catalog group had categories/attributes; no UoM page. Added `/admin/catalog/units`.
- UoM domain already existed (`UnitOfMeasure` + `UnitOfMeasureTranslation.LanguageId`). No prior admin CRUD.
- Language Registry: `/v1/admin/languages` returns `languageId`. FE `SupportedLocaleDefinition` now maps it.
- Product workspace already exposed Unit / DecimalPlaces / Step. Create flow still omits quantity (edit after create).
- `ListUnitOptionsAsync` was active-only; current inactive unit could drop. Now includes current unit.
- Seller offer min/max existed; Product unit was not shown; no FE Min≤Max. No Admin offer editor.
- Rounding tab existed; helper mentioned historical orders only.
- Canonical invoice = `SellerOrder` + `OrderLine` print HTML. No Invoice entity/module.
- Header money snapshots existed (Subtotal/Discount/Tax/GrandTotal). T014 added item count, total qty, net, duty, tax+duty, rounding snapshot.
- List quantity column previously Line JOIN+SUM; now `SellerOrder.TotalQuantity`.
- Duty did not exist; persisted 0 unless a real duty calc exists.
