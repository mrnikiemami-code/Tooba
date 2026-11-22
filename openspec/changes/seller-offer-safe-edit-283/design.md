# Design

## Context

See `proposal.md` and `specs/seller-offer-safe-edit/spec.md`. The existing seller edit page calls one Offer patch, one Pricing write, and one Inventory write on every save. Offer read models currently concatenate an Inventory summary without positions; Inventory seller write chooses the first position. The owner boundaries are already established.

## Goals / Non-Goals

**Goals**
- Expose location positions from Inventory through the existing Offer read model.
- Bind stock writes to an explicit position and reject ambiguous multi-position callers.
- Save only changed sections and validate their responses.
- Make the form honest and usable on mobile.

**Non-Goals**
- No admin create flow, discounts, shipping service selection or settlement.
- No new inventory reservation model, migration, table or provider.
- No live data mutation for testing.

## Decisions

1. Extend the Inventory seller summary with location positions and a target StockItemId on the write request.
   The target is validated against the owned offer. If absent and the offer has >1 position, refuse with the existing `inventory.position.id_required`; one position remains legacy-compatible.

2. Keep three independent owning commands. The UI detects changed sections and sends only their commands. This avoids stale price/stock overwrites and makes concurrency safe by construction.

3. Add `ClearOrderQuantityLimits` to the Offer patch request/command. Absent fields preserve values; this explicit flag clears both. Status is sent only when changed, so Draft SKU edits do not need a transition.

4. Validate response identity and effect in `seller-api.ts`: expected offer, expected seller, expected amount or position quantity. A mismatch is an error; network loss is uncertainty, not success, and the page does not automatically retry.

5. Show aggregate stock read-only and each named position with its own input. The existing screen and design-system controls are reused; no parallel form or shell.

## Risks / Trade-offs

- [More vertical space on mobile] -> Use compact position rows and normal page scroll; keep the aggregate separate.
- [Multiple changed positions] -> Send one explicit command per changed position, stopping on first failure and preserving other inputs.
- [Legacy callers] -> Single-position offers keep the old no-target behavior; multi-position offers fail closed.
- [Concurrent price/SKU] -> Independent commands mean the SKU patch cannot carry stale price/stock.

## Migration Plan

Contracts are additive/trailing. No schema or data migration. Rollback is a frontend/backend code revert; existing single-position callers remain valid.

## Open Questions

None.
