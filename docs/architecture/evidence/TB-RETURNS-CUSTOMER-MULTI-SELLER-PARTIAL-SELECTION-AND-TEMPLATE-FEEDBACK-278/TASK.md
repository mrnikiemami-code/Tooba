# TASK — TB-RETURNS-CUSTOMER-MULTI-SELLER-PARTIAL-SELECTION-AND-TEMPLATE-FEEDBACK-278

Bridge: `BRIDGE-WAKE-V1`, channel `tooba-main`, row `650b8b3d-83d1-4d39-a8fb-1b9be2640416`, Expected-Base-SHA ==
`1dc7494f47a416d6bd65b9dce9b4eda971476782`.

The full record lives at `docs/ai/tasks/TB-RETURNS-CUSTOMER-MULTI-SELLER-PARTIAL-SELECTION-AND-TEMPLATE-FEEDBACK-278/TASK.md`.

## Declared reconstruction

The Bridge row exposes only `id, channelId, taskId, status, createdAtUtc, claimedAtUtc, completedAtUtc` — **no
`content`** — so the downloaded `.task.md` text is not retrievable. The task was executed against the durable sources
that define 278:

- `docs/planning/returns/RETURNS-MARKETPLACE-COMPLETION-PLAN-278-279.md` — Delivery 278 bullets and exit criteria;
- `docs/architecture/evidence/TB-RETURNS-CUSTOMER-MULTI-SELLER-PARTIAL-SELECTION-AND-TEMPLATE-FEEDBACK-278/SOURCE-AUDIT.md`
  — the source anchors the delivery had to repair;
- `AGENTS.md` — the "User clarification — template feedback and decision correction" block (react-toastify template,
  seller+line+reason child cases, no whole-order default).

## Scope executed (A–F)

A authoritative owning-customer per-line eligibility (delivered/occupied/remaining/window/unit/blocker, no aggregate
status permission); B one shared selection modal over the checkout's seller groups with deliberate item/quantity/reason
portions bounded by each line's own unit; C one frozen submission with stable child keys, verbatim replay, terminal
recorded children and honest partial recovery; D server-side creation quantum + capacity gate before any row; E the real
`react-toastify` template on Returns command surfaces; F focused owning/mounted/served proof with zero fixtures and zero
live business write.

Out of scope: 279 (decision reconsideration, legacy multi-item partial authority, delivery-slice allocation, physical
obligations owed after rejection, refundable-amount policy), any migration/DDL, any live case, any provider/money/stock
call, seller surfaces that do not exist.
