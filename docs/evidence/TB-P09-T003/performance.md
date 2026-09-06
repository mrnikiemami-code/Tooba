# TB-P09-T003 — Performance sanity

- Orders grid: server-side query engine (existing); no per-row Identity fan-out
- Operations menu: one GET `/operations` per open menu (existing)
- Actor labels on notes/history: batch GetMany/GetContacts (T002-R1); history resolves current page only
- History bounded/paged (`pageSize` clamp 1–50)
- No new N+1 introduced by navigation fix
- Defect fix for shipment allocation prevents duplicate create_shipment actions after open shipment
