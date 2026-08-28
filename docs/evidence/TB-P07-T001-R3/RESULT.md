PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_WORKER_RESULT

Task-ID:
TB-P07-T001-R3
Channel:
tooba-main
Status:
PASS

Summary:
Admin-first reconciliation complete. Kept T001 category-attribute / variant-axes foundation. Localized Admin nav + DataGrid operators (FA/EN). Admin Product master CRUD with publish/unpublish/archive/safe-delete, SVG multi-image gallery (binary upload deferred), video deferred/hidden, typed specs via attribute panel, clearer variants UX. Product/Order DataGrid enum filters + column manager + Host-persisted saved views (grid.admin.products/orders). Access Control: FA permission labels, enriched user picker, role members, user roles, effective access with resource display names. Preview seed (5 media, draft, archived). Screenshot pack for every live Admin route. Backend 301+4 pass; FE typecheck/lint/test/build green. CURRENT_UI_FOCUS=ADMIN_ONLY. Did not claim SELLER_PANEL_COMPLETE / USER_VISUAL_ACCEPTED / PRODUCTION_GO_LIVE_READY / FULL_VARIANT_MATRIX_LIVE.

Reconciliation:
T001 kept: category attribute schema, inheritance, typed product values, variant axes, combination foundation
adapted: Admin Product Host+FE ownership, media refs DisplayOrder/IsPrimary/AltText, UserPreference ui_preferences, AC DTO enrichment
Seller UI deferred: no Seller visual polish; Offer-only create path; Product create Admin-only
conflicts: none (recovery OK, baseline ancestor)

Admin-Localization:
menu: Persian labels for fulfillment/refund/payout/category schema
raw English: removed from live Admin nav
raw technical: filter operators localized
i18n: admin-chrome-messages.ts + data-grid messages fa/en

Product:
ownership: Admin-only create
CRUD: list/create/get/title + publish/unpublish/archive/delete
images: storefrontMediaUrl SVG presentation (no broken img)
gallery: attach/reorder/primary/alt/remove LIVE
video: DEFERRED + hidden
specs: typed attribute editors
variants: list/create/patch status/code
actions: مشاهده/ویرایش/انتشار/لغو انتشار/بایگانی/حذف

DataGrid:
Product filters: status enum FA
Order filters: payment/status enumOptions FA
saved views: Host /v1/admin/ui-preferences/{key}

Access-Control:
permission labels FA and EN; user picker; role members; user roles; effective access display names

Seed-Screenshots:
seed: 5 media + draft + archived
screenshots: docs/evidence/TB-P07-T001-R3/screenshots/ (24) + 22-admin-screenshot-pack-index.md

Validation:
backend: Host 301 pass, MigrationRunner 4 pass, 0 fail, 0 skip
frontend: typecheck/lint/test/build = 0
git: HEAD == origin/main == b1246e095a38cb994c9160831aa6b9d5b928818e

Flags:
TB-P07-T001 = SUPERSEDED
TB-P07-T001-R1 = SUPERSEDED
TB-P07-T001-R2 = SUPERSEDED
TB-P07-T001-R3 = AWAITING_ARCHITECT_ACCEPT
CURRENT_UI_FOCUS = ADMIN_ONLY

Evidence:
docs/evidence/TB-P07-T001-R3/

Preview:
http://localhost:3000/admin
http://localhost:3000/admin/products
http://localhost:3000/admin/orders
http://localhost:3000/admin/access-control

END_TOOBA_WORKER_RESULT
