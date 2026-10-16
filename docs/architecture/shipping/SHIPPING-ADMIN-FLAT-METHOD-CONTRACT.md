# Shipping Admin — Flat, independently managed shipping services

Status: LOCKED ARCHITECTURAL DECISION — USER APPROVED 2026-10-16
Applies to: /admin/shipping-services and its service-detail administration UX, Shipping module contracts, storefront catalog projections where applicable.
Priority: this decision governs Admin redesign. It does NOT authorize destructive backend/schema changes.

## Domain identity and fundamental rule
**ONE CUSTOMER-SELECTABLE SHIPPING METHOD = ONE INDEPENDENT ADMIN-MANAGED SHIPPING SERVICE.**

Examples:
- Post Express = `post:express` — independent configured service.
- Post Standard = `post:standard` — independent configured service.
- Tipax Express = `tipax:express` — independent configured service.
- Tipax Standard = `tipax:standard` — independent configured service.
- Store courier = `store_courier`, pickup = `in_person` — independently configured methods as applicable.

A provider/carrier such as Post or Tipax may be shared **metadata/provider identity**, NOT the parent administrative entity that owns the price, coverage, schedule or packaging of all its service variants. Different methods from the same provider may have different commercial rates, geography, constraints, time commitments and packaging.

This is explicitly NOT a two-level parent-service -> child service-type management UX. Do not restore a generic 'Post' row with a child 'Express/Standard' editor as the mandatory administrative model. Do not use a standalone 'Service Types' tab in the global services listing, or insert unrelated tabs above the entire catalog. Do not incorrectly interpret one-level to mean that all methods must share configuration.

## Admin navigation and information architecture
Level 1 = the FLAT LIST at `/admin/shipping-services`: each row is a selectable shipping method, e.g. Post Express, Post Standard, Tipax Express, Tipax Standard. Present a clean normal table with name, canonical code, active status and ONLY THREE row actions: **View**, **Edit**, **Delete**. Keep the New Service action. Remove the existing redundant per-row action buttons for rate, timetable, packaging, coverage, etc.; those belong in the individual service editor. Fix table overflow/large blank column gap. Reuse existing Admin visual tokens, component primitives and RTL responsiveness.

Level 2 = the DETAIL of ONE selected independent method (for example `post:express`), using one horizontal, NON-NESTED tab row inside its own management view:
1. General — identity, localized name, provider identity, enabled status.
2. Rates & Coverage — destination/zone, rate policies and constraints ONLY for this method.
3. Transport Schedule — eligible windows, operational schedules ONLY for this method.
4. Packaging & Shipping Fee — packaging policy, applicable freight/fee handling ONLY for this method.
If an existing real field/section cannot be mapped safely, document it and keep it accessible; don't delete capabilities to match the mockup. A reusable provider identity may be shown in General but must not force shared per-method settings.

NO 'Types of Service' tab: the former child types become individual manageable methods in the list. The Provider can group/filter rows for convenience but shall not own rate/schedule/packaging by default.

## Canonical identity and migration safety
- Preserve recognized external/internal method codes (`post:express`, `post:standard`, `tipax:express`, `tipax:standard`, etc.) and their mapping to Checkout/Order/Fulfillment/Payment/Settlement. A visual flattening must not break any stable code, order snapshot or historical booking.
- Inspect and reconcile CURRENT backend model (provider/service + variant) before changing entities or DB. Do not assume UI-only flattening is sufficient; do not delete tables, child records or policies in this approval phase. Explicitly map old carrier/service/option identifiers to canonical flat-method identity. Compatibility, historical orders and existing API consumers must survive.
- Tenant, edition, destination and seller eligibility stay server-authoritative. No client price/ETA invention.

## Two-phase delivery gate — mandatory user acceptance
**Phase 1 — WIREFRAME + VISIBLE ADMIN SHELL ONLY**: show the correct FLAT method list and exact three row actions; a selected method opens its own detail view with the four top-level, non-nested tabs, using current Admin styling. No migration of rate/schedule/packaging forms yet; no unrelated Checkout/Settlement task; show actual browser screenshots and wait for explicit USER APPROVAL. A conceptual picture alone does not count as a shipped UI.
**Phase 2 — AFTER explicit approval only**: migrate each currently scattered form and its existing behavior/API into the appropriate independent method tab, preserving validation/persistence, and remove obsolete nested modal entry points; focused acceptance and real browser proof.

Any implementation that merely redraws the old generic parent-service with child types and scattered popups is NOT PASS. A worker must stop for architect/user review after Phase 1 before proceeding.

## Conflict resolution
This decision supersedes all earlier planning/mockups that present the five tabs 'Services / Service Types / Rates / Transport Schedule / Packaging' as peers ABOVE a generic services list, and supersedes designs in which Post is the only managed service while express/standard are editable solely as children. It does not supersede existing approved per-item Checkout shipping grouping or Settlement PendingAllocation financial safeguards.

## Immediate acceptance
The current screenshot of `/admin/shipping-services` showing 5 provider rows, many action icons and separate deep editors is CURRENT/REJECTED for this target UX. The required output is a method-level list plus a per-method horizontal single-level tab editor. The user must verify the actual interface before authorization to move forms.
