# TOOBA Offer completion — master plan
Status: ACTIVE roadmap / NOT COMPLETE. Owner authorized roadmap registration, not execution of every future task.
Baseline: opencode/iso-agents-002 @ da090324b45f3d55dba31c275bc859d98885c7ac (TB-296-R8).
Gate: STOP_FOR_ARCHITECT after each issued task; no automatic next task. Task identifiers below are reserved planning labels, not executable task packages.

## 1. Decisions and current state
- Preserve the shared seven-tab single-offer workspace and both existing AppDataGrid lists. R8 is PARTIAL / READY_FOR_OWNER_REVIEW, not final acceptance. Owner confirmed visual improvement and requested shared system Toast instead of fixed success banner.
- A Catalog product/variant is not an Offer. Each seller + variant + channel retains an independent offer, authored price, stock positions and commercial policy. A product-level bulk workspace is an editing surface, not a new aggregate or duplicated source of truth.
- Pricing owns the seller-authored base price. Promotion owns seller-funded discounts and marketplace-funded discounts, including shared campaigns. Never overwrite authored base price with a campaign price or merge funding into an unattributed total. An optional validated reference/list price is distinct; its ownership and validation remain a policy decision.
- Owner requirement: if seller contributes 15% and marketplace contributes 5%, customer gets 20% off the same approved base, not sequential 19.25%. Marketplace share may be funded from its commission; seller and marketplace shares must remain separate in quote, order, settlement and reversal. Percent and fixed money discounts; row-level values and column/bulk application; do not confuse UI bulk scope with order-level promotion allocation.
- Commission example ONLY, not an approved universal basis: base 1,000,000; seller discount 150,000; marketplace discount 50,000; customer pays 800,000; assuming commission of 100,000 on base, seller receivable 750,000 and marketplace net 50,000 (excluding tax/shipping/fees). Commission basis, rounding, tax interaction, subsidy limits, stacking, price floor, effective date, refund allocation and consent must be explicitly decided before implementation.
- Existing conceptual funding model: docs/architecture/22-promotion-discount.md §Q, R, Z, AB. Implemented PromotionDefinition and PromotionEvaluationResult carry discount amount/targeting but no independent funding attribution in inspected path. OrderLine snapshots total discount; SettlementDirectory credits payment allocation and SettlementEntry calculates commission on gross; refund path currently loads default policy. Audit full campaign-specific price authority, Cart, Checkout, Payment, Order, Settlement, Returns and any accounting bridges before claiming coverage. Do not infer absence elsewhere from these inspected files.
- Preserve SingleStore/Marketplace edition and channel/owner boundaries. Locale authority is DB language registry GET /v1/admin/languages, not hard-coded fa/en. No unauthorized migrations or live writes.

## 2. Roadmap and dependencies
| ID | Scope | Depends on | State / acceptance gate |
|---|---|---|---|
| TB-296-R9 | Current form repair and owner feedback | R8 | PENDING OWNER REVIEW; focused UX and integrity proof |
| TB-297 | Read-only financial chain audit, policy options, impact map | R8 | PLANNED; architect/owner financial decision gate |
| TB-298 | Promotion funding model and seller/platform/shared campaign | 297 approved policy | PLANNED; owning-module tests and funding invariants |
| TB-299 | Cart/Checkout/Order quote and immutable allocations | 298 | PLANNED; end-to-end quote/order integrity |
| TB-300 | Commission, settlement, payment reconciliation and returns/reversal | 299 | PLANNED; ledger reconciliation and historical policy proof |
| TB-301 | Connect single-offer discount tab to real contracts | 298–300, R9 | PLANNED; permissions, persistence, no fabricated fields |
| TB-302 | Bulk offer workspace design using OpenDesign runnable Next.js | 297 policy; final approval after 298–300 contracts stabilized | PLANNED; owner approval of runnable TSX reference before production integration |
| TB-303 | Bulk workspace production integration | 302 approved, 298–301, R9 | PLANNED; real data, per-row outcome, parity |
| TB-304 | Integrated commercial/financial certification | 301,303 | PLANNED; independent end-to-end acceptance |

R9 owner review and 297 read-only audit may proceed independently; no simultaneous edits to same surface. Early isolated design exploration may use deterministic fixtures, but must not freeze unsupported controls or authorize implementation. Existing approved 295 visuals are not automatically redesigned. Before each execution create the immutable task package and active OpenSpec change under the existing delivery method. Do not treat this document as a blanket authorization.

## 3. R9 retained backlog
- Shared Design System Toast for success and transient global feedback (3 seconds, no fixed success banner/reflow); inline validation at field; persistent conflict/uncertain result only if action required; localized messages. No parallel notification system. This is a scoped requirement; global policy changes require appropriate review.
- Registry-backed locale in real routes/lists, not just injected fixture; actual third-language isolated route evidence, correct RTL/LTR and formatting, preserve edits across language changes.
- Preserve unsaved shipping fields and draft state; verify all persisted commercial fields and currency on reread; atomic concurrency policy at owning boundary (pre-read alone is not sufficient); unknown outcomes and partial success safe, no blind retry.
- Unit precision/step/rounding from Catalog, not placeholder; existing form tabs and list readability subject to owner feedback.
- Multi-position stock failure proof through real isolated runtime; cleanup in finally, verify permission and data restoration. Existing architecture guard debt recorded separately; never weaken guards.
- Other known contract gaps (history, pricing list/multi-currency, shipping service Stage-3, seller-funded promotion authoring) remain visible and scheduled by actual dependency; do not fake controls or silently mark done.

## 4. TB-297 financial audit deliverables (read-only)
Trace exact source paths, API contracts, ownership, persistence/schema, events, snapshots, calculation and tests for:
Pricing authored/base and campaign price; Promotion definition, campaign membership/authority, evaluator and stacking; Cart revalidation; Checkout quote/tax/shipping and order submission; Order immutable per-line snapshots; Payment allocation; Settlement commission policy selection/ledger; cancellation/restore, partial/full returns, refund and accounting export. Inspect current admin/seller permissions, edition behavior, existing ADRs and known stage-2 tests.
For each capability classify EXISTS / PARTIAL / MISSING / NOT_VERIFIED with evidence and proposed minimum changes. Deliver an impact matrix, financial examples (seller-only, platform-only, shared, fixed and percent, multi-seller, partial refund), and policy decision register. No implementation, migrations, live changes or invented policy in 297.

## 5. Financial contract acceptance requirements
- Separate funding attribution per applied benefit and line: source, funding amount/currency, approved base, promotion id/version, eligibility/consent and validity. Shared funding may have multiple components; their sum equals customer discount. Distinct marketplace funding source (commission or explicitly authorized alternative); budget and caps enforced server-side, including concurrent claims.
- Seller can define only own funded benefit; marketplace controls its own contribution; shared participation requires agreed terms. Owner and channel isolation, permissions and SingleStore behavior proved.
- Order freezes accepted amounts, allocation, commission basis/rate/policy version, seller receivable and marketplace net or sufficient immutable inputs. Do not re-evaluate current campaign or commission on historical order/refund.
- Reconcile customer payable, seller entitlement, commission, subsidy, taxes, shipping and fees by currency. No floating-point money; rounding and residual allocation deterministic. Prevent negative payable and unauthorized over-subsidy.
- Preserve quote revalidation, idempotency, failure handling, cancellation/restore and partial/full refunds, accounting traceability and payment/ledger equality. Existing Stage-2 campaign regressions retained. Migration/data transition plan requires separate approval where applicable.

## 6. Bulk offer workspace design requirements
- Product selected once; show actual published variants and existing offers for seller/channel; distinguish missing, draft, active, suspended, archived. Explicit group creation; never create on open; no duplicate active listing.
- Shared grid with dynamic variant-axis columns, image, seller SKU, status and detail link. Column presets: pricing, inventory, shipping, sales conditions; selection/filter preserved across views. Row editing, selected rows/all filtered scope, column application, copy/paste Excel-style, preview and validation; do not overwrite filtered-out rows unintentionally.
- Pricing: base, optional validated reference (read-only), seller discount amount/rate and resulting seller price, platform contribution (read-only for seller), total discount and final customer price. Price changes and promotions are distinct commands; show commission implications only from authoritative calculation. Bulk percent/fixed price adjustments and discount authoring with explicit rounding/validity.
- Inventory: physical editable by stock location, reserved and available read-only from Inventory; warehouse selector, totals read-only, reason and reserved-floor checks. No arbitrary distribution across locations.
- Unit from Catalog read-only, actual precision/step/rounding; min/max order quantities configurable within policy. Shipping responsibility/type according to current admin-only permissions; service assignment only after Stage-3 contract. Return policy and days according to permission and bounds. Shared settings with row exceptions; do not fabricate inherited defaults.
- Save per owning domain/row with explicit partial results, retain failed drafts, safe retry/idempotency and independent reread. Shared Toast, localization, RTL/LTR, accessibility, mobile responsive, existing AppDataGrid and design system; no parallel grid architecture. Form seven tabs remain available per row.
- Follow docs/architecture/NEXTJS-DESIGN-FIDELITY.md: runnable TSX reference, owner approval, direct source integration, independent Playwright fidelity with negative control and Impeccable audit only. Do not use HTML-only design as new approval artifact.

## 7. Release gates
Each task reports actual PASS/FAIL/NOT_RUN, source/build identity, test logs, runtime evidence and limitations. No worker self-certification. Final integrated cases include seller-only, platform-only, shared 15+5, nonqualifying seller, campaign expiry, price change, stacking, budget race, multi-seller/multi-location, permission negative cases, partial save, duplicate creation, partial/full return and historical commission changes. Final owner visual and functional acceptance is separate from architect acceptance.

## 8. Open decisions / next action
Next recommended executable task: TB-297 read-only audit, only after separately issued by architect; owner continues R8 review. Commission basis, tax/funding order, budget caps, consent/stacking and historical migration are unresolved. Shipping Stage-3, history and multi-currency price read gaps are tracked, not silently closed. No new task launched by registration.
