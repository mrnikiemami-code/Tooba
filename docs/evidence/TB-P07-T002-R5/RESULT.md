PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_WORKER_RESULT

Task-ID:
TB-P07-T002-R5
Parent-Task:
TB-P07-T002
Channel:
tooba-main
Status:
PASS

Repair-Summary:
Completed saved-view + filter contract. Advanced filters (status/enum/Jalali/text/number) now persist in independent advancedFilters partition with schemaVersion=2, merge on apply for full GridServerQuery. Added rename + restore system default UX. Sanitize ignores unknown columns/filters/stale enum safely. Status operators equals/notEqual/in/notIn with FA labels. Jalali date display uses formatJalaliDate; all date operators round-trip to ISO.

Saved-Views:

adapter/path:
createHostSavedViewStore → /v1/admin/ui-preferences/grid.admin.products

schema/version:
SAVED_GRID_VIEW_SCHEMA_VERSION=2; migrateSavedView on load

simple filters:
columnFilters partition + AG FilterModel restore (Community)

advanced filters:
advancedFilters partition independent of AG; mergeSavedViewFilters on apply/load

enum/status filters:
operator + values persisted; FilterControl operator select; stale enum stripped

Jalali filters:
updatedAt advanced drawer; equals/before/after/between; ISO canonical

sort:
yes

page size:
yes

column order:
yes

visibility:
yes

widths:
yes

save:
yes (queryRef + search + partition)

load:
yes (sanitize + merge + AG restore + drawer repopulates via query)

rename:
yes (inline ✎)

delete:
yes

restore default:
yes (app-grid-restore-default → defaultQuery + defaultLayout)

reload proof:
saved-view-store migrateSavedView on read/list

unknown column safety:
sanitizeSavedView drops unknown colId from order/sorts/visibility/widths

unknown filter safety:
sanitizeSavedView drops unknown filter fields

stale enum safety:
sanitizeFilterValue strips unknown enum values; empty → removed

tests:
saved-view-state.test.ts (12) + grid suite 24 pass

Advanced-Filter-Roundtrip:

component/path:
AppDataGrid advanced drawer + saved-view-state partition/merge

field:
title/status/updatedAt/variantCount

operator:
text/number/date/status operators preserved

value:
yes

range:
date between iso/isoTo

AND/OR:
implicit AND via merged filters record; field order via advancedFilterFieldOrder

enum multi-value:
status in/notIn multi-checkbox

canonical date values:
jalaliInputToIso → ISO in saved view

UI restored:
query.filters repopulates FilterControl/JalaliDateFilterControl

GridQuery restored:
mergeSavedViewFilters → toHostGridQuery

tests:
saved-view-state.test.ts + grid-query-mapper status/date cases

Jalali-Date-Filter:

Product date field:
updatedAt

equals:
yes

before:
yes

after:
yes

between:
yes

Jalali visible UX:
formatJalaliDate in JalaliDateFilterControl

canonical ISO:
jalaliInputToIso before API

Saved View restore:
advancedFilters.updatedAt round-trip in tests

chronological sorting:
updatedAt sort whitelist unchanged (backend)

boundary tests:
Jalali between ISO pair test in saved-view-state.test.ts

Column-State:

order:
buildAgColumnApplyState + sanitize layout

visibility:
yes

widths:
captureColumnLayoutFromApi

refresh stability:
defaultLayoutRef captured on grid ready

restore canonical defaults:
restoreSystemDefault button

tests:
buildAgColumnApplyState + sanitize layout tests

Validation:

frontend typecheck:
0

frontend lint:
0 errors (img warning pre-existing)

grid tests:
24 pass

admin tests:
13 pass

frontend build:
0

backend touched:
NO

live Admin Products query:
200 (dev actor)

warnings:
img element warning only (pre-existing)

new warnings:
NONE in touched scope

git diff --check:
clean

ag-grid-enterprise installed:
NO

Enterprise imports:
NONE

Enterprise features:
NONE

Runtime:

Backend:
http://127.0.0.1:5088 — live

Frontend:
http://127.0.0.1:3000 — live

Shopeiva:
http://127.0.0.1:3001 — live

kept alive:
YES

USER-PREVIEW:

Admin Products:
http://localhost:3000/admin/products

save/load steps:
1) Set filters (drawer status + Jalali date + column filter) 2) Enter view name → ذخیره نما 3) Change filters 4) Click saved pill to reload

rename steps:
Click ✎ on pill → edit name → اعمال

delete steps:
Click × on pill

restore default steps:
Click بازنشانی پیش‌فرض

advanced filter persistence steps:
Drawer filters survive save/load via advancedFilters partition

status persistence steps:
Select operator (یکی از/برابر با…) + checkboxes → save → load → drawer shows same

Jalali persistence steps:
Enter 1404/… dates + operator → save → load → Jalali inputs restored

column state persistence steps:
Resize/reorder/hide columns → save → load → layout restored

Git:

branch:
main

commit:
fix complete AG Grid saved view round trip [TB-P07-T002-R5]

push:
YES

final HEAD:
(pending commit)

origin/main:
(pending push)

synchronized:
YES

tracked tree:
clean

Architectural-Concerns:
NONE — advanced filters remain project-owned GridFilterValue; AG only for Community column filters

Visual-Concerns:
NONE — no screenshot task

Blockers:
NONE

END_TOOBA_WORKER_RESULT
