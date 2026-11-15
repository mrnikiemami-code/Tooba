# RESULT — TB-SHIPPING-RESEND-REPLACEMENT-OWN-DISPATCH-PLAN-270-R2

Status: **PASS** within the issued R2 scope
Decision: **TWO_STAGE_ALLOCATION_THEN_ORDER_OWNED_IMMUTABLE_DISPATCH_PLAN_WITH_TYPED_PREVIEW_FEASIBILITY_EXACT_REPLAY_CONFLICT_AND_A_SHARED_SERVER_DISPATCH_GUARD_ON_SINGLE_BULK_AND_PACKAGE_PATHS_PLUS_OWN_PLAN_QUEUE_AND_DETAIL_ON_THE_EXISTING_OPERATOR_WORKFLOW_WITH_ZERO_LIVE_DATA_CHANGE**
NOT Architect ACCEPT, NOT user VISUAL/FUNCTIONAL ACCEPT, NOT a carrier/provider/waybill booking, NOT customer consent or
notification, NOT any financial fact.

Parent: `TB-SHIPPING-RESEND-REPLACEMENT-OWN-DISPATCH-PLAN-270-R1` (ACCEPT within its additive read-only seam scope; its
checkpoint is now persisted as `../TB-SHIPPING-RESEND-REPLACEMENT-OWN-DISPATCH-PLAN-270-R1/ARCHITECT-REVIEW.md`).
R1's accepted seam is the ONLY source of captured scheduling inputs here: no re-quote, no destination, no measurements,
no current draft, no provider.

| Area | State | What this means |
| --- | --- | --- |
| 1 Invariant | implemented | Fulfillment creates the physical parcel (unchanged); Order confirms ONE immutable own plan for that exact existing `Created` parcel. Between the stages the parcel exists, is visible and recoverable, and is **not** dispatch-authorized. No Order-generated Fulfillment id, no cross-module atomicity claim |
| 2 Persistence | additive | `order.shipment_dispatch_plans`, PK `plan_id`, **unique `shipment_id`**, check constraints for attempt/source agreement, half-open windows, `handoff_date <= receiving_date`, non-negative allowances and `preparation <= departure <= deadline`; old rows untouched, no backfill |
| 3 Readiness | owning facts only | resend = source `Returned` receipt with per-line **usable** quantity cover; replacement = its OWN decision `Replacement`,`Reserved|Fulfilled` with matching reservation id and covering quantity. Anchor = `max(now, readiness_at)`; `CreatedAt` is never a substitute for stock validation |
| 4 Preview | read-only, no invention | feasible customer dates/slots from `ShippingDispatchSchedulingAuthority` with the THREE independent weeks/timezones, one preparation + one transfer allowance, half-open carrier close, MAX transport days; unprovable deadline → slot skipped; failed calendar → the authority's own reason; missing capture → typed blocker with **no** calendar |
| 5 Confirm | immutable + replay | exact replay answered from the stored row before any new feasibility work; changed selection/key → conflict with zero extra write; expired selection refused, never clamped; DB unique index arbitrates two confirmations (`23505` → conflict, exact intent → same row) |
| 6 Dispatch guard | one shared server rule | every linked member of the batch is checked BEFORE any consume/state/event on the single dispatch command and on consolidated-package dispatch (whole package refused); a missing reader refuses instead of defaulting to authorized; first dispatch unchanged |
| 7 Queue / detail | own plan only | linked attempt reads ONLY its own confirmed plan (never the original group plan, never a sibling's); unplanned → `Unspecified`, null promise, `plan_shipment_dispatch` offered and `dispatch_shipment` withheld; detail renders the stored promise and adds the planning action only for an authorized, Created, non-package-locked linked parcel |
| 8 Workflow | existing surfaces | same card, one new button; `AdminShipmentPlanModal` (no new page/wizard); durable envelope written before the POST; refusal retains the exact occurrence; success refreshes the owning read models; stock-pending keeps an honest blocker; 268 tabs/ordinals, Toast/MessageBox, RTL and finance-free view untouched |

## Owner of each fact

Order owns the plan + `IOrderShipmentDispatchPlanReader` (read-only Order rows). Fulfillment owns parcel/hold/decision
and the dispatch gate. Shipping owns calendar/deadline arithmetic and the captured-input read. No `Order → Fulfillment →
Order` chain, no cross-module joins, no DbContext/Domain leakage into Host.

## Verified once (after the final code change)

| Check | Result |
| --- | --- |
| `AdminShipmentPlan270R2Tests` (Order unit) | **17/17** |
| `AdminShipmentDispatchPlan270R2RelationalTests` (real `tooba_alpha`, rolled back) | **2/2** |
| `AdminFulfillmentOwnPlanProjection270R2Tests` (queue projection) | **3/3** |
| `DispatchPlanGuard270R2Tests` (Fulfillment guarded command) | **6/6** |
| FE mount suites `admin-shipment-plan-270r2` + `admin-order-operational-delivery` | **77/77** (`tsx --tsconfig tsconfig.mount-tests.json --test`) |
| `tsc --noEmit` | 0 errors |
| `npm run build` | 0, `BUILD_ID jdEuO1DKPbRL6rVo1vqUG` |
| Host `dotnet publish artifacts/publish-primary` | 0 errors |
| Served build markers | `_buildManifest.js` 200; chunk `9424-91fab6e90a400226.js` 200 carrying both «زمان‌بندی ارسال» and `admin-shipment-plan-slot` |
| Live read-only routes | `/fa/admin/orders/01a1db2f-…/operations` 200, `/fa/admin/fulfillments` 200 |
| Host health | `/health` 200, `/health/ready` 200 |

## Deployment / data boundaries

- ONE canonical `tooba_alpha` (127.0.0.1:5432, SingleStore `store-alpha`), Host `5088`, Next `3000`. No second,
  disposable, Docker or 15433 database; no connection/edition/port/appsettings/keys/`CapturedPreview` change.
- Migration `order 20261115120000_AddShipmentDispatchPlans` applied **once** by the repository `Tooba.MigrationRunner`;
  `plan --tenant store-alpha` afterwards reports Order `currentMigration = 20261115120000_AddShipmentDispatchPlans` and
  `pendingMigrations: []`. Additive table only; no manual DDL, no destructive change, no reset/reseed/backfill.
- Host republished and restarted on 5088 (PID 19240). Next restarted on 3000 (PID 41560) so the changed FE is actually
  served.
- **No live business write**: 0 rows in `order.shipment_dispatch_plans`, no parcel/hold/reservation/stock/decision
  created or modified, no user order/cart/payment row touched (all reads). The relational proof used task-owned rows in a
  rolled-back transaction.

## Disclosed deviations / limits

1. **Pre-apply private backup was NOT taken.** The additive migration was applied by the runner before a `pg_dump` of
   `tooba_alpha` was written; only the runner's `pre-apply-plan.json` existed. A **post-apply** backup was created
   (`D:\temp\tb270r2-backup\tooba_alpha-post-tb270r2.dump`, 2,949,234 bytes, SHA-256 prefix `331B373AD45882D9`). The
   migration only creates one new table and its indexes, so the residual risk is limited and the `Down` migration drops
   exactly that table — but the issued "backup before apply" step was genuinely not met and is declared rather than
   papered over.
2. The configured mount-test invocation is `tsx --tsconfig tsconfig.mount-tests.json --test`; an initial `npx vitest`
   attempt is not the repository's runner and failed on JSX parsing — no source defect, and the suites were then run with
   the configured runner (77/77).
3. No live eligible unplanned parcel exists in canonical data (only a `Delivered` resend), so no live plan row and no
   live positive operator click-through was produced; the write path is proven by the focused unit/relational/guard
   cases plus the mounted workflow. The review URL is read-only completed history.
4. No concurrency stress harness: the race is proven by the `shipment_id` unique index and two independent relational
   contexts, not load testing.
5. No bulk-specific test class: bulk selection dispatch runs the same `DispatchShipmentAsync` guard; the package batch
   guard is covered by its own case.
6. Time-limited selection: an elapsed confirmed promise is displayed as elapsed and never auto-replanned; a new attempt
   needs the existing lawful lifecycle with its own identity.

## Changed paths (high level)

Backend — `Order.Domain/Aggregates/OrderShipmentDispatchPlan.cs`,
`Order.Contracts/Shipping/OrderShipmentDispatchPlanFacts.cs`,
`Order.Application/Admin/Operations/{Commands/PlanShipmentDispatch, Queries/PreviewShipmentDispatchPlan,
Services/AdminOrderOperationsOrchestrator.ShipmentPlan.cs, Ports/OrderShipmentDispatchPlanPorts.cs, Models,
Services/AdminOrderOperationsOrchestrator(.Projection).cs}`,
`Order.Infrastructure/{Admin/Operations/OrderShipmentDispatchPlanStore.cs, Integrations/DispatchPlan/OrderShipmentDispatchPlanReader.cs,
Persistence/OrderDbContext.cs, Persistence/Migrations/20261115120000_AddShipmentDispatchPlans.cs, OrderModule.cs}`,
`Order.Endpoints/Admin/Operations/AdminOrderOperationsEndpoints.cs`;
Fulfillment — `Directories/FulfillmentDirectory(.Packages).cs`, `Contracts/Errors/FulfillmentErrorCodes.cs`,
`Application/WorkQueue/Models/AdminFulfillmentWorkQueueModels.cs`, `Infrastructure/Queries/AdminFulfillmentWorkUnitProjection.cs`,
`AdminFulfillmentWorkQueueQueryEngine.cs`.
Frontend — `app/admin/admin-shipment-plan-modal.tsx`, `admin-shipment-plan-attempt.ts`,
`admin-order-operational-delivery-section.tsx`, `admin-order-items-shipping-panel.tsx`,
`admin-order-operations.ts`, `admin-error-map.ts`.
Tests — the four classes above plus `admin-shipment-plan-270r2.mount.test.ts`.
