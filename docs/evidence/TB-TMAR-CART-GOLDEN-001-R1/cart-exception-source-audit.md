# cart-exception-source-audit

Audit of Cart production `InvalidOperationException` sources reachable by HTTP use cases.

## Classification legend

- **A** Expected business → mapped by exact machine code in `CartExceptionMapper.TryMapExact`
- **B** Unexpected invariant/system → not mapped; propagates
- **C** Foreign contract seam → Cart emits Cart-owned stable code at Cart boundary (no prose parse)

## Domain — ShoppingCart.cs

| Message | Class | Public SemanticError |
|---|---|---|
| cart.line.missing | A | cart.line.missing |
| cart.version.stale | A | cart.version.conflict |
| cart.line.requires_active | A | cart.rejected |
| cart.converted.not_expirable | A | cart.rejected |
| cart.user_id.required | B | (propagate) |
| cart.guest_secret.hash_required | B | (propagate) |
| cart.line.merge_via_quantity | B | (propagate) |
| cart.convert.order_required | B | (propagate) |
| cart.assign.guest_only | B | (propagate) |
| cart.expiry.future_required | B | (propagate) |
| cart.market.required | B | (propagate) |
| cart.currency.invalid | B | (propagate) |
| cart.expiry.after_created | B | (propagate) |

## Domain — CartLine.cs

| Message | Class | Public SemanticError |
|---|---|---|
| cart.line.quantity_positive | A | cart.quantity.invalid |
| cart.line.quantity_ceiling | A | cart.quantity.invalid |

## Infrastructure — CartDirectory.cs

| Message | Class | Public SemanticError |
|---|---|---|
| cart.missing | A | cart.missing |
| cart.guest_secret.invalid | A | cart.guest.invalid |
| cart.access.denied | A | cart.access.denied |
| cart.version.stale | A | cart.version.conflict |
| cart.offer.missing | A / C | cart.offer.unavailable |
| cart.offer.inactive | A / C | cart.offer.unavailable |
| cart.offer.channel_mismatch | A / C | cart.offer.unavailable |
| cart.quantity_policy.missing | A / C | cart.quantity.invalid |
| offer.min_quantity.not_met | A / C | cart.quantity.invalid |
| offer.max_quantity.exceeded | A / C | cart.quantity.invalid |
| cart.inventory.missing | A / C | cart.inventory.insufficient |
| cart.inventory.insufficient | A / C | cart.inventory.insufficient |
| cart.user_id.required | B | (propagate) |
| cart.pricing.quote_missing | B | (propagate) |

Directory internal `catch (InvalidOperationException)` in merge/revalidate paths are soft-continue for snapshot freshness, not Result classification.

## Application handlers

| Source | Class | Public SemanticError |
|---|---|---|
| CartErrorCodes.Missing (GetCart / GetCurrent) | A | cart.missing |
| CartErrorCodes.AuthenticationRequired (GetCurrent / Merge) | A | checkout.authentication_required |

Handlers use `CartExceptionMapper.TryAsync` which maps only exact known codes; unknown IOE propagates.

## Infrastructure — CartOutboxRegistration.cs

| Message | Class | Notes |
|---|---|---|
| cart.outbox.unmapped_event | B | not on HTTP path; propagates |

## Retained / unchanged throw sites

Expected-business Domain/Directory messages already used stable machine codes (no Persian prose throws in production). Repair changed classification quality only: exact map + no unknown swallow.

## Foreign modules

No Offer/Inventory/Catalog production code changed. Cart continues to emit Cart-owned codes at the Cart directory boundary after contract lookups.
