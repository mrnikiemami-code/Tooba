# Tooba — TB-P00-GATE — Architecture Baseline Gate

BEGIN_TOOBA_CURSOR_GATE_V1

Protocol-Version: 1
Gate-ID: TB-P00-GATE
Phase: P00 — Architecture / Discovery
Type: Phase Gate / Architecture Baseline Consistency Review
Repository: https://github.com/mrnikiemami-code/Tooba
Primary-Branch: main
Implementation-Agent: Cursor
Architect: ChatGPT
Execution-Mode: PIPELINE
Depends-On: TB-P00-T027
Architect-Decision-On-Dependency: ACCEPTED

## Gate Objective

Perform the final P00 architecture-baseline consistency and readiness review.

This Gate decides whether Tooba may leave:

```text
P00 — Architecture / Discovery
```

and become ready for:

```text
P01 — Platform Foundation
```

This is NOT an implementation task.

Do not create application code.

Do not begin P01.

Do not migrate Shopeiva.

Do not create frontend screens.

Do not infer the next task.

Cursor performs evidence-based consistency review only.

Cursor PASS is not Architect ACCEPT.

---

## 1. Repository Recovery

Run first:

```bash
git rev-parse --show-toplevel
git fetch origin
git branch --show-current
git rev-parse HEAD
git rev-parse origin/main
git status --short --branch
```

Require:

```text
branch = main
HEAD == origin/main
```

Expected predecessor:

```text
f4451785dc491af743ef4664e3b6f3d385cc7fa3
```

If synchronized `main` legitimately advanced, continue only from current safe synchronized `main`.

Unsafe or ambiguous state =>

```text
RECOVERY_CONFLICT
```

Never:

```text
force push
rewrite history
destructive reset
silent stash
delete unknown work
auto-commit unrelated work
move/import external Shopeiva references
```

---

## 2. Expected Accepted P00 Architecture Set

Verify repository evidence for the accepted P00 baseline.

At minimum:

```text
00-technical-inventory.md
01-capability-domain-map.md
02-edition-tenant-deployment.md
03-data-ownership-and-module-contracts.md
04-identity-authentication.md
05-spicedb-authorization.md
06-party-organization-b2b-foundation.md
07-catalog-product-offer.md
08-pricing-market-currency.md
09-inventory-availability-reservation.md
10-cart-checkout-order.md
11-payment.md
12-content-page-composition.md
13-seo-architecture.md
14-search-indexing.md
15-media-image-pipeline.md
16-first-party-analytics.md
17-ai-assistant-rag.md
18-observability-logging-audit.md
19-caching-infrastructure-abstractions.md
20-frontend-ux-template-adaptation.md
21-fulfillment.md
22-promotion-discount.md
23-p00-capability-gap-review.md
24-reviews-ratings.md
25-returns-rma.md
26-tax-architecture.md
```

Also verify canonical pipeline / recovery / roadmap SoT.

---

## 3. P00 Accepted Task Chain

Verify task continuity from:

```text
TB-P00-T000
```

through:

```text
TB-P00-T027
```

The Gate must confirm:

```text
T027 is Architect ACCEPTED
P00 Gate is current issued work
No unauthorized P01 task exists
No unauthorized implementation task exists
```

If SoT still says T027 awaiting acceptance, update it minimally for Gate state.

---

## 4. Core Architecture Invariants

Verify no accepted document contradicts these.

### Architecture

```text
Modular Monolith
Future microservice extraction ready
Cross-module SQL JOIN forbidden
No foreign-module repository/table access
Contracts/interfaces/events/projections/gateways across module boundaries
```

Hard principle:

```text
Backend/module boundary != UI boundary
```

---

## 5. Edition / Tenant Invariants

Verify:

Marketplace:

```text
dedicated publish/deployment
multi-seller
one marketplace operational database initially
```

Single-Store:

```text
one shared publish for many customer stores
database per tenant/store
incoming Host resolves durable tenant
durable TenantId != hostname
runtime theme per tenant
no per-tenant build
```

No accidental mixing between Edition, Tenant, Market, Locale, Currency, Theme.

---

## 6. Identity / Party / Authorization

Verify:

```text
Identity/User != Party/Organization
dynamic login identifiers supported architecturally
password + OTP readiness
optional MFA readiness
external IdP readiness
SpiceDB/ReBAC is authorization authority
authorization enforced at use-case boundary
```

No fixed role column as sole authorization model.

---

## 7. Catalog / Offer / Seller

Verify:

```text
Catalog Product != Seller Offer
Variant semantics preserved
Marketplace multi-seller supported
Single-Store does not collapse Product/Offer/Pricing/Inventory seams
```

No Product.Price shortcut.

---

## 8. Pricing / Market / Currency / Tax

Verify:

```text
Locale != Market != Currency != Tax Jurisdiction
```

Pricing:

```text
tax-exclusive commercial/base pricing
context-aware
authored prices distinct from FX-derived display conversion
```

Tax:

```text
Tooba calculates tax separately
configurable effective-dated rules
Tax Exempt supported
context override only by explicit policy
missing rule != zero
no hard-coded Iranian law/rate/date
Iran-first, UK/multi-market ready
B2B VAT/tax invoice out of initial phase
```

Promotion:

```text
Promotion != Pricing
Coupon != Promotion domain
deterministic/versioned evaluation
Order snapshots applied promotion evidence
```

---

## 9. Inventory / Cart / Checkout / Order / Payment

Verify ownership separation.

```text
Inventory owns stock/reservation
Cart != Order
Checkout performs authoritative validation
Order owns commercial commitment
Payment != Order
Payment != provider
Payment Intent != Payment Attempt
```

No cross-module write shortcuts.

---

## 10. Fulfillment / Returns

Verify:

```text
Order != Fulfillment
Shipment != Order
Carrier != Fulfillment Domain
partial/split shipment first-class
multi-seller fulfillment separable
```

Returns:

```text
Return != Cancellation
Return != Refund
Return != Fulfillment
Return != Inventory
Return != Order
partial/multiple RMA supported
Payment owns refund execution
Inventory owns restock
```

---

## 11. Content / Page Composition

Verify:

```text
Semantic Content != Page Composition
```

Approved reusable section registry concept exists.

No arbitrary executable code from DB.

Content is broad enough for:

```text
articles
guides
FAQ
brand/category content
campaign/landing content
multilingual content
SEO metadata
workflow/versioning
AI/RAG eligibility
```

---

## 12. SEO Gate Review

Verify SEO remains architecture-level.

Must cover route policy for:

```text
Home
Category
Brand
Product
Search
Facet
Tag
Landing
Seller
Campaign
Content
```

And:

```text
canonical
hreflang
structured data
sitemap
robots
thin/duplicate control
facet crawl control
Core Web Vitals
server-rendered SEO-critical content
```

Search pages/facets must not create uncontrolled crawl explosion.

---

## 13. Search

Verify:

```text
initial PostgreSQL FTS
future Elasticsearch/OpenSearch
Search consumes denormalized projections
Search != business truth
Persian search readiness
tenant isolation
rebuild readiness
```

Domain must not depend on search-engine internals.

---

## 14. Media

Verify:

```text
original preserved
deterministic variants
resize/crop/format/quality
storage abstraction
transform abstraction
CDN abstraction
focal point / placement metadata
responsive images
modern formats
```

No provider lock.

---

## 15. Reviews / Ratings

Verify:

```text
Product Review != Seller Review
rating aggregate is rebuildable projection
verified purchase via contracts
published-only feeds Search/SEO/AI
moderation lifecycle
SpiceDB + audit
privacy/PII separation
```

---

## 16. Notifications / Fraud / Support

Verify P00 classification:

```text
Notifications = BOUNDARY_SUFFICIENT_FOR_P00
Fraud / Risk = BOUNDARY_SUFFICIENT_FOR_P00
Support = DEFER_POST_P00 unless USER later promotes scope
```

Do not accidentally treat Shopeiva ticket screens as product truth.

---

## 17. Observability / Audit / Analytics

Verify strict separation:

```text
Technical Logs
Business Audit
Security Audit
Analytics
```

are not the same thing.

OpenTelemetry required for:

```text
traces
metrics
technical observability
```

First-party analytics exists separately.

---

## 18. AI / RAG

Verify:

```text
AI Assistant grounded in approved sources
RAG/retrieval
Content approved knowledge
Catalog via contracts/projections
authorization-aware
no direct DB access
Search Index != AI Knowledge Index
live Pricing/Inventory via live contracts where necessary
```

---

## 19. Caching / Infrastructure

Verify:

```text
Cache != truth
Redis not mandatory initially
distributed cache replaceable later
tenant/context-aware keys
public/private cache separation
pricing/inventory revalidation
versioning/invalidation
shared-hosting -> dedicated-hosting evolution
```

---

## 20. Frontend / UX Architecture Gate — CRITICAL

This section is mandatory.

Verify architecture explicitly says:

```text
Weak UI/UX = Product Failure
Backend/module boundary != UI boundary
Build PASS != UI ACCEPT
Functional PASS != Visual ACCEPT
Desktop PASS != Mobile PASS
LTR PASS != RTL PASS
```

Verify distinct professional experiences:

```text
Storefront
Admin
Seller
Customer
```

Verify integrated workspaces:

```text
Product Workspace
Order Workspace
Seller Workspace
Customer Workspace
Content Studio
Tenant Settings Workspace
Return Case Workspace / Returns Workflow direction
```

Admin must NOT be a mirror of backend entities or basic CRUD menus.

---

## 21. Product Workspace Gate — CRITICAL

Verify Product management architecture is cohesive.

The intended UX must support a unified Product Workspace composing concerns such as:

```text
Core product information
Category / Brand
Attributes
Variants
Media
Offer / Seller context
Pricing
Tax classification
Inventory
SEO
Content
Publication
Audit/history
```

These may belong to separate backend modules.

The UX must NOT force operators through disconnected menus purely because backend modules are separate.

---

## 22. Professional Data Grid Gate — MANDATORY

Verify the reusable professional Data Grid requirement is explicitly retained in accepted architecture / roadmap.

Minimum required capability direction:

```text
typed column filters
text filter
number filter
money filter
date filter
enum filter
boolean filter
entity filter
status filter

sorting
column reorder
column resize
show/hide columns
saved views
pagination
row selection
bulk actions
export
sticky header
sticky columns where useful
keyboard accessibility
RTL/LTR
responsive strategy
server-side large-dataset handling
```

The Data Grid is expected to be reusable across operational workspaces such as:

```text
Products
Orders
Sellers
Customers
Inventory
Payments
Fulfillment
Returns
Reviews
Promotions
Content
Analytics
```

If accepted docs only mention a generic "table" and do not preserve this requirement strongly enough:

```text
GATE_REPAIR_REQUIRED
```

Do not silently accept.

---

## 23. Deep Shopeiva Study Gate — MANDATORY FUTURE-EXECUTION RULE

Before serious UI implementation or template migration, Tooba MUST perform a dedicated deep study of the purchased Shopeiva template.

Verify roadmap/recovery context preserves this as a mandatory future step.

The study must be:

```text
deep
file-by-file where relevant
route-aware
component-aware
layout-aware
dependency-aware
responsive-aware
RTL/LTR-aware
interaction-aware
asset-aware
documentation-aware
```

It must inspect both template source and its help/documentation when available externally.

Expected conceptual external references may be:

```text
reference/shopeiva/
reference/help.pdf
```

but repository architecture MUST NOT depend on a hard-coded local Windows absolute path.

The deep study must produce a decision-grade reuse map:

```text
REUSE
ADAPT
REBUILD
DROP
DEFER
```

covering at minimum:

```text
Storefront shell
Navigation
Homepage
Product cards
PDP
Category/listing
Search
Cart
Checkout
Auth
Customer dashboard
Seller/vendor area
Content pages
Shared components
Forms
Tables/Grid
Charts
Theme utilities
Assets
Fonts
Dependencies
Demo data
```

Hard rule:

```text
Shopeiva = UI/reference/reuse input
Shopeiva != architecture/domain/SEO/security/tenant truth
```

If this mandatory deep-study step is absent from durable SoT/roadmap:

```text
GATE_REPAIR_REQUIRED
```

Do not start UI implementation.

---

## 24. Shopeiva Dependency Rule

Verify no template dependency is retained automatically.

Future deep study must classify dependencies:

```text
KEEP
REPLACE
REMOVE
DEFER
```

No wholesale template copy.

No template-driven domain model.

---

## 25. Runtime Theme Gate

Verify:

```text
Design tokens
Brand assets
Approved component variants
Layout/composition config
Tenant theme config
```

are the safe theme model.

No arbitrary executable code from DB.

Single-Store remains one shared publish with runtime tenant themes.

---

## 26. RTL / LTR / Mobile / Accessibility

Verify these are architecture concerns, not finishing tasks:

```text
RTL
LTR
mobile-first
keyboard accessibility
semantic HTML
focus states
screen-reader usability
responsive layouts
loading/empty/error states
```

---

## 27. Visual Acceptance Protocol

Verify future UI tasks require visual evidence.

Evidence location:

```text
docs/evidence/
```

Evidence should identify:

```text
Task-ID
route/screen
viewport
locale/direction
state
commit/build
```

Architect visual acceptance is mandatory for user-visible implementation.

Build/test PASS alone must never become UI acceptance.

---

## 28. Hosting / Evolution Readiness

Verify architecture can begin on practical initial infrastructure while preserving evolution toward:

```text
dedicated hosting
Redis
Elasticsearch/OpenSearch
external IdP
3PL/carriers
future microservices
additional tax jurisdictions
```

Architecture-ready does not mean implementing them now.

---

## 29. Gap / Contradiction Scan

Search accepted P00 docs for unresolved contradictions or dangerous ambiguous statements.

Especially detect:

```text
Product.Price
foreign-module join
direct cross-module repository access
role-only authorization
hostname as TenantId
locale as market
Search as source of truth
AI direct DB access
Payment == Order
Return == Refund
Carrier == Fulfillment
Tax missing rule == zero
template == architecture
Admin == CRUD
Build PASS == UI ACCEPT
```

Report every material contradiction.

If a contradiction can be repaired by a minimal documentation correction within this Gate, do so and report it.

If it would require a real architectural decision:

```text
BLOCKED
```

---

## 30. P01 Entry Readiness

Assess whether P01 Platform Foundation can safely begin after Architect accepts the Gate.

Expected P01 direction, for assessment only:

```text
repository/application skeleton
module boundaries
configuration
tenant/edition foundation
PostgreSQL foundation
OpenTelemetry
logging/error handling
outbox/events foundation
cache abstractions
background-work foundation
Next.js shell foundation
```

Do not create P01 task.

---

## 31. Durable Roadmap Requirement

Ensure roadmap explicitly retains future work for:

```text
Deep Shopeiva Study
Template reuse map
Design System extraction
Professional Data Grid foundation
Workspace interaction patterns
Visual acceptance gates
```

These must not be left only in chat memory.

If absent, minimally add them to durable SoT.

Do NOT implement them in P00 Gate.

---

## 32. Gate Evidence Document

Create:

```text
docs/architecture/27-p00-gate-review.md
```

Include:

```text
Gate summary
Accepted P00 task chain
Architecture invariant checklist
Capability completeness
P00 gap status
Cross-document contradiction scan
Frontend/UX quality gate
Product Workspace gate
Professional Data Grid gate
Deep Shopeiva Study gate
Visual acceptance gate
P01 entry-readiness assessment
Remaining deferred items
Gate verdict
```

---

## 33. Gate Verdict

Return exactly one:

```text
P00_GATE_PASS
P00_GATE_REPAIR_REQUIRED
P00_GATE_BLOCKED
```

Definitions:

```text
P00_GATE_PASS
= no material contradiction, all mandatory P00 boundaries present,
  durable future Shopeiva/Grid/UI rules preserved, P01 can be issued after Architect ACCEPT.

P00_GATE_REPAIR_REQUIRED
= architecture is fundamentally sound but durable docs need bounded repairs
  before Architect may accept P00.

P00_GATE_BLOCKED
= unresolved real architecture/product decision prevents P00 completion.
```

Cursor cannot mark P00 complete on its own.

---

## 34. Source-of-Truth Updates

Update:

```text
docs/PROJECT-STATE.md
docs/ROADMAP.md
docs/ai/TOOBA-RECOVERY-CONTEXT.md
```

Expected current state:

```text
Last Architect Accepted Task: TB-P00-T027
Current Issued Work: TB-P00-GATE
Current Phase: P00 — Architecture / Discovery
Gate State: AWAITING_ARCHITECT_ACCEPT
P01: NOT ISSUED
```

If Gate verdict is PASS, SoT may say:

```text
P00 Gate review = PASS BY CURSOR / AWAITING ARCHITECT ACCEPT
```

It must NOT say:

```text
P00 COMPLETE
```

until Architect accepts Gate.

---

## 35. Save Complete Gate Envelope Verbatim

Save exact Gate envelope to:

```text
docs/ai/tasks/TB-P00-GATE.gate.md
```

Verify exact markers:

```text
BEGIN_TOOBA_CURSOR_GATE_V1
END_TOOBA_CURSOR_GATE_V1
```

---

## 36. Explicit Out of Scope

Do NOT:

- write application code;
- start P01;
- create P01 task;
- implement module skeleton;
- implement Data Grid;
- implement Design System;
- inspect/import/migrate Shopeiva deeply in this Gate;
- copy Shopeiva;
- implement frontend;
- create Tax code;
- create final production UI;
- change product scope;
- invent Support as first-sale requirement;
- implement B2B Tax;
- create unauthorized ADRs.

---

## 37. Validation

Run:

```bash
git diff --check
git status --short --branch
```

Manual checks:

- T027 Architect ACCEPTED;
- Gate current;
- accepted architecture set exists;
- architecture invariants consistent;
- gap review closed;
- Tax user decision durable;
- Reviews/Returns complete;
- Notifications/Fraud sufficient for P00;
- Support deferred;
- UI/UX quality rule durable;
- Product Workspace rule durable;
- professional Data Grid rule durable;
- deep Shopeiva study future step durable;
- Shopeiva not architecture truth;
- visual evidence rule durable;
- no application code;
- P01 not issued;
- Gate not self-accepted.

---

## 38. Git Commit & Push

Commit:

```text
docs review Tooba P00 architecture gate [TB-P00-GATE]
```

Then:

```bash
git push origin main
git fetch origin
git rev-parse HEAD
git rev-parse origin/main
git status --short --branch
```

Require:

```text
HEAD == origin/main
```

No force push.

---

## 39. Result Contract

Return exactly:

```text
BEGIN_TOOBA_CURSOR_RESULT_V1

Protocol-Version: 1
Gate-ID: TB-P00-GATE
Phase: P00 — Architecture / Discovery
Status: PASS | REPAIR_REQUIRED | BLOCKED | RECOVERY_CONFLICT
Gate-Verdict: P00_GATE_PASS | P00_GATE_REPAIR_REQUIRED | P00_GATE_BLOCKED

Summary:
...

Repository-Recovery:
- Repo-Root:
- Branch:
- Starting-HEAD:
- Starting-Origin-Main:
- Starting-Status:

Changes:
- ...

Accepted-Architecture-Set:
- ...

Core-Invariants:
- ...

Edition-Tenant:
- ...

Identity-Authorization:
- ...

Commerce-Boundaries:
- ...

Tax-Promotion-Pricing:
- ...

Fulfillment-Returns:
- ...

SEO-Search-Media:
- ...

Reviews-Notifications-Fraud-Support:
- ...

Observability-Analytics-AI:
- ...

Frontend-UX-Gate:
- ...

Product-Workspace-Gate:
- ...

Professional-DataGrid-Gate:
- ...

Deep-Shopeiva-Study-Gate:
- ...

Visual-Acceptance-Gate:
- ...

Contradiction-Scan:
- ...

Deferred-Items:
- ...

P01-Readiness:
- ...

Validation:
- git diff --check:
- other checks:
- manual consistency review:

Git:
- Commit:
- Push:
- Final-HEAD:
- Final-Origin-Main:
- Final-Status:
- Head-Matches-Origin: YES | NO

Source-of-Truth:
- Last-Architect-Accepted-Task:
- Current-Issued-Work:
- Current-Phase:
- Gate-State:
- P01-Issued: YES | NO
- Recovery-Ready: YES | NO

Architectural-Concerns:
- ...

Blockers:
- ...

END_TOOBA_CURSOR_RESULT_V1
```

---

## 40. Pipeline Continuity — MANDATORY

After RESULT:

```text
WAITING
```

Remain inside the Tooba Architect-controlled pipeline.

Do not leave PIPELINE mode.

Do not invent, infer, prepare, or execute:

```text
P01
TB-P01-T001
module skeleton
frontend implementation
Shopeiva migration
deep template study
Data Grid implementation
Design System implementation
```

without a new valid Architect envelope.

Even:

```text
P00_GATE_PASS
```

means only:

```text
PASS BY CURSOR
```

not Architect ACCEPT.

Only Architect can declare P00 complete and issue P01 work.

After RESULT, stop and remain:

```text
WAITING
```

---

## 41. UI / UX Protection — MANDATORY PROJECT RULE

Tooba is a commercial product.

Weak UI/UX is considered product failure.

Hard rules remain:

```text
Backend/module boundary != UI boundary
Build PASS != UI ACCEPT
Functional PASS != Visual ACCEPT
Desktop PASS != Mobile PASS
LTR PASS != RTL PASS
```

Professional reusable Data Grid remains mandatory.

Deep Shopeiva study remains mandatory before serious UI implementation.

Product/Order/Seller/Customer and other major operational experiences must be workspace/workflow oriented, not CRUD mirrors.

Future user-visible implementation requires visual evidence and Architect visual acceptance.

END_TOOBA_CURSOR_GATE_V1
