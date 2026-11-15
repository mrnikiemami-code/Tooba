# RESULT — TB-SHIPPING-RESEND-REPLACEMENT-OWN-DISPATCH-PLAN-270-R3

Pipeline: `BRIDGE-WAKE-V1` · Channel: `tooba-main` · Worker: `tooba-worker-01` · Program: TOOBA
Mode: `REPAIR_EXACT_CAPTURE_BINDING_LIVE_HOLD_AND_IMMUTABLE_PLAN_RECOVERY`
Parent: `TB-SHIPPING-RESEND-REPLACEMENT-OWN-DISPATCH-PLAN-270-R2` (PARTIAL — checkpoint persisted in `R2/ARCHITECT-REVIEW.md`)

Status: **PASS within the issued R3 repair scope**
Decision:
`EXACT_CAPTURED_GROUP_AND_RATE_ROW_IDENTITY_ONE_SHARED_PLAN_BINDING_ON_GUARD_PACKAGE_PROJECTION_AND_BULK_PREFLIGHT_INVENTORY_OWNED_REPLACEMENT_HOLD_READ_AND_A_FROZEN_SERVER_FINGERPRINTED_REVIEW_WITH_EXPIRED_SLOT_RECONCILIATION_EPOCH_FENCING_AND_CREATE_TO_PLAN_HANDOFF`

Not Architect ACCEPT. Not user visual/functional acceptance. Not a carrier/provider/waybill/customer-consent/financial claim.

---

## 1. What this increment repaired

R2 delivered the two-stage parcel (Fulfillment `Created`) + one immutable Order-owned plan, but left five concrete
seams. Each is repaired here, and each repair has a focused proof.

| # | R2 seam | Repair landed | Where |
| --- | --- | --- | --- |
| A | The reader could address the captured revision with the **Order** shipping-group id, so the "captured" lookup was not provably the same group the rate row was captured for | the plan reads the capture with `binding.AreaGroupId` (captured area group) and refuses anything else; the Order-side `ShippingGroupId` is only ever the Order identity. A real connected test uses **different** ids for both, and a captured answer for the Order group is a typed `binding_mismatch` | `AdminOrderOperationsOrchestrator.ShipmentPlan.cs:369,388` |
| B | The dispatch guard, the package member guard, the queue projection and the bulk path each decided "planned" in their own way | ONE Contracts comparison `OrderShipmentDispatchPlanBindingRules.Validate` (parcel, checkout/order/fulfillment/group, attempt kind, source/decision/hold, canonical line fingerprint, method, internal coherence) is used by all readers; the store now arbitrates only **its own** unique index by constraint name | `Tooba.Order.Contracts/Shipping/OrderShipmentDispatchPlanBindingRules.cs`, `OrderShipmentDispatchPlanStore.cs` |
| C | Replacement readiness was inferred from an Order-side projection of a decision status, and the 270-R1 seam did not name the live hold | an owning Fulfillment read `IFulfillmentReplacementReadinessReader` verifies the unit, the replacement parcel, its source, ONE decision in `Reserved|Fulfilled`, the exact line/quantity and Inventory's **current** `Held` reservation, writing nothing. Order depends on the Fulfillment-owned contract; there is no Order→Inventory call | `FulfillmentReplacementReadinessContracts.cs`, `FulfillmentDirectory.Readiness.cs` |
| D | The review could drift between preview and confirmation; a refused/ambiguous confirmation could not be resumed byte-for-byte | the preview issues a server-computed `ReviewFingerprint` over the reviewed owning facts; confirmation must echo it unchanged, a changed context is `order.shipment_plan.review_context_changed` with zero write, an expired slot is `order.shipment_plan.selection_expired` (never clamped), and the dialog freezes the exact serialized envelope and re-sends it byte-for-byte after an unknown outcome (epoch-fenced against late responses) | `AdminOrderOperationsModels.cs`, `AdminOrderOperationsOrchestrator.ShipmentPlan.cs:783–929`, `admin-shipment-plan-modal.tsx`, `admin-shipment-plan-attempt.ts` |
| E | Create/recover returned a parcel that the operator then had to find again by hand | the create response resolves the exact created parcel and hands it to the plan surface for that same parcel; pending/referral/completed-replay outcomes never open a calendar | `features/order-operational-groups/created-parcel.ts`, `admin-order-items-shipping-panel.tsx`, `post-dispatch-outcome.ts` |

## 2. Focused proofs (each executed once, after the final code change)

| Scope | Suite | Result |
| --- | --- | --- |
| Order unit | `AdminShipmentPlan270R2Tests` | **25/25** |
| Order relational (real `tooba_alpha`, rolled back) | `AdminShipmentDispatchPlan270R2RelationalTests` | **2/2** |
| Queue projection (own plan / sibling / unplanned) | `AdminFulfillmentOwnPlanProjection270R2Tests` | **3/3** |
| Unique-index arbitration filter (new class) | `OrderShipmentDispatchPlanStoreFilter270R3Tests` | **3/3** |
| Fulfillment dispatch guard | `DispatchPlanGuard270R2Tests` | **7/7** |
| Fulfillment replacement readiness (new class) | `ReplacementReadiness270R3Tests` | **4/4** |
| Fulfillment bulk preflight (3 new cases) | `AdminFulfillmentBulkTests` | **12/12** |
| Shipping captured seam | `ShippingAttemptSchedulingLookupTests` | **8/8** |
| FE mount — plan dialog | `admin-shipment-plan-270r2.mount.test.ts` | **10/10** |
| FE mount — operational delivery/panel | `admin-order-operational-delivery.mount.test.ts` | **74/74** |
| Typecheck | `tsc --noEmit` | **0** |
| FE build | `npm run build` | **0**, `BUILD_ID iXZVso1qBFzB4wDa0huHP` |
| Backend build | `dotnet build Tooba.slnx -c Release` | **0 errors** (243 pre-existing warnings) |
| Host publish | `dotnet publish … -o artifacts\publish-primary` | **0 errors** |

The two new cases that were added because of a concrete introduced seam, not for green coverage:

- `OrderShipmentDispatchPlanStoreFilter270R3Tests` — the store must arbitrate ONLY
  `ux_shipment_dispatch_plans_shipment` with SQLSTATE `23505`; any other 23505, another constraint name, or a
  completed row is an unexpected failure, never a silent replay. The relational case asserts the same through the
  real DB constraint.
- `ReplacementReadiness270R3Tests` — a released/consumed hold, a mismatched quantity, a decision for another line and
  a non-replacement parcel each answer their own typed reason; a genuinely `Held` reservation for the exact decision
  line is ready. All four assert the reader performed **zero** release/consume calls beyond the seeding baseline.

## 3. Runtime / environment

- ONE canonical `tooba_alpha` @ 127.0.0.1:5432 (SingleStore, tenant `store-alpha`), Host 5088, Next 3000.
- **No migration was added or applied** in R3: the schema is exactly R2's `order 20261115120000_AddShipmentDispatchPlans`.
  No DDL, no reset/reseed/backfill, no manual business SQL.
- Host republished to `artifacts\publish-primary` and restarted: listener **PID 43312**, `/health` 200,
  `/health/ready` 200 → `SingleStore / postgresql / messaging Healthy`; `/__platform-commerce` reports the unchanged
  Development provenance tuple.
- FE rebuilt and the new build served: previous server stopped, new Next on 3000 (**PID 620**), `/fa` 200,
  `_next/static/iXZVso1qBFzB4wDa0huHP/_buildManifest.js` 200, chunk `9424-518a110b21e84091.js` 200 carrying the new
  review/recovery markers.
- No provider call, no waybill, no notification send, no money movement, no user-data mutation.
- **No live plan row was created.** The only writes in this increment happened inside rolled-back relational
  transactions; there is no live eligible `Created`/unplanned resend-or-replacement parcel in the current data, and no
  example order, parcel, hold or reservation was manufactured to produce one.

## 4. Disclosed limits (not papered over)

1. **R2 pre-apply backup is still absent.** The additive `shipment_dispatch_plans` migration was applied in R2 before a
   private dump existed; only a post-apply dump exists
   (`D:\temp\tb270r2-backup\tooba_alpha-post-tb270r2.dump`, 2,949,234 B). Persisted in `R2/ARCHITECT-REVIEW.md`.
   R3 adds no schema change, so nothing new depended on that missing artifact.
2. **The R2 relational suite was a point-in-time factory fixture.** Its hand-built captured binding could not prove the
   real 270-R1 seam. Repair A closes that with the real seam; the fixture limitation is stated rather than re-labelled
   as end-to-end proof.
3. **Concurrency is proven by the unique index plus independent contexts**, not by a stress harness.
4. **No live operator click-through.** The current data holds no eligible parcel, so the review URL is completed
   read-only history; the actionable path is proven by the mounted suite and the owning-application cases.
5. **Bulk dispatch has no separate test class beyond its preflight cases**; bulk runs the same single-dispatch guard.
6. The workspace clone carries pre-existing untracked scratch scripts (`scripts/tb266r1-*.mjs`, `scripts/tb270r2-*.mjs`);
   they are NOT part of this change set and were not staged.

## 5. Non-claims

Worker PASS is not Architect acceptance. No user visual/functional acceptance, no carrier booking, no
provider/waybill, no customer notification or consent, no new financial fact, no universal "shipping complete"
claim. `271` remains NOT ISSUED.
