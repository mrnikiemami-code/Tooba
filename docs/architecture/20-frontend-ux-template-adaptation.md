# Tooba — Frontend, UX & Template Adaptation Architecture

Status:

```text
P00 architecture design — candidate for later ADR; not an ADR lock
```

Task:

```text
TB-P00-T021
```

Documentation only. No pages, components, CSS, template unzip/migration, routes, screenshots, visual redesign, or backend APIs.

```text
Weak UI/UX = Product Failure
Backend/module boundary != UI boundary
Shopeiva = UI/reference/reuse input, not architecture truth
Locale != Market != Currency
Build PASS != UI ACCEPT
Functional PASS != Visual ACCEPT
Desktop PASS != Mobile PASS
LTR PASS != RTL PASS
```

Digikala, Amazon, and other competitor storefronts are **not** architecture truth. Shopeiva/`shopeiva.zip` is `TEMPLATE_PRESENT`; template-only features remain `PRODUCT_DECISION_PENDING` until USER/product confirms them.

## A. Product-Critical UI Principle

Frontend quality is a first-class product requirement, not a late cosmetic phase.

Architecture must support as first-class:

```text
storefront
Admin
Seller
Customer
mobile
RTL/LTR
accessibility
SEO
performance
visual consistency
```

A technically correct but weak commercial UI is treated as product failure.

## B. Backend Boundary != UI Boundary

Hard rule:

```text
Backend/module boundary != UI boundary
```

Do not mirror backend modules as disconnected menu CRUDs.

Backend may own Catalog, Media, Pricing, Inventory, SEO, Content, Offer as separate modules. Admin UX may still present one cohesive **Product Workspace** with integrated sections/tabs/steps.

This is a locked UX architecture principle: UI orchestrates user goals; modules remain ownership boundaries behind application/read contracts.

## C. Shopeiva Role

Purchased template `shopeiva.zip` is:

```text
UI/reference/reuse input
```

It is **not**:

```text
domain truth
architecture truth
SEO truth
security truth
tenant truth
authorization truth
```

Reuse selectively. Do not preserve bad information architecture because it exists in the template. Do not promote template-only features into Tooba requirements (`TEMPLATE_PRESENT / PRODUCT_DECISION_PENDING`).

## D. Template Reuse Classification

Classifications: `REUSE` | `ADAPT` | `REBUILD` | `DROP` | `DEFER`. No implementation.

| Candidate | Class | Note |
| --- | --- | --- |
| Storefront shell | ADAPT | Layout/visual cues only; Tooba IA, SEO, tenant, edition policy |
| Header / navigation | ADAPT | Customer-goal IA; locale/market/currency separate; edition capabilities |
| Home sections | ADAPT | Map to approved Page Composition registry; drop demo sections |
| Product cards | ADAPT | Design-system commerce component; Offer/Price/Inventory facts from contracts |
| Product detail layout | ADAPT | Hierarchy + marketplace offer UX; not template module dump |
| Category / listing | ADAPT | Facets, chips, crawlable pagination; SEO policy over template filters |
| Search UI | ADAPT | Server/search-engine backed; template search is not product Search |
| Cart | ADAPT | Seller grouping, price/availability change, stale-cart recovery |
| Checkout | REBUILD | Friction, guest checkout, payment/inventory authority; hide orchestration |
| Auth | REBUILD | Extensible identifiers/OTP/MFA/IdP; template login is not Identity truth |
| Customer dashboard | ADAPT | Polished account UX; not backend entity menus |
| Seller dashboard | REBUILD | Authorization-aware B2B/marketplace workflows; do not clone Admin or template seller |
| Content / blog pages | ADAPT | Content types + composition; blog is not the Content root |
| Shared components | ADAPT | Promote only into design-system/commerce layers with ownership |
| Icons | ADAPT | One system (see BC); direction-aware RTL |
| Forms | ADAPT | Server validation + a11y + RTL; not client-only |
| Tables | REBUILD | Professional typed Data Grid for Admin/Seller |
| Charts | ADAPT | Analytics UI only; accessible fallback; library not architecture truth |
| Theme utilities | ADAPT | Validated tokens only; no executable tenant theme code |
| Demo data | DROP | Never ship; never treat as requirements |
| Fonts / assets | DEFER | License/asset review before embed; multilingual strategy |

Exact component inventory after template extraction (section BY). Classifications may tighten then without changing this policy.

## E. Frontend Stack Direction

Preserve if product direction remains consistent:

```text
Next.js App Router
React
TypeScript
Tailwind CSS
```

**Recommended vs ADR:** App Router + TypeScript + React is `RECOMMENDED_FOR_ADR`. Tailwind as styling engine is recommended if it remains the token/utility vehicle, not as a license to hardcode per-page styles.

Do not blindly keep every template dependency. Each dependency must earn its place (section BZ).

## F. Server Component First

Public storefront and SEO-critical pages default to:

```text
Server Component First
```

Client Components only when interaction requires them. Do not convert entire pages into client-rendered apps.

Benefits: SEO, performance, smaller JS, server data composition, security boundaries.

## G. Client Component Boundary

Client islands are appropriate for: interactive filters, cart interactions, modals/drawers, forms, autocomplete, drag/drop, rich editors, dashboard interactions, local optimistic UI.

Do not mark every reusable primitive `"use client"`. Prefer server-rendered shells with small interactive islands. Shared presentational components stay server-capable unless they need browser APIs or event handlers.

## H. Data Fetching / BFF Composition

Pages compose **application / BFF / read models**, not frontend-direct database access.

A public PDP may compose Catalog, Offer, Pricing, Inventory, Reviews, Media, Content, SEO behind application/read contracts.

Do not encode backend module topology into page-level spaghetti requests. UI sees composed screen contracts; modules remain owners.

## I. Storefront Information Architecture

Customer-goal IA (not table names):

```text
Home
Category
Brand
Search
Product
Cart
Checkout
Content
Landing / Campaign
Account
```

Marketplace may add Seller storefront and Other offers. Digikala/Amazon IA is not a requirement source.

## J. Admin Information Architecture

Operational, workflow-oriented. Candidate top-level domains (exact IA may evolve):

```text
Commerce
Catalog
Orders
Sellers
Customers / Parties
Content
Promotions
Analytics
Operations
Configuration
Security
```

Hard rule: do not create one menu item per backend entity/table.

## K. Product Workspace UX

Mandatory unified product-authoring workspace. Possible areas: Overview, Core Information, Variants, Specifications, Categories/Brand, Media, Offers, Pricing, Inventory, SEO, Content/Editorial, Publishing, Audit/History.

Backend modules stay separate. UI orchestrates them so the admin does not jump disconnected screens for one product.

## L. Seller Panel UX

Professional B2B/marketplace workflows, not an Admin clone. Potential areas: Dashboard, Offers, Products/Catalog Requests, Pricing, Inventory, Orders, Customers where permitted, Analytics, Reviews, Promotions where permitted, Support, Settings, Team/Access.

Seller scope is authorization-aware (SpiceDB). Hidden UI is not security.

## M. Customer Dashboard UX

Polished, mobile-first, no backend boundaries: Overview, Orders, Addresses, Profile, Security, Notifications, Support. Saved items/wishlist only if product later confirms (`PRODUCT_DECISION_PENDING` if template-only).

## N. Design System

First-class layer: Design Tokens, Typography, Spacing, Grid, Radius, Elevation, Color Roles, State Colors, Iconography, Motion, Component Variants, Breakpoints, Accessibility Constraints.

No random per-page styles. Theme overrides operate only through approved tokens/variants.

## O. Component Layers

Conceptual hierarchy:

```text
Primitives
Design-system components
Commerce components
Feature composites
Page sections
Page layouts
```

Examples: Button, Input, Dialog, DataGrid, ProductCard, PriceDisplay, StockBadge, SellerOfferCard, ProductGallery, OrderTimeline, HeroSection.

Avoid one giant shared-components folder with no ownership.

## P. Runtime Theme Architecture

Single-Store: one shared publish, runtime tenant theme, **no arbitrary executable tenant code**.

Theme may include design tokens, brand assets, approved component variants, layout/composition settings.

Theme must preserve a11y, responsive behavior, SEO, Core Web Vitals.

## Q. Theme Safety

Tenant-configured theme values must not: hide legal/critical content, break contrast, destroy focus states, inject JavaScript, inject unsafe CSS, change semantic page structure arbitrarily.

Validated token constraints (ranges, contrast floors, allowlisted properties). Invalid theme → safe default + operator signal; fail closed on unsafe values.

## R. RTL / LTR

Architectural, not page hacks: logical CSS properties, direction-aware icons, layout mirroring, text alignment, mixed-language content, tables, forms, charts, drawers, breadcrumbs, pagination.

Direction is derived from locale/presentation, not from market or currency.

## S. Mobile-First UX

Mobile is not scaled desktop. Intentional patterns for: navigation, search, filters, product gallery, sticky purchase CTA, cart, checkout, forms, admin/seller dense tables, drawers/sheets, dashboards.

Operational UIs choose cards/stacked views vs horizontal grids per workflow. Dense desktop Data Grid remains preferred for many Admin/Seller tasks; mobile uses condensed list, priority columns, detail drawer, or justified horizontal scroll.

## T. Accessibility

Mandatory acceptance: semantic HTML, keyboard navigation, focus management, screen reader labels, heading hierarchy, contrast, form errors, ARIA only when needed, reduced motion, accessible dialogs, accessible tables/charts, alt text.

## U. Loading States

Every major path needs intentional loading: skeleton, streamed section, optimistic local action, progress, deferred panel.

No random full-screen spinners everywhere. Preserve layout stability (CLS).

## V. Empty States

Professional empty states explain the state, offer next action, respect role/scope, and do not look broken. Examples: no orders, no seller offers, no search results, no products in category, no analytics data.

## W. Error States

Intentional UX for: validation, authorization denial, not found, dependency unavailable, partial data failure, payment failure, inventory change, network error, AI unavailable.

Do not leak exception details. Retry/recovery where safe.

## X. Partial Degradation

Composite pages degrade by section. Example: reviews unavailable must not necessarily break product + price + purchase.

Critical vs optional sections are designed per screen (see CX).

## Y. Forms

Support: server validation, client interaction validation, field-level and form-level errors, async checks, dirty state, unsaved changes, multi-step flows, autosave where justified, accessibility, RTL/LTR.

Do not rely only on client validation.

## Z. Admin Data Grid

Mandatory reusable professional Data Grid: sorting, column-type filters, global search where appropriate, reorder/show-hide columns, resize where useful, saved views, pagination, selection, bulk actions, export, sticky columns/header where useful, empty/loading/error, keyboard a11y, RTL/LTR, responsive strategy.

Column types need typed filters: text, number, money, date, enum, boolean, entity, status. Not a one-size-fits-all text filter.

## AA. Saved Views

Future: personal saved views, shared/team views where authorized, default views, filter/sort/column state. Authorization applies to shared views. Do not implement now.

## AB. Bulk Actions

Require: selection, scope clarity, authorization, confirmation for risky actions, partial failure reporting, background execution for large operations, audit.

Do not make every bulk action synchronous.

## AC. Navigation

Separate navigation patterns for Storefront, Admin, Seller, Customer. Do not reuse one shell everywhere. Breadcrumbs/context for deep workflows.

## AD. Search UX

First-class storefront search: autocomplete, suggestions, recent/popular queries (future), category/brand/product suggestions, mobile search, keyboard support, zero-result recovery, filters, chips, sorting, result count.

Search backend boundaries remain invisible. Fuse.js is not product Search (CA).

## AE. Category / PLP UX

Facets, sort, result count, selected chips, clear all, mobile filter sheet, pagination with infinite-scroll as **progressive enhancement**, skeletons, empty results, SEO crawlable navigation.

Do not sacrifice SEO for client-only interaction. Facet URL policy follows SEO architecture; UI must not create unbounded crawlable URLs.

## AF. PDP UX

Gallery, title/brand/category context, variants, price, availability, seller/offer selection, other sellers (Marketplace), delivery/fulfillment, CTA, specifications, content, reviews, related products, SEO semantic content.

Reviews on PDP: aggregate, distribution, verified-purchase labels, cards, pagination — composition via Reviews contract, not Catalog CRUD (`docs/architecture/24-reviews-ratings.md`). Admin uses a Moderation Workspace, not a raw review grid.

Clear visual hierarchy. Not a dense unstructured wall of modules.

## AG. Marketplace Offer UX

Selected/buy-box offer, other offers, seller reputation, price, availability, delivery promise, warranty/service.

Do not confuse Product identity with seller Offer. Single-Store hides other-offer UI via capabilities, not scattered conditionals.

## AH. Cart UX

Seller grouping where relevant, quantity, price changes, availability changes, promotion, shipping estimates, next action.

Long-lived stale cart state must be visible and recoverable.

## AI. Checkout UX

Clear steps, progress, address, delivery, payment, review, validation, price/stock changes, recovery, mobile usability, guest checkout.

Do not expose internal module orchestration as user-visible complexity.

## AJ. Auth UX

Extensible identifiers/methods: username/email/phone, password, OTP, MFA, recovery, external IdP future — without redesign.

Do not assume one fixed email/password form forever. Template login screens are not Identity requirements.

## AK. Content Studio UX

Library, filters, locale status, draft/review/published, revision history, preview, schedule, SEO inputs, taxonomy, related entities, AI eligibility.

Do not reduce to title/body CRUD.

## AL. Page Composition UX

Approved section palette, reorder, configuration, media picker, product/category binding, responsive preview, locale preview, theme preview, schedule/publish, validation.

No arbitrary code blocks.

## AM. Media UX

Upload, progress, ordering, primary selection, crop/focal point, alt/caption, responsive preview, processing/failure states.

Product/Content editors must not be forced into separate Media CRUD for normal work. Media remains the data owner.

## AN. Analytics UX

KPI/decision-oriented: date range, comparison, filters, drill-down, freshness, accessible charts/table, export, mobile, RTL/LTR.

Do not expose raw event schema. Analytics is not business SoT.

## AO. Operations UX

Cohesive diagnosis: order/payment correlation, audit history, search/index state, jobs/queues, media failures, AI degradation, tenant context.

Not raw log CRUD.

## AP. SEO Rendering

Public critical content is server-rendered and crawlable. Interactions may progressively enhance.

Preserve: semantic headings, canonical content, breadcrumbs, crawlable pagination, structured data consistency.

Do not hide critical SEO content behind client-only fetch.

## AQ. Core Web Vitals

Protect LCP, INP, CLS via: server rendering, image optimization, font strategy, JS budget, client-component budget, code splitting, streaming, cache, layout dimensions, third-party scripts.

No implementation now.

## AR. Performance Budgets

Implementation phase must establish measurable budgets for: JS shipped, page weight, LCP asset, interaction latency, image weight, third-party scripts, API/query latency.

Exact values later. Do not optimize only for Lighthouse screenshots.

## AS. Third-Party Scripts

Marketing/analytics/chat scripts: consent/policy, defer/lazy load, performance budget, failure isolation.

Must not block the purchase path.

## AT. Frontend State Management

Do not default to global client state. Prefer: server data, URL state, local component state, form state, small shared client stores only when justified.

Template Zustand is not automatically retained. Each global store needs an explicit need.

## AU. URL as State

Prefer URL for shareable/navigable: search query, filters, sort, pagination, semantically appropriate tabs.

Do not put all UI state in the URL (transient modals, focus, local dirty form).

## AV. Client Data Cache

Frontend client cache must not become a duplicate business source. Avoid complex client-side normalized cache unless product later needs it.

Server-side composition/read models remain primary for public pages.

## AW. Optimistic UI

Only for reversible/predictable actions (e.g. cart quantity, future wishlist, simple preferences).

Do not optimistically claim payment success, inventory reservation success, or order placed without server authority.

## AX. Design Tokens and Theming

Token categories: color, typography, spacing, radius, shadow, motion, breakpoints, component density.

Theme changes through approved tokens/variants. No arbitrary per-tenant CSS injection.

## AY. Visual Consistency

One coherent visual language across Storefront, Customer, Seller, Admin.

Different density/navigation is allowed; foundations stay shared. Do not copy unrelated template visual patterns blindly.

## AZ. Density Strategy

Design system supports comfortable vs compact (or equivalent) density modes.

Do not force storefront card spacing onto data-heavy operations.

## BA. Accessibility + Density

Dense tables/forms still require touch-target minimums, keyboard focus, readable type, contrast, row/action clarity.

Density must not destroy usability.

## BB. Motion

Motion aids orientation, not decoration: reduced-motion support, bounded animations, no layout-shifting entrance effects.

Do not inherit excessive template animations automatically.

## BC. Iconography

One consistent icon system. Known template uses Lucide React — classify later (`KEEP` vs `REPLACE`); do not mix many libraries.

Direction-aware icons must behave correctly in RTL.

## BD. Typography

Multilingual strategy: Persian readability, Latin readability, font loading, weights, fallbacks, numerals, line-height, CWV.

Do not embed font files in repository decisions without license/asset review.

## BE. Localization UX

Design for longer labels, different pluralization, RTL/LTR, localized numbers/dates/currency.

Do not hardcode widths around Persian-only labels.

## BF. Date / Time UX

Localized presentation; domain time semantics stay in owning modules.

Frontend date-picker libraries do not define the business time model. Template Persian date packages are UI implementation details (`DEFER` until chosen).

## BG. Currency UX

Price display respects currency, locale formatting, market, rounding/display policy.

```text
Locale != Market != Currency
```

Do not infer commercial currency from language.

## BH. Security UX

Sensitive actions: reauth/step-up, confirmation, authorization denied, session expired, MFA.

Do not leak permission-model internals.

## BI. Authorization-Aware Navigation

Menu visibility may use authorization for UX, but:

```text
hidden menu != security
```

Server/use-case authorization remains mandatory. Prefer bulk/scope-aware permission data over hundreds of per-item checks.

## BJ. Error Boundaries

Support: route-level error boundary, feature/section boundary, recoverable component error, not-found, unauthorized.

Composite pages degrade intentionally (X, CX).

## BK. Telemetry / UI Diagnostics

Later integrate with OpenTelemetry/observability for: JS errors, route failures, CWV, critical interaction latency, failed API calls.

No sensitive form data in telemetry.

## BL. Testing Pyramid — Frontend

Future: unit, component, integration, route/server, E2E critical journeys, accessibility, visual regression, responsive, RTL/LTR.

Do not rely only on snapshots.

## BM. Visual Acceptance Protocol

Mandatory. Any future task implementing/modifying user-visible UI must provide visual evidence.

At minimum, capture/review as applicable: Desktop, Mobile, meaningful intermediate width, RTL, LTR where supported, Loading, Empty, Error, long-content/edge state.

Not every screen needs every state in every task; the task must define applicable states.

Cursor PASS is insufficient without visual inspection. Architect must separately ACCEPT the visual result.

## BN. Screenshot Evidence

Future UI tasks place durable evidence under `docs/evidence/` or the pipeline-defined location.

Evidence identifies: Task-ID, Route/screen, Viewport, Locale/direction, State, Date/build/commit.

No context-free screenshots.

## BO. Visual Regression

Preserve future automated visual regression. Do not require a tool/provider now. Use stable fixtures/data.

## BP. Realistic Data

Acceptance uses realistic lengths and states. Do not accept screens only with `"Test"`, `"$10"`, `"Product 1"`.

Need long Persian/English names, multi-line content, zero/large values, multiple statuses, seller variations.

## BQ. Skeleton Fidelity

Skeletons should approximately match final layout. Avoid severe CLS when content loads.

## BR. Empty/Error Quality

Empty and error states are part of visual acceptance. Not raw exception boxes or blank white space.

## BS. Cross-Module Workspace Pattern

Reusable UX orchestration: Product Workspace, Order Workspace, Seller Workspace, Customer Workspace, Content Studio, Tenant Settings Workspace.

Each composes multiple module-owned data/contracts into one user-goal-oriented screen.

## BT. Order Workspace

Admin/Seller order view may compose Order, Payment, Fulfillment, Inventory events, Customer/Party, Audit, Support — without direct DB joins. Customer shipment timeline is Fulfillment UX, not Order status collapse. See `docs/architecture/21-fulfillment.md`.

One coherent case/workspace.

## BU. Seller Workspace

Admin seller management may compose Party/Organization, Seller status, Offers, Orders, Inventory, Analytics, Authorization/team, Audit, Support — not eight unrelated screens.

## BV. Customer Workspace

Admin customer support may compose Party, identity-safe summary, Orders, Payments, Support, Audit, authorization-safe actions.

PII access must be permissioned.

## BW. Route / Feature Folder Strategy

Organize around: app routes, feature modules, shared design system, server composition, client interaction islands.

Do not create technical folders solely from backend module names.

Recommended high-level convention (names illustrative, not code):

```text
app/                 # App Router: storefront, admin, seller, account route areas
features/            # User-goal features (pdp, checkout, product-workspace) — not Catalog/Offer folders
design-system/       # Tokens, primitives, DS components
commerce-ui/         # ProductCard, PriceDisplay, StockBadge, DataGrid
server/composition   # BFF/read-model mappers for screens
```

Edition/capability context at composition roots, not scattered `if (marketplace)` in leaves.

## BX. Shared Component Governance

A component becomes shared only when genuinely reusable. Avoid premature generics.

| Rule | Direction |
| --- | --- |
| Where shared lives | `design-system` or `commerce-ui` with an owner |
| Feature-local stays local | Until a second real consumer exists |
| Variants | Token/variant API, not copy-paste forks |
| Breaking visual changes | Governed review + visual evidence |

## BY. Template Extraction Process

Future Shopeiva adaptation workflow:

```text
inventory component
classify reuse
extract dependency
remove demo data
normalize styles
convert to design-system tokens
adapt RTL/LTR
adapt server/client boundary
replace hardcoded content
connect real read contracts
validate SEO/a11y/CWV
visual review
```

Do not copy the whole template into production unchanged. Do not unzip into the app in this task.

## BZ. Template Dependency Audit

Later classify each as `KEEP` | `REPLACE` | `REMOVE` | `DEFER`. Do not automatically accept:

| Dependency | Initial class | Note |
| --- | --- | --- |
| axios | DEFER | See CB; platform fetch may suffice |
| chart.js | DEFER | Analytics UI only; not architecture truth |
| framer-motion | DEFER | Motion budget; reduced-motion; may REMOVE if excessive |
| fuse.js | REMOVE as product Search | Tiny local lists only (CA) |
| lucide-react | DEFER | Single icon system candidate (BC) |
| next-themes | DEFER | Runtime tenant theme is Tooba tokens, not template theme truth |
| persian-date / persian-datepicker | DEFER | UI detail; not time-model owner (BF) |
| react-chartjs-2 | DEFER | With charts |
| react-hook-form | DEFER | Forms still need server validation |
| react-loading-skeleton | DEFER | Skeleton fidelity over library lock |
| react-otp-input | DEFER | Auth UX capability, not Identity truth |
| react-paginate | DEFER | Crawlable pagination preferred on public lists |
| react-toastify | DEFER | Toasts not critical-error channel (CD) |
| swiper | DEFER | Gallery/carousels; CLS/a11y constraints |
| zod | DEFER | Client/schema help; server remains authority |
| zustand | DEFER | Not retained merely because template has it (AT) |

## CA. Client-Side Search Libraries

Fuse.js is not product Search architecture. Tiny local lists only. Canonical storefront search remains server/search-engine based.

## CB. Axios vs Platform Fetch

Whether `axios` remains necessary in Next.js is an implementation detail (`NEEDS_LATER_P00_DETAIL` / not locked). Server-side data access may prefer platform/native fetch and application contracts.

## CC. Charts

Charts belong to analytics/admin UI. Require accessible fallback/table, RTL labels, responsive sizing, loading/empty states.

Do not make one chart library architecture truth.

## CD. Toasts / Notifications

Toasts are transient UX, not a replacement for inline status/error.

Do not hide critical payment/order failures only in a disappearing toast.

## CE. Modals / Drawers

Choose modal/drawer/sheet by workflow and viewport. Mobile often needs bottom sheet or full-screen.

Accessibility and focus management are mandatory.

## CF. Tables vs Cards

Decide per use case. Do not mechanically convert every table row into cards.

Dense desktop workflows prefer the Data Grid. Mobile may use condensed list, priority columns, detail drawer, or horizontal scroll only when justified.

## CG. Internationalization Architecture

Consume translation contracts without hardcoded language assumptions: route locale, message locale, content locale, direction, number/date/currency formatter.

Do not mix translation strings into random components without namespace/domain organization.

```text
Locale != Market != Currency
```

## CH. Feature Discovery / Progressive Disclosure

Complex Admin/Seller workflows use progressive disclosure. Do not show every advanced setting first.

Advanced capabilities must remain discoverable.

## CI. Confirmation / Destructive Actions

Destructive/high-impact actions need clear consequence, authorization, confirmation, reason where appropriate, audit.

Avoid generic "Are you sure?" without context for critical operations.

## CJ. Keyboard Efficiency

Admin/Seller: logical tab order, keyboard grid navigation, future command/search palette, discoverable shortcuts, bulk selection.

Do not compromise accessibility.

## CK. Data Freshness

Composite dashboards/workspaces communicate freshness when data is eventually consistent (search index pending, analytics updated N minutes ago, AI knowledge refresh pending).

Do not present stale projections as real-time truth.

## CL. Optimistic vs Confirmed States

Visually distinguish pending from confirmed: saving…, payment pending, inventory reservation pending, content publishing scheduled, search reindex pending.

Do not show success before authoritative confirmation.

## CM. UI Security Boundaries

Never render secrets. Avoid exposing connection refs/secrets, provider keys, internal error stacks, private IDs unnecessarily.

Admin reveals only needed operational detail.

## CN. Frontend Build Boundaries

Storefront/Admin/Seller/Customer may share one Next.js application or composed route areas initially.

Do not prematurely split into many frontend deployments. Preserve a future split if commercially needed.

Exact deployment split:

```text
NEEDS_LATER_P00_DETAIL
```

## CO. Edition Differences

Marketplace and Single-Store UI differ by policy/composition.

Do not scatter `if (marketplace)` through every component. Use edition capabilities/configuration/context.

Single-Store may hide seller/other-offer UI while reusing core commerce components.

## CP. Feature Capability Model

Frontend consumes a normalized capability model, conceptually: Marketplace enabled?, Seller portal enabled?, Multiple offers?, B2B enabled?, AI assistant enabled?

Exact mechanism later. Template route existence is not capability truth.

## CQ. Page Composition Renderer

Approved sections through a registry:

```text
SectionType → Approved Renderer → Validated Config
```

No arbitrary dynamic code import from database values.

## CR. Content Renderer Safety

Rich content uses safe rendering. No unsanitized arbitrary HTML/script execution. Heading semantics remain controlled.

## CS. SEO / UI Coordination

SEO route policy and UI navigation must agree: facet chips, pagination, canonical route, hreflang links, breadcrumbs, internal linking.

Interactive UI must not create unbounded crawlable URLs.

## CT. Performance / Design Trade-Off

```text
Visual richness is allowed,
but uncontrolled JS, media, animation and layout instability are not.
```

A visually polished product must still be fast. Do not solve performance by making UI plain/uncompetitive.

## CU. UI Acceptance Gate Criteria

Future implementation gates should reject UI when: build passes but layout is poor; mobile is broken; RTL is broken; loading/empty/error missing; accessibility is poor; screens are generic CRUD; visual hierarchy is weak; template artifacts/demo content remain; cross-module workflow is fragmented; performance is visibly degraded.

```text
Build PASS != UI ACCEPT
```

## CV. Data Ownership / UI Composition Matrix

Marks: `UI OWNER` = screen/workflow owner (not module CRUD). `DATA OWNER` = authoritative module. `COMPOSED FROM` = contracts/read models. `AUTHORIZATION SOURCE` = SpiceDB/use-case.

| Screen | UI OWNER | DATA OWNER | COMPOSED FROM | AUTHORIZATION SOURCE |
| --- | --- | --- | --- | --- |
| PDP (Storefront) | Storefront PDP | Catalog (product identity); Offer/Pricing/Inventory for commerce; Media; Content; SEO policy | Catalog, Offer, Pricing, Inventory, Reviews, Media, Content, SEO | Public + tenant/edition; purchase actions via Cart/Checkout authz |
| PLP (Storefront) | Storefront listing | Catalog taxonomy; Search index for results; Pricing/Inventory display facts | Search, Catalog, Offer, Pricing, Inventory, Media, SEO | Public + tenant/edition |
| Product Workspace (Admin) | Admin Product Workspace | Catalog write; Media; Offer; Pricing; Inventory; Content; SEO inputs | Catalog, Media, Offer, Pricing, Inventory, Content, SEO, Audit | Admin product/catalog permissions |
| Order Workspace | Admin or Seller Order Workspace | Order; Payment; Inventory events; Party | Order, Payment, Fulfillment, Inventory, Party, Audit, Support | Admin order vs seller-scoped order |
| Seller Workspace (Admin) | Admin Seller Workspace | Seller/Party; Offer; Order; Inventory; Analytics projection | Party, Seller status, Offers, Orders, Inventory, Analytics, Authorization, Audit | Admin seller-ops permissions |
| Customer Dashboard | Customer account UX | Order (customer scope); Party profile; Identity-safe | Order, Party, Notification, Support | Authenticated customer subject |
| Content Studio | Admin Content Studio | Content; Page Composition; Media; SEO inputs | Content, Composition, Media, SEO, taxonomy | Content/editor permissions; AI eligibility separate |
| Analytics Dashboard | Admin or Seller analytics UX | Analytics projections (not business SoT) | Analytics + filters (tenant/seller/market); never raw event schema | Admin analytics vs seller-scoped analytics |

Storefront/Admin/Seller/Customer are **surfaces**, not data owners. Catalog/Offer/Pricing/Inventory/Content/Media/Search/Order/Payment/Analytics/Authorization remain module owners as in existing P00 docs.

## CW. Screen Quality Matrix

Future UI tasks must identify applicable dimensions:

| Dimension | Meaning |
| --- | --- |
| Visual hierarchy | Primary action and facts are obvious |
| Information architecture | Goal-oriented, not module CRUD |
| Interaction clarity | Next action and consequences are clear |
| Mobile | Intentional, not scaled desktop |
| RTL/LTR | Architectural direction, both where supported |
| Accessibility | Semantic, keyboard, contrast, labels |
| Performance | CWV/budgets, no blocked purchase path |
| Loading | Stable skeletons/streaming |
| Empty | Explains + next action |
| Error | Recoverable, no leaked internals |
| Data freshness | Stale projections labeled |
| Authorization clarity | Denied vs missing, without leaking model |

## CX. Failure Matrix

| Case | Page still usable? | Hide/degrade section? | Retry? | Fallback? | Error boundary? | Customer-visible direction |
| --- | --- | --- | --- | --- | --- | --- |
| Partial read-model failure | Yes if critical path remains | Optional sections hide/degrade | Section retry | Last-known safe display if policy allows | Section, not full page | Honest unavailable; keep purchase if price/stock still authoritative |
| Search unavailable | PLP/search no; home/other yes | Search results area | Retry search | Category browse / popular recovery | Search island | Explain; offer browse |
| Price unavailable | PDP degraded | Price/CTA fail closed | Retry | Do not invent price | Price island | Cannot purchase until price exists |
| Inventory stale | Yes with warning | Availability messaging | Refresh | Conservative availability copy | No full-page | Show freshness; confirm at cart/checkout |
| Image failed | Yes | Gallery placeholder | Retry asset | Alt/placeholder; product still sells | Media island | Product remains purchasable |
| Unauthorized action | Surface remains | Hide or disable + deny | No for same action | Authorization-denied state | Route if whole area denied | No permission-model leak |
| Theme config invalid | Yes on safe default | Theme extras off | Operator fix | Default tokens | No | Storefront stays usable/a11y-safe |
| Translation missing | Yes | Show fallback locale/key policy | N/A | Fallback locale; never blank critical legal | No | Layout must not break |
| Client JS error | Server content yes | Interactive island degrades | Reload/retry island | Non-JS crawlable core on public pages | Feature/section | Critical SEO content already in HTML |
| Server render error | No for that route | N/A | Retry | Cached/stale only if policy allows | Route-level | Generic error; no stack |
| Slow third-party script | Yes | Isolate/defer script | No on purchase path | First-party continues | Isolation, not page crash | Purchase path unblocked |
| AI unavailable | Yes | Assistant/eligibility UI | Retry later | Non-AI workflows remain | AI island | Core commerce unaffected |

## CY. Testing Strategy — Architecture Level

Future implementation must test: desktop/mobile, RTL/LTR, screen reader/keyboard, tenant theme isolation, locale switching, market/currency display, public/private routing, SEO SSR, client island boundaries, Data Grid behavior, Product Workspace composition, Seller scope, Customer scope, loading/empty/error, visual regression, CWV budgets.

No tests now.

## CZ. Decision Summary

Do not create a final ADR yet.

### RECOMMENDED_FOR_ADR

1. UI/UX quality is a product-critical architecture concern.
2. Backend module boundaries must not dictate UI information architecture.
3. Shopeiva is reuse/reference input, not architecture truth.
4. Template areas are explicitly classified REUSE/ADAPT/REBUILD/DROP/DEFER.
5. Next.js App Router + Server Component First remains recommended.
6. Client Components are interaction islands, not default page architecture.
7. Public pages compose server-side read models/contracts.
8. A first-class Design System with validated runtime theme tokens is required.
9. One shared Single-Store publish supports tenant runtime themes.
10. RTL/LTR and mobile UX are architectural, not patchwork.
11. Accessibility is a mandatory acceptance dimension.
12. Admin Product Workspace composes Catalog/Media/Offer/Pricing/Inventory/SEO/Content coherently.
13. Seller/Admin/Customer experiences are distinct workflow products.
14. Professional typed Data Grid is a shared operational UX capability.
15. Loading/Empty/Error states are mandatory product states.
16. SEO-critical content remains server-rendered/crawlable.
17. CWV/performance budgets coexist with rich commercial UI.
18. Global client state is minimized; template Zustand is not automatically retained.
19. Page Composition uses approved renderer registry, not arbitrary executable code.
20. User-visible UI implementation requires visual evidence and Architect visual acceptance.
21. Build/test PASS alone never constitutes UI acceptance.
22. Future UI tasks must validate Desktop/Mobile/RTL/LTR/a11y/responsive states as applicable.
23. Cross-module workspaces are a reusable UX architecture pattern.
24. Marketplace/Single-Store UI variation uses capabilities/policy, not scattered conditionals.

### NEEDS_LATER_P00_DETAIL

- Exact Admin/Seller IA labels and route map
- Frontend deployment split (one app vs later split)
- Axios vs platform fetch
- Icon/chart/form/motion library KEEP/REPLACE
- Performance budget numeric values
- Capability-model mechanism
- Saved-view storage/authorization details
- Consent/third-party script policy
- Font license/asset selection

### DEFERRED

- Implementation, unzip of Shopeiva, UI code, screenshots, visual-regression vendor, final ADR, TB-P00-T022 or any next task
