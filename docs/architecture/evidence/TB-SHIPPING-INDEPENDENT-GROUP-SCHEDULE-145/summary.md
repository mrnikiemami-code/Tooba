# Evidence — TB-SHIPPING-INDEPENDENT-GROUP-SCHEDULE-145

Mode: `BOUNDED_CONTRACT_GAP_AND_APPROVAL_WIREFRAME` — **DESIGN ONLY** (no production activation, no UI edits,
no migrations, no data writes, no shared `:5088`/`:3000`).

## Deliverables
| Artifact | Purpose |
|---|---|
| `docs/planning/shipping/SHIPPING-GROUP-SCHEDULE-WIREFRAME-145.md` | RTL textual wireframe + component-state map (Mission B) |
| `docs/planning/shipping/SHIPPING-GROUP-SCHEDULE-IMPLEMENTATION-145.md` | Source-grounded code gap map + 3 bounded increments (Mission A + C) |
| `task-receipt.txt` / `task-receipt.json` | Persisted received Task artifact + claim row (UNSTAGED) |
| `RESULT.bridge.txt` | Canonical Worker Result envelope (UNSTAGED) |

## Contracts read first
- `docs/architecture/shipping/SHIPPING-GROUP-INDEPENDENT-DELIVERY-COMMITMENT.md` (supersedes one-global-date and
  name-based merging)
- `docs/architecture/checkout/SHIPPING-METHOD-GROUPING-UI-CONTRACT.md`
- `docs/architecture/shipping/SHIPPING-ADMIN-FLAT-METHOD-CONTRACT.md`

## Root finding (source-grounded)
The stable Order model **already persists per-group** schedule fields, but the **read/write path and the UI
impose one global date/time**:

1. **UI/global state** — `storefront-shipping.tsx` L135-136 keeps ONE `deliveryDate`/`deliveryTime`; the
   `زمان تحویل` card L912-1002 renders ONE date row + ONE window row for the whole page.
2. **Cross-group intersection** — `storefront-shipping-group-horizon.ts` `deriveGroupDateHorizon` L38-78
   computes intersecting dates/windows and the latest minimum; `distinctSelectedMethodCodes` L137-144 keys
   fetches by **method code only**, so two sellers selecting `tipax:standard` share one horizon.
3. **Global write** — `StorefrontShippingService.SaveSelectionAsync` L269-280 validates ONE date against the
   **max** minimum; `CartShippingGroupPlanner.Plan` L40-146 takes scalar `selectedDeliveryDate`/
   `selectedDeliveryTimeWindow` and writes the **same** value into every `CartShippingDraftGroup` (L103-104).
4. **Unused capability** — `CartShippingDraftGroup.ApplySchedule(...)` (L287-293) exists and is never called;
   `CartShippingDraftGroup.SelectedDeliveryDate`/`SelectedDeliveryTimeWindow` (L160-166) and
   `OrderShippingGroup.RequestedDeliveryDate`/`RequestedDeliveryTimeWindow` (L55-61) already exist.
5. **No migration needed for per-group schedule** — `20261016100000_AddOrderShippingGroups.cs` already has
   `requested_delivery_date`/`requested_delivery_time_window` (L35-36).
6. **Fulfillment already per-group** — `OrderFulfillmentBridge.LoadGroupsAsync` L68-103 passes each group's
   `RequestedDeliveryDate`/`RequestedDeliveryTimeWindow`.
7. **Hard invariant to revisit** — `CartShippingGroupPlanner` L85-91 throws `DuplicateSellerMethod`, and the
   unique index `ux_order_shipping_groups_checkout_seller_method (checkout_id, seller_party_id, method_code)`
   (L56-57) blocks case D (same seller+method, incompatible origin/promise).
8. **Missing authority** — `StorefrontShippingCalculator.DayTimeWindows` L11-18 is a static global window list;
   `ValidTimeWindows` L148-157; `BuildDeliveryDates` L94-97 has no working-days/timezone; no per-method
   Transport Schedule / timezone / slot owner exists (Admin contract Phase 2, not implemented).
   `OriginContext` is forced to `null` (`OriginUnverified`) in both draft and accepted group creation.

## Wireframe coverage (Mission B)
Two separate cards for **Seller A `tipax:standard` earliest +2d** and **Seller B `tipax:standard` earliest +3d**,
each with its own items, method selection, date picker, time-window picker and fee; plus same-seller-two-methods
and same-seller+same-method-incompatible-promise (reported BLOCKED). Global date selector and cross-group
intersection are explicitly banned. Only existing storefront tokens/markup are reused.

## Plan coverage (Mission C)
Three ≤45m increments (backend per-group read/write + validation; frontend per-card schedule controls with
backward compatibility; one scratch runtime proof), each PASS/STOP. Trusted fixture = per-seller
`SellerPreparationDaysByPartyId` in an approved Development configuration with unchanged `tipax:standard`
`LeadDays` and clock-derived reference date — no invented rates/dates.

## Stop state
`STOP_FOR_ARCHITECT` — WIREFRAME must be approved by the user before any disruptive UI/backend change.
