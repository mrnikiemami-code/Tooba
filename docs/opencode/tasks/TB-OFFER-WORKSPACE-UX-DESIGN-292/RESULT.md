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

## R1 REDESIGN (architect-mandatory) — delivered
- New prototype mockups/offer-workspace-r1.html (4 views) + screenshots r1-*.png (admin/seller tables+forms, product picker, mobile 360).
- Exact order-grid pattern (no KPI cards); columns per spec (seller removed in seller table).
- Pro product/seller pickers (search/filter/server-paging, 1M/10k scale, image+brand+variant chips, no auto-select, locked after create).
- Multi-currency price rows (add/edit-amount/retire; currency locked once saved; IRR no decimals) + discounts section w/ preview + multi-warehouse inventory + UoM + shipping (incl unset, published-only, editions) + 3 return modes + lifecycle/history/unsaved-guard.
- Domain names hidden; add-warehouse & add-discount flagged as not-ready (contract pending).
- Impeccable R1 review: APPROVED (no blocking). No product/backend/db change. STOP_FOR_DESIGN_APPROVAL.

## R2 (architect reject → redesign)
- Prototype mockups/offer-workspace-r2.html + r2-*.png (create/edit/error/picker/seller/mobile).
- Fixed all 11 UX issues; dependency matrix enforced; overlap=0; minFont 13; contrast 16.4/6.99; single save; advanced collapsed.
- Real rendered audit evidence: evidence/audit-r2.cjs + audit-r2-result.json (all checks true). Impeccable R2 APPROVED on rendered output. Design-only. STOP_FOR_DESIGN_APPROVAL.
