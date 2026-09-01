# TB-P07-T043 — Financial section polish

## Integrated financial card
- Tabs moved into card header (`bg-gray-50/70`) with ring-style inactive buttons
- Content padding `p-3 md:p-4`

## Seller breakdown
- Denser table (`px-3 py-2.5`, `text-xs` header)
- Footer emphasis on payable total (`text-blue-800`)
- Hover row affordance

## Summary panels
- Dividers between label/value rows
- Highlight rows retained (blue/emerald) with tighter padding

## History grid
- Compact header (`py-2.5`) and `min-h-[140px]` wrapper to avoid empty void

Data bindings unchanged (still `detail.sellerFinancials`, `detail.financialSummary`, `detail.financialEvents`).
