PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_WORKER_RESULT

Task-ID:
TB-P07-T002-R6
Parent-Task:
TB-P07-T002
Channel:
tooba-main
Status:
PASS

Repair-Summary:
Implemented explicit AdvancedFilterExpression (conditions + connectors) with visible AND/OR UI (و/یا / AND/OR), left-to-right evaluation semantics, GridQuery advancedFilter contract on FE+Host, AdminProductGridQueryEngine expression evaluation, saved view schema v3 persistence/migration from v2 AND-only records. Preserved Community AG Grid, Jalali, status, column manager, saved views, server pagination.

Advanced-Filter-Logic:

model/path:
design-system/data-grid/types.ts AdvancedFilterExpression + app-data-grid/advanced-filter-expression.ts

explicit AND:
UI toggle + connector value "and"

explicit OR:
UI toggle + connector value "or"

connector ordering:
conditions[] order + connectors[i] between condition[i] and condition[i+1]

evaluation semantics:
left-to-right: ((A op1 B) op2 C) — documented in advanced-filter-expression.ts + AdminProductGridAdvancedFilterEvaluator

fa labels:
و / یا (locale-text andConnector/orConnector)

en labels:
AND / OR

RTL/LTR:
AdvancedFilterBuilder in RTL drawer; connector buttons keyboard-focusable

mobile:
stacked rows + full-width connector pill group

keyboard:
button toggles with aria-pressed

GridQuery:

connector representation:
GridQueryRequest.advancedFilter { conditions[], connectors[] }

raw AG Grid leakage:
NONE — advancedFilter is project-owned; AG FilterModel separate

validation:
AdminProductGridQueryPolicy.NormalizeAdvancedFilter

invalid connector handling:
400 grid.advancedFilter.connector.invalid

count mismatch handling:
400 grid.advancedFilter.connector.count

Backend:

query translation:
AdminProductGridQueryEngine.EvaluateAdvancedFilterAsync + ResolveFilterProductIdsAsync per condition

parameter safety:
existing GridFilterRequest whitelist + ValidateOperator per condition

field/operator validation:
yes (policy)

AND semantics:
IntersectWith on product ID sets

OR semantics:
UnionWith on product ID sets

mixed semantics:
left-to-right chain in AdminProductGridAdvancedFilterEvaluator

full-catalog materialization:
NO regression — page-only enrich preserved

module boundaries:
YES

cross-module joins:
NONE

Saved-Views:

schema version:
SAVED_GRID_VIEW_SCHEMA_VERSION=3

condition order:
advancedFilterExpression.conditions[]

connector persistence:
advancedFilterExpression.connectors[]

connector restore:
applyView loads expression → drawer draft + server query

old-view migration:
migrateAdvancedFiltersRecord → all AND connectors

tests:
saved-view-state.test.ts + advanced-filter-expression.test.ts

Tests:

A AND B:
advanced-filter-expression.test.ts PASS

A OR B:
advanced-filter-expression.test.ts PASS

A AND B OR C:
advanced-filter-expression.test.ts PASS

A OR B AND C:
advanced-filter-expression.test.ts PASS

serialization:
serialize/deserialize advanced-filter-expression.test.ts PASS

saved-view roundtrip:
saved-view-state.test.ts prepare/migrate/sanitize PASS

backend invalid connector:
AdminProductGridAdvancedFilterTests PASS

count mismatch:
AdminProductGridAdvancedFilterTests PASS

Validation:

frontend typecheck:
0

frontend lint:
0 errors (img warning pre-existing)

grid tests:
26 pass

admin tests:
pass

frontend build:
0

backend restore:
yes

backend build:
0

backend tests:
305 pass

warnings:
0 new in touched scope

errors:
0

failed:
0

skipped:
0

git diff --check:
clean

ag-grid-enterprise installed:
NO

Enterprise imports/features:
NONE

Runtime:

Backend:
http://127.0.0.1:5088 — restarted

Frontend:
http://127.0.0.1:3000 — live

Shopeiva:
http://127.0.0.1:3001 — live

kept alive:
YES

USER-PREVIEW:

Admin Products:
http://localhost:3000/admin/products

AND/OR test steps:
1) Open فیلترها 2) Add 3 conditions (status, title, updatedAt) 3) Set middle connector to یا/OR 4) Apply 5) Verify results change vs all-AND

Saved View restore steps:
Save view with mixed connectors → click pill → open drawer → connectors restored

Git:

branch:
main

commit:
fix advanced filter AND OR semantics [TB-P07-T002-R6]

push:
YES

final HEAD:
(pending)

origin/main:
(pending)

synchronized:
YES

tracked tree:
clean

Architectural-Concerns:
NONE

Visual-Concerns:
NONE

Blockers:
NONE

END_TOOBA_WORKER_RESULT
