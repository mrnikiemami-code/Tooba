# Marketplace/customer Returns source audit — 2026-11-17
Base5a6c5f82118c366005425fa717ef1e0ffd1a06b6; Architect source inspection only, no live data write/test run.
Read docs/planning/returns/RETURNS-MARKETPLACE-COMPLETION-PLAN-278-279.md for concrete delivery/boundaries.

Confirmed source anchors:
- src/frontend/app/returns/return-ui.tsx: ReturnFormModal241–258 defaults all quantityShipped;273–285 posts all positive rows with one Reason, no passed idempotency key;322–331 is a text summary without selection controls.
- src/frontend/app/customer-panel/orders/[checkoutId]/page.tsx:169–175 opens modal only on aggregate Delivered, passes one seller order's fulfillment items.
- src/frontend/app/returns/return-api.ts:850–875 accepts line/quantity only and generates a new random key when caller omitted it.
- src/backend/Modules/Returns/Tooba.Returns.Endpoints/Customer/ReturnCustomerEndpoints.cs: one SellerOrderId+Reason+Items per request; authenticated customer.
- Returns.Infrastructure/Directories/ReturnDirectory.Creation.cs: owner validation, per-SellerOrder advisory serialization, exact replay before new eligibility; item lookup only inside owning seller order; storage precision and remaining ceiling but no owning unit gate.
- Order.Infrastructure/Integrations/Returns/OrderReturnBridge.cs: accepted line quantities/unit precision/policy facts belong to that SellerOrderId and checkout customer.
- Returns.Infrastructure/Evaluators/ReturnEligibilityEvaluator.cs: BuildLines calculates stillInWindow minus all alreadyReturned; no persisted allocation from ordinary return to delivery slice.
- Returns.Application/ReturnRequests/Commands/{ApproveReturn,RejectReturn}Command.cs: whole case decision. ReturnRequest.RefundAmount = sum(UnitPriceSnapshot*Quantity). No per-line requested resolution/reason in ReturnLineCommand.
- src/frontend/app/providers.tsx: actual Shopeiva common ToastContainer is react-toastify/top-right/RTL/colored3000ms; app/layout.tsx installs it. Current Returns workspace uses passive DS ToastRegion, so a shared DS import was not proof of template compliance.
- src/frontend/features/admin-shipping-authoring/tb261-notification.mount.test.ts: already proves ONE toast.success per actual command, no toast on GET/reload. Reuse this convention.
- Returns.Infrastructure/Directories/ReturnDirectory.Decisions.cs: serialized immutable decision row. It protects approve-vs-reject concurrency; it is not a user-approved business rule that a no-money decision can never be reconsidered.

User images record second exercised case01a1e9c5-c729-7000-b614-79daf3d0e812: receipt damaged1, approval as separate step, start-first-refund separately, then recorded success+non-saleable disposition. UI sequence is improved. It does not test rejection/reconsideration, multi-seller selection, shared-checkout real PSP or all customer states.
All such cases remain READ-ONLY for implementation. No reset/clone/new demo case or money/stock/receipt action authorized.
No production code changed by this audit checkpoint.
