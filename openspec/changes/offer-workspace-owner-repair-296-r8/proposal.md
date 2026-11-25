# Proposal — offer-workspace-owner-repair-296-r8

## Why
Owner review rejected R7 with concrete UX defects, and architect review confirmed localization, data-integrity and verification gaps. R8 repairs the existing approved surface; it does not redesign it, create a parallel form, change the theme, or alter shared grid infrastructure.

## What changes
- Independent product then variant modal pickers backed by real Catalog products/variants and human axis labels; single/zero variant states; product change clears variant; real media.
- Seller/product/variant modal pickers with server search/paging, current selection, loading/error/empty and close-on-select.
- Header/container-query balance, aligned controls, readable text, active-locale formatting and zero overflow.
- Real create flow: locked-tab reasons, Draft creation after valid identity, real id/edit mode, no open-time record, double-submit lock.
- Duplicate active-listing lookup, existing-offer link, natural errors, network vs conflict separation.
- Real fa/en localization plus isolated third-language fixture; direction/calendar/numbers/currency from active locale; draft retention across language navigation.
- Number/amount normalization/grouping; empty amount never zero; IRR no decimals; zero subject to Pricing.
- Independent stock-location save preserving other drafts/reasons and retrying only remaining adjustments.
- Save safety: try/catch/finally, partial/uncertain classification, stale-snapshot guard, no blind retry.
- Real sales channel in both lists; readable shared AppDataGrid columns/actions; human variant labels.
- Additive Catalog/Offer read contracts for picker products/variants, localized labels, channel and duplicate lookup.
- Test-data disposition and independent acceptance matrix.

## Impact
Offer workspace, admin/seller offer lists, Offer/Catalog read contracts and endpoints, focused tests, OpenSpec/Impeccable evidence. No migration, no shared grid rewrite, no invented discount/shipping/history/multi-currency contract.
