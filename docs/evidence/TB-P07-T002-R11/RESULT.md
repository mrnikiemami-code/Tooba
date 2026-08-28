PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

Task: TB-P07-T002-R11 — Official AG Grid legacy theming, clear-all filters, column manager rebuild, page size 1000.

Validation:
- FE typecheck: 0 errors
- FE lint: 0 errors (img warning pre-existing)
- grid tests: 54 pass
- admin tests: 13 pass
- FE build: 0 errors
- BE pageSize tests: 3 pass
- BE build: 0 errors (Host restarted)
- live API pageSize=1000: accepted (pageSize 1000, totalCount 82)
- live API pageSize=1001: clamped to 1000

Preview: http://localhost:3000/admin/products

Official docs audit: docs/evidence/TB-P07-T002-R11/01-ag-grid-official-help.md
