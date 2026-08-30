# Final Reference Comparison — TB-P07-T031

Locked reference opened:

`D:\Users\User\source\repos\SarvNewVerRequirment\reference\Image\ChatGPT Image Aug 29, 2026, 06_33_46 PM.png`

Compared against live Product Admin VIEW/EDIT + list/create/Category Admin.

## Aligned (material)
- RTL Persian Catalog Admin chrome
- Product header: title, lifecycle, category path, primary thumb
- Save split + پایان ویرایش + انصراف action cluster
- Readiness / summary cards with meaningful progress (not color-only)
- Tab strip: General / Translations / Attributes / Variants / Media / SEO / Publishing / History
- General desktop media side-column + gallery thumbs + manage-all affordance
- TipTap-style rich description (controlled fonts/sizes; sanitized)
- Product list uses polished grid (image, title, status, category, **Brand**, variants, updated, actions)

## Architecture-locked differences (not treated as unfinished mock debt)
- Locale chips: live enables **fa/en only** — reference mock shows Arabic; fabricating Arabic is forbidden (T030-R2 ACCEPT)
- General tab: live keeps language-neutral ownership fields; localized title/short/full live under Translations (reference mock mixes copy into General)
- Exact card counts / demo timestamps / demo history rows are fixture-dependent

## Remaining material visual mismatch for Catalog Admin gate
**none** identified after Brand list-column repair.

`USER_VISUAL_ACCEPTED=NO` (Worker must not claim human visual acceptance).
