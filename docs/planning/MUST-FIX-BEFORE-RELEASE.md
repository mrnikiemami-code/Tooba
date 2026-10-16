> **Architect handoff / complete open backlog:** [Shipping Architect Recovery — 2026-10-16](shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-16.md). Specifically, storefront per-seller ShippingGroupId delivery cards and dates/time slots (approved TB-145 wireframe) remain **RELEASE-CRITICAL and NOT IMPLEMENTED**, regardless of Admin improvements. Also tracks multilingual Admin, monetary policy, optional constraints, real create-method, persistent writes, EF log flood and financial policy. Read before issuing tasks.

# TOOBA — Must-fix issues before release

Status: ACTIVE release-blocking register. Owner: Software Architecture.
Last updated: 2026-10-16.
Rule: No issue marked CRITICAL may be silently closed by task completion or documentation. Closure requires reproducible before/after runtime evidence.

## PERF-CRIT-001 — Storefront catalog read N+1 and home page timeout

**Status: OPEN — CRITICAL / RELEASE BLOCKER**

**Affected routes:** storefront Home (`/fa`, `/v1/storefront/home`) and potentially catalog/listing/product-dependent pages. Shipping/login routes show severe latency in the reported environment but are not proven to share the same cause.

**User-provided diagnostic report** (not yet independently reproduced by architect):
- `/v1/storefront/home`: approx. **13.94 s wall time**, **2,604 reported queries**, approx. **9.2 s aggregate DB query time**.
- Repeated lookups named in trace: `party.parties`, `catalog.localized_texts`, `pricing.prices`, `catalog.variants`, `inventory.stock_positions`; suspected per-product N+1 fan-out when composing sellers, prices, inventory and media.
- `/fa`: HTTP **500 after ~30.05 s**, likely proxy/request timeout.
- `/fa/shipping`: HTTP **200 after ~12.7 s**; `/fa/login`: HTTP **200 after ~19.3 s** in that observation.
- EF Core Debug logging reportedly wrote **~26.8 MB for one Home request**, with stdout log **~1.5 GB**; logging/I/O overhead must be measured, not assumed to explain all wall time.
- Report also quotes `FROM party.parties` 5,136 and `~8,000 queries` while describing 2,604 total; counts appear to mix units/scope. **Reconcile telemetry scopes** before treating counts as exact.

**Separate observations, NOT the same root cause:**
- Development Next.js `.next` cache cleanup removed intermittent `TypeError: Cannot read properties of undefined (reading 'call')`, 500/white page and proxy `ECONNRESET` symptom; cache corruption root cause remains unconfirmed.
- `/fa/shipping` redirects anonymous users to `/fa/login?returnTo=/fa/shipping` through `useCheckoutAuthGate`; this is expected authentication behavior, not a confirmed defect. Development OTP fixture must not be enabled in production.

**Required engineering response:**
1. Capture one authoritative per-request trace/command-count breakdown (same request id, identical environment and data), wall time, DB execution vs EF materialization, payload size, backend latency and Next.js proxy timing. Separate logs from actual DB commands; establish a consistent count.
2. Identify the precise module-owned storefront Home reader / product projection and N+1 call sites. Batch reads **per owning module and Contracts-only boundaries** for product, seller, localized text, price, inventory and media rather than per card. Avoid Host business logic, cross-module DbContext access, undocumented cache shortcuts, larger proxy timeouts as a supposed fix, or hiding the issue by decreasing visible product count.
3. Restore production-appropriate EF logging level, avoid sensitive data logging and huge stdout; retain structured diagnostics/targeted performance instrumentation. Do not simply suppress errors.
4. Prove before/after on comparable dataset/routes and confirm product names, offers, prices, availability and seller mapping are identical and tenant-safe.
5. Check Next.js dev `.next` behavior separately if recurring, with reproducible clean-cache vs warm-cache evidence. Never indiscriminately delete shared build artifacts while another process uses them.

**Definition of Done / acceptance (all mandatory):**
- Home page reliably HTTP 200 without 30-second proxy timeout; meaningful performance reduction validated across at least one repeated warm run on same dataset.
- Query count is measured consistently and substantially reduced via bounded/batch accesses rather than number-of-products linear N+1 growth; evidence includes an explicit before/after table and one scale-check.
- EF debug stdout flood stopped and per-request log volume reduced without losing error tracing.
- No changed storefront appearance, removed product content, altered prices/stock, security/auth/tenant regression or Checkout/Shipping behavior change.
- Architect reviews measured numbers and user validates relevant page responsiveness before changing status to CLOSED.

**Scope/priority:** Release-blocking performance defect tracked separately from current Admin Shipping flat-method visual milestone; do not derail current Admin phase-1 approval, but must fix before production release. Cursor tasks maximum 45 minutes, narrow focused tests, STOP_FOR_ARCHITECT.

## ADM-UI-001 — Shipping Admin flat-method UI is not yet delivered

**Status: OPEN — USER VISUAL APPROVAL REQUIRED**
Authority: [Shipping Admin flat method contract](../architecture/shipping/SHIPPING-ADMIN-FLAT-METHOD-CONTRACT.md).
Current `/admin/shipping-services` uses provider-level rows, redundant actions and scattered nested configuration forms. Phase 1 must deliver independent method-level list and exactly View/Edit/Delete per row plus one service detail view with top-level General, Rates & Coverage, Transport Schedule, Packaging & Fee tabs, using existing Admin style. **DO NOT** migrate scattered forms until user approves actual visual skeleton.

## FIN-POL-001 — Group shipping commercial allocation

**Status: OPEN — BUSINESS APPROVAL REQUIRED**
Authority: [Shipping group fee allocation policy](../architecture/checkout/SHIPPING-GROUP-FEE-ALLOCATION-POLICY-DECISION.md).
Shipping charge group rows persist as PendingAllocation; carrier payable, seller/platform shares, taxes, discount funding, adjustments and refund/reversal authorization remain unsigned. Never invent policy or activate payout.

