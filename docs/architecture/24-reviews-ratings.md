# Tooba — Reviews & Ratings Architecture

Status:

```text
P00 architecture design — candidate for later ADR; not an ADR lock; not a Gate
```

Task:

```text
TB-P00-T025
```

Documentation only. No review APIs, schemas, moderation engine, AI summaries, seller-response UI, helpful votes, Shopeiva import, T026, or P00 Gate.

```text
Product Review != Seller Review
Review != Rating Aggregate
Review != Catalog
Review != Order
Review != Analytics
Backend/module boundary != UI boundary
Modular monolith; no cross-module DB joins
```

## A. Core Separation

**Catalog** owns Product/Variant truth. It does not own review rows, star columns, or review counts as write-model fields.

**Seller / Offer** owns seller identity and commercial offer identity. Seller score is seller-scoped, never mixed into Product aggregate.

**Order** owns purchase truth. Reviews may consume an approved verified-purchase **contract/projection**, never Order tables.

**Reviews** owns review/rating submissions, moderation/publication state, and rebuildable rating projections.

**Analytics** observes review-related behavior; it does not own ratings or publication.

**Authorization (SpiceDB)** governs who may submit/moderate/view metadata. UI visibility is not the security boundary.

**Fraud/Risk** may later supply abuse signals. Reviews still owns moderation business state.

## B. Review Types

Required now:

```text
Product Review
Seller Review
```

Future types (not unified into one generic “feedback” table without semantic type):

```text
Content Review/Feedback
Fulfillment/Delivery Feedback
Support Feedback
```

Only Product and Seller review architecture is required in this document.

## C. Product Review

Conceptual fields (not a schema): ProductId; optional VariantId; author Party/Identity reference; Order/OrderLine verification reference; rating; title; body; future pros/cons and media refs; locale; moderation state; created/updated timestamps.

Review text is **not** Catalog descriptive truth.

## D. Seller Review

About seller/service experience (delivery experience, seller service, accuracy, packaging, communication as possible dimensions later).

Do not mix seller rating into Product aggregate. One product may have many sellers; seller score stays seller-scoped.

## E. Verified Purchase

Verified purchase derives through an approved Order (and, where needed, Fulfillment) contract/projection.

```text
Review module does not query Order tables directly.
```

Conceptual evidence: opaque OrderId / OrderLineId; Buyer PartyId; Product/Variant; Seller where relevant; delivered/eligible state.

Exact eligibility timing:

```text
NEEDS_LATER_P00_DETAIL
```

## F. Review Eligibility

Policy candidates (not locked): must have purchased; must have received/delivered; one review per order line/product; editable window; guest review; anonymous display; seller review after fulfillment.

Architecture vs policy: keep eligibility as Reviews rules over **contract facts**, not Order writes.

## G. Rating Model

Preserve overall rating now; optional dimensions later. Do not overbuild multi-dimensional ratings.

Use an explicit numeric/range invariant. A 1–5 integer scale is **recommended** for first-sale UX familiarity; it is **not** silently locked as an ADR until Architect/USER confirms.

## H. Aggregate Rating

```text
Aggregate rating is a derived projection.
```

Do not store canonical average on Product as Catalog truth.

Projection contents: count, average, distribution; Bayesian/confidence-adjusted score later.

Search, SEO, and PDP consume the projection, not Catalog writes.

## I. Aggregate Rebuild

Aggregates must be recomputable from authoritative **published** reviews. If projection is lost or corrupt: **rebuild**. No irreversible counter-only architecture.

## J. Moderation Lifecycle

Concepts (enum not locked): Submitted; Pending Moderation; Published; Rejected; Hidden; Edited; Reported.

Conceptual transitions (not locked names):

```text
Submitted -> Pending Moderation
Pending Moderation -> Published | Rejected
Published -> Hidden | Edited (re-queue or stay published per policy)
Rejected -> (optional resubmit as new revision / remain rejected)
Reported -> Pending Moderation (or flagged overlay)
Hidden -> Published (restore) | remain Hidden
```

Moderation action is immediately authoritative in Reviews. Public consumers converge via events/projections.

## K. Moderation Policy

Potential checks: spam, abuse, profanity, PII, advertising, irrelevant content, fake review, duplicate review, seller self-review.

Do not implement automation. AI moderation may assist later; it is not sole authority for sensitive decisions unless later policy says so.

## L. Publication

Only published/approved reviews contribute to public aggregate and SEO.

Hidden/rejected/draft/pending must not leak into PDP, Search, SEO structured data, or public AI summaries.

## M. Editing

If editing is allowed: version/history or at least auditability. Do not silently replace published text with no trace when it affects moderation/publication.

Exact revision model: `NEEDS_LATER_P00_DETAIL`.

## N. Deletion / Withdrawal

Concepts: author withdrawal; moderator removal; account deletion/anonymization; legal removal later.

Do not hard-delete in a way that breaks audit or projection consistency without policy. Prefer withdraw/hide + rebuild.

## O. Author Identity vs Display Identity

Separate internal author reference from public display identity.

Do not expose email, phone, or full private identity.

Public display candidates (policy later): first name, masked name, nickname, anonymous label.

## P. Tenant Isolation

Single-Store: reviews are tenant-scoped.

Marketplace: marketplace-scoped with Product/Seller references.

No cross-tenant review leakage. Seller isolation: a seller must not read other sellers’ moderation metadata or unpublished reviews.

## Q. Locale

Review body has locale/language context. Do not auto-translate and present as original user text without labeling.

Translated rendering is a separate future projection/service.

Locale ≠ Market ≠ Currency; review locale is language of the text, not a market id.

## R. Product / Variant Semantics

A review may attach to Product with optional Variant context.

Preserve variant reference. Do not automatically fragment Product aggregate by variant unless product policy requires.

Exact aggregation policy:

```text
NEEDS_LATER_P00_DETAIL
```

## S. Seller / Offer Semantics

Product review is not implicitly seller-specific unless it captures seller experience as a **Seller Review** (or an explicit seller-scoped field later).

Seller review may reference SellerId and Order/Seller fulfillment context. Offer identity may be **evidence**, not the public review root.

## T. Order / Fulfillment Integration

Verified purchase and seller-review eligibility may depend on Order and Fulfillment delivered state via contracts/events/projections.

Reviews **must not** write Order or Fulfillment state.

## U. Fraud / Abuse Boundary

Fraud/Risk may later signal review abuse, brigading, fake accounts, velocity.

Reviews still owns moderation/business state. Authorization and Fraud are not moderation.

## V. Authorization

SpiceDB should govern: submit review; edit own review; withdraw own review; moderate; publish/reject/hide; view moderation metadata; seller respond to own seller-review context (future).

UI visibility is not the security boundary.

## W. Seller Response

Preserve future seller response.

```text
seller may respond only to seller-scoped/authorized review context
response is not review rewrite
customer review remains immutable/audited according to policy
```

Exact first-release scope:

```text
DEFERRED
```

## X. Helpful Votes

Helpful / not helpful is not a first-release requirement. If added later: separate interaction signal/projection, not review mutation.

## Y. Reporting / Flagging

Customers/sellers may report. Future: report reason, reporter, status, moderation queue, abuse protection. No implementation now.

## Z. Search Integration

Search may consume rating average and review count as **projection**. Search does not own aggregate truth. Ranking may use rating later through versioned policy.

## AA. SEO Integration

SEO structured data may consume AggregateRating and Review **only** from published/eligible projections.

Do not fabricate counts/ratings. Structured data must match visible page truth.

## AB. AI Integration

AI may summarize later. Requirements: published-only source; source provenance; no invented quotes; seller/product separation; locale awareness; authorization/public scope.

AI summary is derived content, not review truth.

## AC. Analytics

May observe: review viewed, review submitted, rating distribution interaction, helpful vote later, review–conversion correlation.

Analytics does not own published state or aggregate.

## AD. Notifications

Reviews emits facts/intents (published, rejected, seller response later, moderation action). Notification capability chooses channel/provider/template. Reviews must not couple to an SMS/email vendor.

## AE. Audit

Durable audit for: publish, reject, hide, restore, moderator edit, seller-response moderation.

Need actor, reason, target, time, correlation. Technical logs are not sufficient. See `docs/architecture/18-observability-logging-audit.md`.

## AF. Admin Moderation UX

Do not build raw Review CRUD.

Moderation Workspace: queue, filters, Product/Seller context, verified-purchase badge, rating, content, flags/reports, author **safe** summary, moderation reason, approve/reject/hide, bulk actions where safe, audit/history.

```text
Backend/module boundary != UI boundary
Build PASS != UI ACCEPT
Functional PASS != Visual ACCEPT
Desktop PASS != Mobile PASS
LTR PASS != RTL PASS
```

## AG. PDP UX

PDP composition via contract: aggregate score, count, distribution, filters/sort later, verified-purchase label, review cards, variant context where useful, pagination/load more, empty/loading/error, mobile, RTL/LTR, accessibility.

Do not dump unstructured text. Reviews unavailable must not necessarily break product + price + purchase (`20-frontend-ux-template-adaptation.md`).

## AH. Seller UX

Seller panel may show seller-review insights and authorized responses later.

Sellers must not moderate/hide customer Product reviews unless later policy explicitly permits and is audited.

## AI. Customer UX

Customer dashboard: reviews written, eligible products/orders to review, edit/withdraw where permitted, moderation state in human language (not backend enums).

## AJ. Sorting

Public sort candidates later: newest, highest, lowest, most helpful, verified purchase. Exact set later. Do not sort by hidden moderation/internal signals.

## AK. Pagination

Scalable pagination required. Do not load all reviews client-side. Cursor/search-after later; mechanism not locked.

## AL. Cache

Published aggregates/lists may be cached. Invalidate/version on publish, hide, delete/withdraw, rating change, moderation change.

Private moderation queues use different cache/security policy. See `docs/architecture/19-caching-infrastructure-abstractions.md`.

## AM. Eventual Consistency

PDP/Search/SEO aggregate may lag slightly after publication. Bounded eventual consistency.

Moderation action is authoritative immediately in Reviews. Public projection must converge quickly.

## AN. Events

Candidate facts (names not locked): ReviewSubmitted, ReviewPublished, ReviewHidden, ReviewRejected, ReviewUpdated, SellerReviewPublished, RatingAggregateChanged.

Consumers: Search, SEO, cache invalidation, Analytics, Notifications, AI projections.

## AO. Abuse / Rate Limiting

Future: submission rate, duplicate text, same-order repeat, account velocity, IP/device signals.

Do not store abuse decisions in Catalog. No provider selection here.

## AP. Privacy

Public reviews must not expose email, phone, address, or private order details.

Moderation/admin access to author context is permissioned (SpiceDB).

## AQ. Right-to-Erasure / Anonymization Readiness

If later identity deletion requires anonymization: author display anonymization; internal reference retention; audit/legal retention.

Do not invent legal policy now. Architecture must allow anonymization without collapsing audit/history.

## AR. Data Ownership Matrix

| Fact | Reviews | Catalog | Seller | Order | Fulfillment | Party | Authorization | Search | SEO | AI | Analytics | Notifications | Audit |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| review text | OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | REFERENCE | CONSUMER | NOT_OWNER | CONSUMER (published) | CONSUMER (published) | NOT_OWNER | NOT_OWNER | CONSUMER |
| rating | OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | CONSUMER | PROJECTION | PROJECTION | CONSUMER | NOT_OWNER | NOT_OWNER | CONSUMER |
| aggregate rating | OWNER (projection) | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | CONSUMER | PROJECTION | PROJECTION | CONSUMER | NOT_OWNER | NOT_OWNER | NOT_OWNER |
| verified purchase evidence | CONSUMER | NOT_OWNER | REFERENCE | SOURCE | SOURCE (delivered) | REFERENCE | CONSUMER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | CONSUMER |
| product truth | REFERENCE | OWNER | NOT_OWNER | REFERENCE | NOT_OWNER | NOT_OWNER | CONSUMER | CONSUMER | CONSUMER | CONSUMER | CONSUMER | NOT_OWNER | NOT_OWNER |
| seller truth | REFERENCE | NOT_OWNER | OWNER | REFERENCE | REFERENCE | REFERENCE | CONSUMER | CONSUMER | CONSUMER | CONSUMER | CONSUMER | NOT_OWNER | NOT_OWNER |
| purchase truth | REFERENCE | NOT_OWNER | NOT_OWNER | OWNER | CONSUMER | REFERENCE | CONSUMER | NOT_OWNER | NOT_OWNER | NOT_OWNER | CONSUMER | NOT_OWNER | CONSUMER |
| moderation state | OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | CONSUMER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | CONSUMER (intent) | CONSUMER |
| search rating projection | SOURCE | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | CONSUMER | OWNER (index copy) | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER |
| structured data | SOURCE | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | CONSUMER | NOT_OWNER | OWNER (page emit) | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER |
| AI summary | SOURCE | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | CONSUMER | NOT_OWNER | NOT_OWNER | OWNER (derived) | NOT_OWNER | NOT_OWNER | CONSUMER |
| behavioral metric | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | CONSUMER | NOT_OWNER | NOT_OWNER | NOT_OWNER | OWNER | NOT_OWNER | NOT_OWNER |
| notification delivery | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | REFERENCE | CONSUMER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | OWNER | CONSUMER |
| moderation audit | SOURCE | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | REFERENCE | CONSUMER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | OWNER |

## AS. Failure Matrix

| Failure | fail closed? | retry? | degrade? | queue? | customer-visible | admin alert? |
| --- | --- | --- | --- | --- | --- | --- |
| Order verification unavailable | Yes for **new** verified-required submit | Yes | Unverified submit only if policy allows | Optional | Cannot submit as verified | Yes if prolonged |
| Fulfillment verification unavailable | Yes for delivery-gated seller/product review | Yes | Delay eligibility | Yes | Eligible-later state | Optional |
| Duplicate review submission | Yes (reject second) | No | Idempotent first wins | N/A | Duplicate error | No |
| Moderation service unavailable | Public unchanged | Yes | Queue new submits as Pending | Yes | Submitted / pending | Yes |
| Aggregate projection lag | No | Rebuild/retry | Stale public score briefly | Rebuild job | Possibly stale stars | If SLA exceeded |
| Cross-tenant review request | Yes deny | No | Empty/deny | N/A | Not found / deny | Yes |
| Search shows stale rating | No | Reindex | Stale search facet | Reindex | Search stale | If SLA exceeded |
| SEO stale aggregate | No | Refresh | Stale JSON-LD briefly | Refresh | Must converge to visible | If SLA exceeded |
| AI summary stale | No | Rebuild | Omit summary | Rebuild | Hide or previous published | Optional |
| Unauthorized seller response | Yes deny | No | N/A | N/A | Denied | Yes |
| Abuse/rate limit exceeded | Yes | Backoff | N/A | N/A | Rate-limit error | If repeated |

## AT. Testing Strategy — Architecture Level

Future implementation must test: product vs seller review; verified purchase; unverified policy; publish/reject/hide; aggregate rebuild; tenant isolation; seller isolation; variant context; duplicate race; SEO aggregate truth; Search projection; AI published-only; privacy masking; mobile/RTL review UX.

No tests in this task.

## AU. Decision Summary

| # | Decision | Classification |
| --- | --- | --- |
| 1 | Product Review and Seller Review are distinct | RECOMMENDED_FOR_ADR |
| 2 | Reviews owns submission/moderation; Catalog does not own review rows | RECOMMENDED_FOR_ADR |
| 3 | Rating aggregate is a rebuildable projection, not Product write-model | RECOMMENDED_FOR_ADR |
| 4 | Verified purchase via Order/Fulfillment contracts, never cross-module joins | RECOMMENDED_FOR_ADR |
| 5 | Only published reviews feed public/search/SEO/AI | RECOMMENDED_FOR_ADR |
| 6 | Author identity separated from public display | RECOMMENDED_FOR_ADR |
| 7 | Single-Store tenant isolation; Marketplace product/seller scopes | RECOMMENDED_FOR_ADR |
| 8 | Variant context preserved; no forced variant-fragmented aggregates | RECOMMENDED_FOR_ADR |
| 9 | Seller response ≠ review mutation; authorization-scoped | RECOMMENDED_FOR_ADR |
| 10 | Search and SEO consume rating projections only | RECOMMENDED_FOR_ADR |
| 11 | AI summaries: published sources, provenance, no fabricated quotes | RECOMMENDED_FOR_ADR |
| 12 | Moderation SpiceDB-authorized and durably audited | RECOMMENDED_FOR_ADR |
| 13 | UX is moderation/workflow/PDP, not CRUD | RECOMMENDED_FOR_ADR |
| 14 | Projections cacheable; invalidate/version from lifecycle | RECOMMENDED_FOR_ADR |
| 15 | Abuse controls are extension points, not Authorization | RECOMMENDED_FOR_ADR |
| 16 | Public review data minimizes PII | RECOMMENDED_FOR_ADR |
| 17 | Events support Search/SEO/Analytics/Notifications/AI | RECOMMENDED_FOR_ADR |
| 18 | Future deletion/anonymization without collapsing audit | RECOMMENDED_FOR_ADR |
| 19 | Build/test success is not UI acceptance | RECOMMENDED_FOR_ADR |
| 20 | Backend/module boundary does not dictate review UX | RECOMMENDED_FOR_ADR |
| — | Exact verified-purchase timing | NEEDS_LATER_P00_DETAIL |
| — | Exact variant aggregation policy | NEEDS_LATER_P00_DETAIL |
| — | Exact rating scale lock | NEEDS_LATER_P00_DETAIL |
| — | Exact revision model | NEEDS_LATER_P00_DETAIL |
| — | Seller response first-release | DEFERRED |
| — | Helpful votes | DEFERRED |
| — | Legal erasure policy | DEFERRED |

Do not create a final ADR in this task.

## P00 Gap Status After Reviews

```text
Returns / RMA = still requires dedicated architecture task
Tax = still requires USER product decision before Gate
Notifications = boundary sufficient for P00
Fraud / Risk = boundary sufficient for P00
Support = deferred post-P00 unless USER changes scope
```

Reviews / Ratings architecture is documented here and awaits Architect ACCEPT of TB-P00-T025.

Cursor does not issue T026 or P00-GATE.
