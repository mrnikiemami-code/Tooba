# RESULT — TB-OFFER-WORKSPACE-UX-DESIGN-292
Status: PASS (design only — no product code)
- OpenSpec analysis of ALL current Offer capabilities + Stage-3 needs (ANALYSIS.md, nothing-dropped checklist).
- Standard table reuse mandated = ServerGridPage/AppDataGrid (same as /admin/orders): search, advanced filter, column
  manager, saved views, Excel/CSV, row actions, status badges, Jalali dates, grouped amounts, server-side paging. No
  bespoke/legacy grid.
- 4 interactive browser views delivered (mockups/offer-workspace.html + PNG screenshots): admin offers table, admin
  offer form, seller offers table, seller offer form. Form covers seller/product/variant, pricing/discount, inventory,
  shipping service/prep/destination/window (Stage 3), purchase limits, returns, status/publish, change history —
  with explicit DOMAIN OWNERSHIP labels (Offer/Pricing/Inventory/Shipping/Catalog/Party), no domain merging.
- Quality: RTL-first, Persian, Jalali, responsive (≤960px single column, mobile 360×640 captured), design-system reuse.
- Impeccable independent review: APPROVED (no blocking defects; minor follow-ups recorded).
- No backend/DB/product-logic change. Final implementation gated on owner approval.
STOP_FOR_DESIGN_APPROVAL
