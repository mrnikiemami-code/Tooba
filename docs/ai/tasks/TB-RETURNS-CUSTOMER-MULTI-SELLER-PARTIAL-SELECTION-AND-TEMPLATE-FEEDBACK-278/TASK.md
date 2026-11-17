# TASK — TB-RETURNS-CUSTOMER-MULTI-SELLER-PARTIAL-SELECTION-AND-TEMPLATE-FEEDBACK-278

Bridge protocol: `BRIDGE-WAKE-V1`, channel `tooba-main`, Bridge task row
`650b8b3d-83d1-4d39-a8fb-1b9be2640416` (`Pending` at wake, created `2026-11-17T14:20:46Z`).
Expected-Base-SHA == Base `1dc7494f47a416d6bd65b9dce9b4eda971476782`.

## Provenance of this record (declared)

The Bridge row exposes only `id, channelId, taskId, status, createdAtUtc, claimedAtUtc, completedAtUtc`; it carries **no
`content` field**, so the downloadable `.task.md` envelope text is not retrievable through the Bridge API. This `TASK.md`
is therefore a **declared reconstruction** from the issued record that *is* durable in the repository:

- `docs/planning/returns/RETURNS-MARKETPLACE-COMPLETION-PLAN-278-279.md` (Delivery 278 section and its exit criteria),
- `docs/architecture/evidence/TB-RETURNS-CUSTOMER-MULTI-SELLER-PARTIAL-SELECTION-AND-TEMPLATE-FEEDBACK-278/SOURCE-AUDIT.md`,
- the "User clarification — template feedback and decision correction" block in `AGENTS.md`,
- the task identity itself (`…MULTI-SELLER-PARTIAL-SELECTION-AND-TEMPLATE-FEEDBACK-278`).

No Architect envelope sentence is invented: every scope item below is a sentence already present in those durable
sources. Nothing outside 278 was executed (279 stays planned, NOT ISSUED).

## Issued scope (as reconstructed)

**A — authoritative customer eligibility per seller/line**
Authenticated owning-customer eligibility per seller order and line: delivered quantity, prior returned quantity, exact
remaining returnable quantity, the line's own return-window deadline, and the line's unit quantum/blocker. No return
permission may be inferred from shipped quantity or the aggregate order/fulfillment status.

**B — one proper shared selection modal over checkout seller groups**
One shared modal that shows every seller order of the checkout with the owning authority's per-line facts: real product
label, unit label, exact remaining ceiling, the line's own accepted precision. Deliberate item + quantity + reason
portions, no preselect, no full-order default, exact decimal bounds, correct/unselected items untouched.

**C — one frozen submission, stable child identities, honest partial recovery**
Customer submission is frozen before the first send as discrete children keyed by `(sellerOrderId, orderLineId,
reason)`, each with its own stable idempotency key and verbatim request bytes; exact replay on retry; a recorded child is
terminal and never re-sent; a possibly-sent child stays unresolved after any refusal that is not dictated by its own
immutable body; partial successes are visible and only the unresolved children are retried.

**D — server-side creation integrity**
Creation enforces the owning unit quantum and the authoritative remaining capacity server-side before any row is
written; a foreign seller / foreign customer / foreign line fails closed; a fractional whole-piece quantity is refused
with a typed error.

**E — actual template feedback (react-toastify)**
Returns command surfaces use the application's existing `react-toastify` template (`toast.success` / `toast.error`
through the single container installed by `app/providers.tsx`), never the passive design-system `ToastRegion` and never
a second container.

**F — proof, not fixtures**
Controlled two-seller/same-checkout owning proof, mounted modal proof, partial-delivery visibility, failure/retry/
retarget tests, focused builds and served-artifact proof. **No** business fixtures, provider/money/stock calls or schema
change; the user's exercised cases stay unchanged and read-only.

## Explicitly out of scope

- 279: audited pre-money reconsideration of `Approve`/`Reject`, legacy multi-item partial-case authority, per-delivery-
  slice expiry allocation, rejected-but-received physical obligations, refundable-amount policy. Not designed, not
  implemented, not migrated here.
- No migration/DDL/seed/backfill/reset/cleanup, no new live return case, no provider/PSP/Wallet/Inventory call.
- One worker, one task, no delegation, no historical subagent resumed, no screenshots, no automatic next implementation.
