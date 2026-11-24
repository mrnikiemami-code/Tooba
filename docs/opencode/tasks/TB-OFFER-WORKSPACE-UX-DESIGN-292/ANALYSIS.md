# ANALYSIS — TB-OFFER-WORKSPACE-UX-DESIGN-292 (OpenSpec capability inventory)

Source-grounded from real contracts: `SellerOfferPanelDtos.cs` (ListItem/DetailPage/Patch/Create/PriceWrite/
InventoryWrite), `OfferFulfillmentDtos.cs`, Offer endpoints (seller `/v1/seller/offers*`, admin `/v1/admin/offers*`,
`PUT /v1/admin/offers/{id}/fulfillment`), Inventory/Pricing/Catalog/Party/Shipping contracts; and the standard
admin table = `ServerGridPage` (design-system/app-data-grid) used by `AdminOrdersScreen` with `createOrderColumns` +
`createHostSavedViewStore`.

## A. Current Offer capabilities (must ALL survive the redesign)
Offer fields (DetailPage/Create/Patch): OfferId, SellerPartyId + SellerDisplayName, CatalogVariantId, ProductId +
ProductTitle, BrandName, SellerSku, Status (Draft/Active/Suspended/Archived), Channel, Amount + Currency,
OnHand/Reserved/AvailableUnits, CatalogReadOnly, ReturnPolicyChoice (Default/Custom/NonReturnable) +
CustomReturnWindowDays + DefaultReturnWindowDays + Min/MaxReturnWindowDays + SellerCanOverrideReturnPolicy,
MinimumOrderQuantity + MaximumOrderQuantity (+ ClearOrderQuantityLimits), fulfillment responsibility (Seller|Platform)
+ ShippingTypeId (assign via admin), change history/last-updated.
Endpoints: seller create (idempotent requestId), patch, price write, inventory write, list/detail, dev-contexts;
admin create/patch/list/detail/query, assign fulfillment.

## B. Standard table (must reuse, not rebuild)
`ServerGridPage` + `AppDataGrid`: server-side paging, search, advanced filter (`AdvancedFilterBuilder/Drawer`),
column manager + labels, saved views (`createHostSavedViewStore`), pinned/row actions (`app-grid-row-actions`),
export Excel/CSV, status cells/badges, Jalali date + amount formatting. Offer tables MUST use this, not a bespoke grid.

## C. Stage 3 (Shipping) needs to appear in the workspace (design only)
Per-offer shipping **service** assignment per shop/destination/time-window; prep-time; no phantom methods;
SingleStore vs Marketplace; checkout eligibility is read-side (not authored here) — surfaced as read-only context/links.

## D. Nothing-dropped checklist (traceability)
Seller+Variant · SKU · Status lifecycle (Draft/Active/Suspended/Archived) · Price+Currency · Inventory
(onHand/reserved/available) · Purchase limits (min/max/clear) · Return policy (default/custom/nonreturnable + window
bounds) · Fulfillment responsibility + shipping type · Stage-3 service/prep/destination/window · Change history ·
Admin dual-role (create for seller + assign fulfillment) · Idempotent create · Table (search/filter/columns/saved
views/export/actions/pagination) · RTL/Persian/Jalali/amount.
