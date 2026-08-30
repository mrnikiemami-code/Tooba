# MegaMenu + Brand pickers — TB-P07-T032

## MegaMenu
- Current Category is route-bound (workspace `categoryId`); user does not re-select self.
- Placement destination uses `AdminSearchableCombobox` (search by human path/name).
- Save/reload retained.

## Brand
- Integrated searchable combobox (`product-edit-brand`); search inside dropdown.
- Option «بدون برند» → nullable BrandId (no fake No Brand entity).
- Home «برندهای محبوب» remains Published brands only (unchanged storefront rule).
