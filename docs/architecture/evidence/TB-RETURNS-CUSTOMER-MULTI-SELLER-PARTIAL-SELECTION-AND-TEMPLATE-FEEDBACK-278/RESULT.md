# RESULT — TB-RETURNS-CUSTOMER-MULTI-SELLER-PARTIAL-SELECTION-AND-TEMPLATE-FEEDBACK-278

Status: **PASS within the issued reconstructed 278 scope** (see `TASK.md` provenance: the Bridge row carries no envelope
text). Decision:

```
ONE_AUTHORITATIVE_PER_LINE_CUSTOMER_ELIGIBILITY_READ_ONE_OWNING_UNIT_QUANTUM_GATE_AT_CREATION_ONE_FROZEN_MULTI_SELLER_SUBMISSION_OF_STABLE_KEYED_CHILD_CASES_AND_THE_APPLICATION_OWN_REACT_TOASTIFY_TEMPLATE_ON_THE_EXISTING_RETURNS_SURFACES_WITH_ZERO_MIGRATION_AND_ZERO_LIVE_BUSINESS_WRITE
```

**NOT Architect ACCEPT, NOT user functional/visual acceptance, NOT Marketplace/customer Returns readiness.**
Base `1dc7494f47a416d6bd65b9dce9b4eda971476782` (== Expected-Base-SHA). Bridge task row
`650b8b3d-83d1-4d39-a8fb-1b9be2640416`. `279` remains planned and NOT ISSUED. 277 / 277-R1 / 277-R2 bounded acceptances
and every earlier Returns/shipping acceptance are preserved unchanged. Single worker, one task, no delegation, no
historical subagent resumed, no screenshots, no automatic next implementation.

| Area | State | What this means |
| --- | --- | --- |
| A eligibility read | one owning authority | `GET /v1/customer/returns/eligibility/{sellerOrderId}` (`GetCustomerReturnEligibilityQuery`) binds the checkout's owning customer **before** any fact is returned (`return.missing` / `return.not_owner`), then answers each line with the SAME `IReturnEligibilityEvaluator` the create command runs: `DeliveredQuantity`, `AlreadyReturnedQuantity`, `RemainingReturnableQuantity`, `ReturnableUntil`, `BlockerCode`, plus Catalog variant title, stored unit code and the accepted `QuantityDecimalPlacesSnapshot`. An unanswered precision is `unit_precision_unavailable` (never six decimals). No shipped/full quantity and no aggregate `Delivered` permission is used. `Selectable` is `false` for a blocked, exhausted or unknown-precision line |
| A evaluator truthfulness | blocker + deadline | `ReturnLineEligibility` gains two additive trailing optional members; an expired window is `window_expired` (not "nothing returnable"), an exhausted ceiling is `nothing_returnable`, a non-returnable/delivered-none line keeps its own reason, and `ReturnableUntil` is derived from the line's own delivered slice(s) and stored policy window — never the browser clock or the order status |
| B creation quantum gate | before any row | `ReturnDirectory.Creation.cs` re-reads eligibility under the existing per-seller-order serialization, enforces the remaining ceiling and then `OwningUnitQuantity.IsCreationQuantityAcceptable(quantity, line.QuantityDecimalPlacesSnapshot)`: `0.5` on a whole-piece line is `return.quantity_unit_invalid` (400 Business) with **zero writes**, a 3-decimal gram line accepts its own quantum, and an unanswered precision still refuses a fraction while keeping a whole quantity lawful. The exact stored-occurrence replay is answered **before** this gate, so a committed child is never re-described as invalid. New code `return.quantity_unit_invalid` is catalogued and localized fa/en |
| C customer selection UX | deliberate, nothing preselected | the old `ReturnFormModal` (all `quantityShipped`, one case reason, no key, text-only summary) is **deleted**; `app/returns/return-request-modal.tsx` is one modal over the checkout's seller orders showing real product + unit + «حداکثر قابل دریافت» + the line's own step/precision, per-line reason, exact decimal bounds via the existing millionth helpers, no preselect and no auto-fill, blocked lines explain themselves; a checkout with no eligible line offers no selection |
| D child cases | one per seller+line+reason | `app/returns/return-submission.ts` freezes ONE act into children keyed `(sellerOrderId, orderLineId, reason)`, each with its own stable idempotency key and its own verbatim request bytes, written and verified in storage **before** the first send; a recorded child is terminal and never re-sent; `SUBMISSION_NO_EFFECT_REFUSALS` (6 body-dictated tokens) is the only proof of no effect — every other refusal keeps the child `unknown`; a partial success keeps its successes and retries only unresolved children; no implicit clear, only the exact current act clears once every child is resolved; unreadable storage is an explicit state and refuses to send |
| E template feedback | react-toastify | `admin-return-workspace.tsx` drops `ToastRegion` for `toast.success` / `toast.error` from `react-toastify` (the container already installed by `app/providers.tsx`); the customer modal uses the same template for command outcomes and concise inline validation for field errors. No second container, no new provider, no custom floating box. No seller-facing Returns mutation surface exists in this app to convert (declared) |
| F order page | line facts, not status | `customer-panel/orders/[checkoutId]/page.tsx` loads `loadCustomerReturnEligibility` per seller order and offers the modal from those facts, so a partially delivered seller order with an eligible line is actionable; each child case is listed with its own case number/state |
| schema / data | untouched | zero migration/DDL/seed/backfill/reset/cleanup/restore; one canonical `tooba_alpha`; no live return case created; no payment/order/shipment/stock/hold/provider/notification write; both user-exercised cases remain read-only settled history |
| proof | focused | Returns `148/148`; AMSC architecture guards `34/34`; FE submission+mounted `11/11`; `return-api` `7/7`; `tsc` 0; `next build` 0 (`BUILD_ID 0chXf5Nh9mayZ6KZAKbUC`); backend Release build 0 errors; Host publish 0 errors |
| served | refreshed | Host `5088` PID `31444`, dll SHA256 `292C479DE80B8B21CEFDA693BBE293E972F86F5789D4A59A18CC9A665406BC27` (`/health` 200, `/health/ready` 200 SingleStore + messaging Healthy, `/__platform-commerce` → `store-alpha`); Next `3000` PID `45076`, `BUILD_ID 0chXf5Nh9mayZ6KZAKbUC`, route chunk `page-7821fa6a631ec3c4.js` 200 with the modal copy, shared `9627-0b0f29163bba673b.js` 200 |

## Known limits

- The mounted modal suite is jsdom with a scripted HTTP boundary: it proves selection/validation/submission/recovery
  behaviour, not browser geometry, RTL pixel layout or a real customer session.
- The customer payment/checkout path was not exercised live: no live customer cookie session was used, so the served
  proof is the route's typed `401 customer.session.required`, the served client chunk, and the mounted component.
- Money/stock/provider are untouched by design; no provider/PSP readiness is claimed.
- The read-side `Selectable` is deliberately conservative: a line whose owning precision did not answer is presented as
  blocked even though the write path would still accept a whole quantity — the UI never offers a quantity it cannot
  bound.
- A same-line multi-reason selection is two children sharing the server's one remaining ceiling; the server remains the
  final authority and a second child can be lawfully refused by capacity.
- No delivery-slice allocation, no decision reconsideration and no legacy multi-item partial authority: those stay 279.

## Declared deviations

1. The Bridge envelope content is not retrievable (row exposes no `content`), so `TASK.md` is a declared reconstruction
   from the durable 278–279 plan, the SOURCE-AUDIT and the AGENTS clarification block.
2. `ReturnsCreateIntegrity272Tests` needed its fractional-scale fixture to declare a 6-decimal owning unit, because a
   whole-piece line now lawfully refuses that fraction at creation (the TB-278 gate). The assertion's intent (the
   persisted scale accepts a representable fraction) is preserved, not weakened.
3. The three AMSC pins were extended by this task's own additions (15 routes, 15 dispatches, 45 → 46 HTTP-reachable
   error codes) and `GetCustomerReturnEligibilityQuery` was added to the documented `NoValidatorRequired` list because
   the route takes only the owning customer and the bound seller order id.
4. `ReturnEligibilityEvaluator` line construction gained the additive `ReturnableUntil`/`BlockerCode` facts; existing
   consumers keep compiling and the aggregate `Eligible`/`ReasonCode` semantics are unchanged.
5. Host was restarted from the fresh publish on `5088`; Next was rebuilt and restarted (`BUILD_ID` changed). Both were
   already-running development processes on the canonical ports, not new deployments.
6. Untracked `scripts/tb27*.mjs` probe scripts and `src/backend/Host/Tooba.Host/App_Data/dataprotection-keys/` were
   already present/emitted by the running Host and are not part of this change set.

STOP_FOR_ARCHITECT; `automaticNextImplementationTask: NONE`.
