# TB-SHIPPING-ONE-LEVEL-TABS-081 — evidence summary

**Task:** one-level shipping methods + five-tab management blueprint (READ-ONLY audit + design).
**Mode:** `BOUNDED_READ_ONLY_ARCHITECTURE_AUDIT_AND_IMPLEMENTATION_DESIGN`, timebox 30 min HARD.
**Status:** PASS (bounded audit scope), **STOP_FOR_ARCHITECT** — one genuine architectural fork reported, not guessed.

## Base

- `git fetch origin` performed; `HEAD == origin/main == 36bdcff729def1537cb9f76ea1b0c44622003846`.
- Local was one commit behind (`7316bc31f` → `36bdcff72`, the 071–080 recovery checkpoint, a docs-only
  append). Fast-forwarded, so the audit reads the current shipping checkpoint.
- Pre-existing dirty/untracked paths were **preserved unchanged** (three modified evidence logs plus two
  modified frontend screens, and the untracked `docs/ai/tasks/*` and evidence folders). No
  `RECOVERY_CONFLICT`.

## Deliverable

- `docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-081.md` — the architecture report:
  - **A** identifier/reference inventory for Fulfillment (catalog), Shipping (rates/coverage/packaging/
    schedule/quote), Order (projection/draft/snapshot/legacy checkout) and the frontend admin surface, with
    file paths and line anchors, plus the live Development catalog identity.
  - **B** one-level identity proposal: hidden internal `provider_kind` grouping, frozen composite method codes
    for all six existing identities, and the two alternatives (A = flatten, no new table; B = new Fulfillment
    table) left as the explicit Architect decision.
  - **C** the five-tab interaction plan («سرویسها», «تعرفه و پوشش», «بستهبندی و کرایه», «برنامه کاری و
    ظرفیت», «بررسی هزینه») with context bar, deep-link contract, loading/empty/error/denied states,
    save/cancel, RTL/responsive and accessible tab navigation.
  - **D** impact map plus additive migration, dual-read window, default-OFF feature gate and rollback.
  - **E** the five unfinished capabilities (packed measurement, atomic capacity, COD settlement, delivery
    promise, commercial cutover) with owners and an explicit statement that UI work cannot close them, plus a
    bounded **11-task total** roadmap including 081.
  - **F** risks, blockers, minimum targeted tests and the bounded recommended scope for 082.

## Live read-only Development probe

`docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-081/log-live-catalog-identity.txt`

- Target positively identified: `GET /__platform-commerce` → `dev-singlestore` / `store-alpha` /
  `tenant-alpha` (Development). Host **not** restarted, **not** rebuilt.
- Read-only: `POST /v1/auth/login` (existing Development bootstrap admin actor) then authenticated `GET`s for
  the service list, the enabled method tree (fa + en) and per-service rates / packaging-policies /
  operating-schedules.
- Findings that shaped the design:
  - `post` has **2 option rows but only 1 active** ⇒ the storefront offers only `post:standard`.
  - `snapp_courier`, `store_courier`, `in_person` have **zero options** ⇒ their Shipping rules are keyed with
    `option_code = null`; a one-level identity therefore cannot be "the option row".
  - `snapp_courier` is **inactive** and has no rate/coverage/schedule row at all.

## Guardrails observed

- No production code, schema, migration, seed or runtime change. No process restart.
- No Checkout price authority, no paid-order/snapshot mutation, no shadow gate enabled.
- `docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md` not modified.
- Only GETs (plus the identity check and the login call) were issued against Development.

## Blocker reported (not resolved here)

- **B1 (STOP):** Alternative A vs Alternative B — flatten the existing `(service_code, option_code)` pair
  into the one-level list, or create a new Fulfillment `shipping_methods` table. Different permanent schema and
  different ownership; cannot be resolved from source.
- **B2 (STOP):** `checkouts.shipping_method_code` is `varchar(32)` while `cart_shipping_drafts` is
  `varchar(64)`; cap new codes or widen the column — a paid-snapshot compatibility decision.
- The four capability blockers from task 080 remain open with their named owners.

**No automatic task 082. `automaticNextImplementationTask = NONE`.**
