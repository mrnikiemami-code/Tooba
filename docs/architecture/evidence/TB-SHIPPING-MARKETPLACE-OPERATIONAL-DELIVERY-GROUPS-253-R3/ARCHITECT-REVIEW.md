# ARCHITECT REVIEW — TB-SHIPPING-MARKETPLACE-OPERATIONAL-DELIVERY-GROUPS-253-R3 (limited correction)

Date: 2026-11-08
Verdict for this chain: **R3 closed as PARTIAL for end-to-end recovery/reuse; corrected by 253-R4.**

## Accepted from R3 (do not redo)

- Marketplace responsibility is no longer treated as physical origin; origin comes only from the committed
  dispatch-origin context/address, and Marketplace-owned groups without it fail closed.
- Allocation identity is the prebound occurrence id, not a payload hash: one occurrence replays its recorded ids and
  an identical **next** occurrence still allocates distinct parcels.
- Whole-plan preflight (unknown / duplicate / foreign reuse / over-quantity) refuses before the first write.
- Canonical FE3000 → Host5088 / SingleStore / store-alpha / tooba_alpha deployment and the read-only live order
  review (`01a1b92d-…`, 3 Store groups, no Marketplace claim).

## Not delivered by R3 (why it was PARTIAL)

1. **No complete immutable intent record.** The server kept only constituent `ShippingDispatchKey`s, so a failure
   after member A could let the same occurrence change member B's quantities — and a change limited to the reuse
   set was not covered at all. A reload could also recompute selections from current remaining quantities.
2. **Reuse-only still reached the create binder.** A member with reuse ids and zero selections was resolved by
   `AcceptedGroupShipmentCreateBinder`, which could invent/substitute selections or refuse an exhausted member, and
   the execution branch never added the reviewed reuse ids.
3. **The reviewed client request did not survive uncertainty.** Only the occurrence GUID was stored, so a lost
   response/remount could not offer reconcile of the exact envelope, and a partial-progress card with create no
   longer ready had nothing authoritative to replay.

## Corrected by 253-R4

Additive durable envelope claimed before the first allocation with typed conflicts on any envelope change;
explicit `Create`/`ReuseOnly`/`Replay` plan kinds with group-scoped reuse ownership; frontend persistence and replay
of the complete prebound attempt per checkout + viewer scope + group. Evidence:
`docs/architecture/evidence/TB-SHIPPING-MARKETPLACE-OPERATIONAL-DELIVERY-GROUPS-253-R4/`.
