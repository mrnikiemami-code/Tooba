# UoM catalog UI — TB-P09-T014

- Nav: واحدهای اندازه‌گیری → `/admin/catalog/units` (`product.view`)
- AppDataGrid: Code, localized Name/ShortName, Dimension, Active, SortOrder, ops
- Grid locale from admin chrome language; no NameFa/NameEn columns
- Add/Edit Dialog: Code, Dimension, IsActive, SortOrder, dynamic language tabs (Name/ShortName)
- Deactivate if referenced; no hard-delete
- API: GET/POST/PUT `/v1/admin/catalog/units`, POST deactivate
