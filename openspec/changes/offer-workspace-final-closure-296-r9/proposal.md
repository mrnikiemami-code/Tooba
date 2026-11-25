# Proposal — offer-workspace-final-closure-296-r9

## Why
Owner accepted R8 visual improvement as the repair baseline but not final functional acceptance. R9 closes the remaining ordinary full-stack gaps on the single-offer workspace without redesigning the owner-approved R8 visual layout or opening the unissued TB-297+ financial roadmap.

## What changes
- Preserve the seven-tab workspace, four routes and two AppDataGrid lists.
- Use the actual shared Toast and MessageBox mechanisms for transient success, operation errors, duplicate/link, uncertain/reload and dirty exit.
- Correct the inventory-reason recovery and expose human warehouse names from Inventory.
- Read Catalog unit/precision/step/rounding and validate quantities.
- Audit channel authority; do not fake a channel selection where no authority exists.
- Unlock supported editable tabs before persistence where the owning contracts allow it, while keeping inventory/history/honest gaps explicit.
- Integrate the DB language registry into actual offer routes/lists, including an existing third locale, without mutating the live registry.
- Scope draft recovery by actor/tenant/owner, clear committed/discarded drafts and prevent stale overwrite.
- Strengthen independent save rereads and record concurrency limitations precisely.
- Prove two-location partial failure and restore using real isolated runtime data.
- Run the full build/typecheck/lint/backend/FE/architecture/OpenSpec/regression/runtime/Impeccable matrix.

## Impact
Offer workspace and lists, shared Toast/MessageBox integration, Catalog/Inventory/Offer read contracts (additive), locale integration in offer-owned surfaces, tests and evidence. No migrations, no shared-grid redesign, no financial/promotion/shipping Stage-3/multi-currency invention, no TB-297+ start.
