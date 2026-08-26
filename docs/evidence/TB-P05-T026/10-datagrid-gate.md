# 10 — Data Grid gate (TB-P05-T026)

Shared foundation: `src/frontend/design-system/data-grid/*`  
Prior truthful map: `docs/evidence/TB-P05-T024/03-admin-datagrid-capability-map.md`

Representative live Admin grids (Products, Orders, Sellers, Customers, Reviews) share the same foundation.

## Capabilities — mark only what truly works

| Capability | Status | Truthful note |
|---|---|---|
| Server pagination | **PASS** | Grid requests page/size from Host list endpoints |
| Filters | **PASS** | Per-column / typed filters where configured on each screen |
| Sorting | **PASS** | Column sort wired to list queries |
| Column visibility (show/hide) | **PASS** | Foundation column chooser |
| Column reorder | **PASS** | Foundation supports reorder |
| Column resize | **PASS** | Foundation supports resize |
| Saved views | **PASS (foundation)** | Client/foundation saved-view store; not a fake server “workspace product” |
| Selection | **PASS (foundation)** | Row selection where screens enable it |
| Bulk actions | **PASS where real** | Only real Host actions (e.g. review moderation); no fake mass ops |
| Export | **PASS (honest)** | CSV visible/selected + honest notice when Host export endpoint not available — no fake download of invented data |
| Responsive / narrow mode | **PASS** | Internal scroll / narrow layout behavior from foundation |

## Explicit non-claims

- Do **not** claim enterprise BI export, server-side saved-view sync, or analytics pivots beyond implemented foundation.
- Do **not** invent “all screens identical feature parity” beyond the shared Data Grid contract.

**Data Grid gate: PASS (truthful foundation capabilities)**
