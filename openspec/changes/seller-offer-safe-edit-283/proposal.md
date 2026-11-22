# Proposal

## Why

Editing an offer's SKU can silently rewrite an aggregate multi-location stock total onto the first location. The same form also re-sends stale price/stock and unsupported Draft status, and it can show success for a response that belongs to another offer or is stale. This is the first roadmap stage: make editing an existing seller offer safe without changing money or stock unless the operator deliberately edits that section.

## What Changes

- Read the exact inventory positions of an offer and show the aggregate separately from the editable position.
- Require an explicit stock position for multi-location writes; a legacy caller without a target is refused unless the offer has exactly one position.
- Save only changed sections: SKU/limits/return metadata, price, or named position stock.
- Preserve Draft when the operator edits only SKU and allow both purchase limits to be explicitly cleared.
- Validate every write response for the expected offer, seller, and value; an unrelated/stale response is not success.
- Keep unsaved input in another section when one section saves or fails; no automatic blind retry after an uncertain result.
- Reuse the existing VendorShell page and design-system controls; add clear Persian labels, cancel, dirty/error feedback and mobile-safe geometry.

## Capabilities

### New Capabilities

- `seller-offer-safe-edit`: editing an existing seller offer preserves independent price and per-location inventory identities.

### Modified Capabilities

<!-- None. -->

## Impact

Offer seller endpoint/DTO/read model, Inventory seller stock write, seller API client and the existing `vendor-panel/products/[offerId]` form. No schema, migration, provider, auth policy or live business data change.
