# Proposal — offer-workspace-production-296
## Why
Implement the approved OpenDesign-295 7-tab offer workspace in the real product (admin+seller) reusing the public
AppDataGrid for tables, real domain contracts, and real tests. No fabricated capability.

## What Changes
- Offer tables (admin/seller) already use public AppDataGrid (accepted commit 528fd7b5).
- Shared 7-tab workspace for create/edit, role-gated.
- Wire existing contracts (create/get/patch, fulfillment responsibility, return policy, qty limits, lifecycle; price if route verified).
- Record and DO NOT implement capabilities lacking backend contracts.

## Contract gaps (blocking full finish; architect decision)
- Promotion per-offer discount definition (only campaign membership exists).
- Inventory multi-warehouse adjust + reason (no Inventory endpoints).
- Offer history projection (no API).
- Shipping per-offer service assignment (Stage-3 undecided).

## Impact
Frontend offer routes + shared workspace; no domain-root changes; no fake endpoints.
