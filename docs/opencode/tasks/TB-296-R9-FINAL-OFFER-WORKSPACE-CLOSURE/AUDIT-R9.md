# R9 implementation audit and limitations

## R9-04 — channel authority
- Source audit: `CreateOfferCommand` accepts any defined `SalesChannel`; the duplicate guard is seller+variant+channel (`OfferStore.ExistsActiveListingAsync` / `FindActiveListingOfferIdAsync`). No owner/domain contract exposes allowed channels by edition, seller, permission or applicable policy. `__platform-commerce` is a development-only edition probe, not a production channel authority.
- UI: the create form no longer presents a hardcoded Marketplace `select` as a user choice. It shows the current system channel as read-only policy with an explicit pending-policy note and still sends the existing Marketplace value required by the current create contract.
- Gap: implement a real allowed-channel read contract (edition/seller/permission/policy) before exposing channel choice or changing SingleStore channel behavior. No fake channel set and no unrelated policy change were made.

## R9-05 — create tabs and inventory provisioning
- Supported before persistence: pricing, conditions, and (admin) shipping. They render from a local draft based on the selected real seller/product/variant; explicit save creates exactly one Draft and then applies price/conditions/shipping through owning clients, with independent reread. Seller shipping remains admin-only.
- Inventory and history remain gated before persistence. No synthetic stock position or total-stock write was created. Missing authority: an eligible-location read/provisioning contract that can create real positions before an Offer exists. Existing positions are used after Draft creation and are proven by the multi-position runtime.
- Discounts, history, shipping service assignment (Stage-3), and multi-currency remain explicitly gated.

## R9-07 — locale integration
- Admin offer routes use `GET /v1/admin/languages`; seller/public offer routes use the additive read-only `GET /v1/languages` active registry. Offer routes accept `?locale=<registry code>` and set offer-owned direction/formatting/copy. The existing active `ru-RU` registry row was used through the actual route; the registry was not mutated.
- The global admin shell routing remains the project fa/en routing. Offer-owned copy is translated for fa/en/ru; missing Russian keys fall back to the official English catalog. Shared AppDataGrid chrome remains on the existing shared grid locale API.

## R9-09 — concurrency
- Audit: Offer, Pricing and Inventory aggregates expose no RowVersion/ConcurrencyToken; the current workspace pre-read (`detailVersionChanged`) is a safety check, not an atomic compare-and-swap. No false claim of atomicity is made. A real optimistic-concurrency contract and migration would be a separate owning-domain decision; no unauthorized migration was created.
- Current behavior: stale pre-read blocks the save, preserves the draft, and requires explicit reconciliation; unknown outcomes block retry until reload.

## R9-11 — fixture safety
- Real isolated IDs used: seeded offer `457e375f-ceec-4b66-b81e-3829c4e15893` and two-position offer `01a0ba10-5a31-7000-b733-4cd74ef3763a`.
- Runtime script snapshots both inventories, restores them in `finally`, archives only `TB296-R9-` offers, and removes the temporary seller-B role/assignment/ceiling. `evidence/r9-runtime/cleanup.json` records the verified restoration.
