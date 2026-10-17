> **Architect handoff / complete open backlog:** [Shipping Architect Recovery — 2026-10-16](shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-16.md). Specifically, storefront per-seller ShippingGroupId delivery cards and dates/time slots (approved TB-145 wireframe) remain **RELEASE-CRITICAL and NOT IMPLEMENTED**, regardless of Admin improvements. Also tracks multilingual Admin, monetary policy, optional constraints, real create-method, persistent writes, EF log flood and financial policy. Read before issuing tasks.

# TOOBA — Must-fix issues before release

Status: ACTIVE release-blocking register. Owner: Software Architecture.
Last updated: 2026-10-16.
Rule: No issue marked CRITICAL may be silently closed by task completion or documentation. Closure requires reproducible before/after runtime evidence.

## PERF-CRIT-001 — Storefront catalog read N+1 and home page timeout

**Status: OPEN — CRITICAL / RELEASE BLOCKER**
**TB-178 update (2026-10-17): EF-log item ADDRESSED, blocker still OPEN.** The EF Debug/Information command
flood was a pure Host configuration defect (no explicit `Microsoft.EntityFrameworkCore` level, so
`Database.Command` at Information was serialised by the JSON console sink in every environment). Committed
fix: `"Microsoft.EntityFrameworkCore": "Warning"` in `appsettings.json` and `appsettings.Development.json`;
errors/warnings retained, sensitive-data logging already disabled. Measured same-binary A/B on a disposable
loopback Host + scratch Postgres, 3× `GET /v1/storefront/home`: `Database.Command` lines **317 → 0**,
request-scoped stdout **675,281 → 10,115 bytes (−98.5%)**, response bodies byte-identical, 0 error lines both
legs, wall latency unchanged within noise. **Still missing for closure:** a scale-check across two data sizes
and product identifier/name/price/availability parity on a seeded catalog (the scratch catalog was empty),
plus Architect review and user validation. See
`docs/architecture/evidence/TB-STOREFRONT-EF-LOGGING-RELEASE-178/RESULT.md`.

**TB-179 update (2026-10-17): log fix re-confirmed on a NONEMPTY catalog; scale evidence still missing.**
Same-binary A/B on a disposable loopback Host seeded by the app-owned Development bootstraps (5 products,
6 offers, 10 prices, 8 stock positions) with 1 warm-up + 3 warm `GET /v1/storefront/home?locale=fa`:
`Database.Command` lines **1,287 → 0**, stdout **7,681,486 → 24,608 bytes**, 3/3 bodies byte-identical inside
each leg, 0 errors, median latency **60.4 ms in both legs**. Still OPEN because: the **≥4× catalog size** was
not obtainable (the only scale-capable app-owned seeder sits behind an authenticated Admin Panel session), so
no commands-per-item growth trend exists; cross-leg payload differed by 1,428 bytes (unexplained, UNVERIFIED);
and item-level field parity was not captured. See
`docs/architecture/evidence/TB-STOREFRONT-PERF-SCALE-ACCEPTANCE-179/RESULT.md`.

**TB-180 update (2026-10-17): cross-leg delta closed by runtime proof; scale size still blocked.**
Same instance/binary/database, 1 warm-up + 3 warm `GET /v1/storefront/home?locale=fa`: the EF=Information and
committed-EF=Warning legs returned **byte-identical** bodies (16,279 B, SHA `E4EB276E…`, parsed diff 0 paths),
with **37 request-scoped commands per request** (warm-up + 3 = 37/37/37/37), ≈406 startup commands reported
separately, stdout **1,059,974 → 23,121 B**, and median warm latency 76.6 → 66.2 ms (no regression). The TB-179
14,851/16,279 delta is therefore a **catalog/pricing data-state transition**, not a logging effect: the same
route returned 14,856 B at `prices=13` and settled at 16,279 B afterwards, staying there across three further
process starts while `pricing.prices` grew 37→40→43; the 1,423–1,428 B delta matches the `specialOffers`
section (1,371 B / 2 items). Still OPEN because the **≥4× size remains unreachable**: the only owner-correct
scale seeder (`POST /v1/admin/catalog/demo/seed-only`) returns `403 admin.authorization.denied` on an isolated
instance without a real Admin session, and hand-written/cloned rows are forbidden. No growth ratio exists and
none was invented. See `docs/architecture/evidence/TB-STOREFRONT-PERF-SCALE-CLOSURE-180/RESULT.md`.

**TB-181 update (2026-10-17): the 403 resolved to a per-process authority condition; a structural blocker now
explains the missing second size.** With the **Host itself** owning the Development bootstrap (no separate
MigrationRunner), `POST /v1/admin/catalog/demo/seed-only` reaches its post-authorization validation (`400
catalog.demo.failed`) instead of `403 admin.authorization.denied` — same actor, tenant, header and route. Cause:
`InMemoryAuthorizationAdapter` is a **per-process singleton**, so a `user#member@tenant` grant written in the
MigrationRunner process does not exist in the serving Host. The decisive blocker, however, is the fixture itself:
the owner demo seeder hard-asserts demo products **must remain Draft**, while `StorefrontComposer` composes Home
**only** from `Published` rows; the reset path additionally requires `published == 0`. One bounded seed attempt
grew products 5 → 151 and categories 6 → 122, yet left `published=3 / draft=147`, so the storefront stayed at
size-A scale. No legitimate worker action can produce a ≥4× **storefront-visible** catalog without either a
bounded publish authorization, a new owner `Published` fixture mode, or accepting size-A evidence. No ratio was
invented. See `docs/architecture/evidence/TB-STOREFRONT-PERF-SEED-AUTH-SCALE-181/RESULT.md`.

**TB-182 update (2026-10-17): the missing two-size proof now exists — 4.33x Published growth; blocker still
OPEN.** On an isolated scratch PG/Host, the legacy "demo products must remain Draft" assertion was not touched;
instead the already-authoritative owner routes were used: seller `POST /v1/seller/offers`, `PUT …/price`,
`PUT …/inventory` with the genuine Development seller memberships, then ProductWorkspace
`POST /v1/admin/products/{id}/publish` (Catalog `PublishProductCommand` + real `ProductPublishReadiness`). Size A
(published=3) → size B (published=13), i.e. **13 distinct product IDs in the Home payload vs 3, 52,084 B vs
33,964 B**, warm-up + 3 requests HTTP 200 on the same binary/tenant/log level, byte-identical payload across size
B, 68–93 ms wall time. Of 38 attempted demo Drafts only 10 passed real publish readiness — the remainder were
rejected by validators, not bypassed. Remaining gates: Architect/register acceptance, the user's real-browser
responsiveness verdict, and a size-B request-scoped EF command count (not captured here).
See `docs/architecture/evidence/TB-STOREFRONT-PERF-PUBLISHED-SCALE-182/RESULT.md`.

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

