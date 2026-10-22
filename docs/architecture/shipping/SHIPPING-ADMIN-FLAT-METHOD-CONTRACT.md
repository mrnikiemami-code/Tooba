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

## LOCKED ADDENDUM — Independent monetary status and opt-in constraints (user approved 2026-10-16)

This section is NORMATIVE and supersedes any earlier interpretation that a missing restriction means a disabled method, or that a provider-level rate/restriction may silently apply to sibling methods.

### A. Each selectable shipping method has ONE explicit financial mode
Every independently administered canonical method (e.g. `tipax:standard`, `tipax:express`) MUST have an unambiguous, server-authoritative monetary configuration and validation state. Supported business modes to model explicitly (subject to existing capability/approved integration):
- `FREE_TO_CUSTOMER`: customer shipping charge is 0 by an explicit rule, not by absent data; the responsible seller/platform/carrier cost payer must be separately tracked or pending approved finance policy.
- `FIXED_AMOUNT`: a declared amount and currency (IRR with canonical Money precision/rounding) for this method.
- `RULE_BASED_RATE`: its own valid tariff rules; ambiguity or no matching required price is NOT free.
- `COLLECT_ON_DELIVERY`: explicitly customer-payable to carrier at delivery; expose any advance charges and payer responsibility distinctly, never pretend the full cost is zero.
- `PROVIDER_QUOTE`: a verified carrier quotation from an authorized integration with identity, validity and failure rules; do not fabricate amounts or delivery promises.

No implicit financial-mode inference from an empty tariff list or provider name. Missing, incomplete, conflicting or unavailable monetary policy MUST be shown in Admin as `NEEDS_FINANCIAL_CONFIGURATION`/unquotable and fail closed for checkout quote/selection as appropriate, not silently priced at zero. The method's enabled flag and quote-ready/financial-ready status are DISTINCT; an enabled method is not necessarily customer-selectable until its mandatory pricing/commitment requirements are satisfied. Preserve existing accepted order snapshots and financial PendingAllocation invariants; never activate carrier payout or allocation shares without an approved policy.

### B. Unconfigured optional constraints default to UNRESTRICTED PER METHOD
For an enabled method, absent optional restrictions mean **no admin-configured restriction for that method** on geography (all supported cities/provinces), parcel weight/dimensions, eligible weekdays, order-value thresholds, and other optional dimensions. Restriction configuration is opt-in. When an admin adds explicit restrictions, they narrow ONLY the selected canonical method. Removing all configured restrictions restores that dimension's unrestricted state. Distinguish `NO_RESTRICTIONS` from `UNCONFIGURED_REQUIRED_POLICY` and from `DENY_ALL`; an empty include-list must not accidentally mean zero eligible cities/days/weights.

'Unrestricted' does NOT override verified carrier capabilities, legal/physical shipping limits, server-side tenant/seller eligibility, calendar/cut-off/capacity authority or missing financial/delivery-promise prerequisites. 'Every day' means no extra admin blacklist, not a guaranteed carrier pickup/delivery slot. Never invent infinity-based physical claims or an unsupported guaranteed date.

### C. Hard method isolation — NO sibling or provider policy inheritance
Every rate, geographic condition, weight/dimension rule, working-day rule, packaging rule, payment responsibility and operational policy MUST be scoped using the selected **canonical method identity** (and its authorized server binding), with tenant/seller context where required. No sibling method inherits or is edited by these settings, even when both methods share a carrier/provider. Provider identity is shared METADATA ONLY. Example: `tipax:standard` restricted to Tehran and <=20 kg at a fixed fee MUST NOT restrict or price `tipax:express`.

Old provider/service-level policies that cannot be unequivocally attributed to one canonical method MUST be marked `LEGACY_SHARED_UNRESOLVED`, kept readable/auditable, and NOT silently projected as independently owned policies. Migration/compatibility and any conversion of genuinely shared historical rules require explicit reviewed mapping rather than accidental inheritance, deletion or duplication. Persist and retrieve method-specific constraints consistently in Admin, storefront quotation and final Order/Fulfillment snapshot.

### D. Acceptance / prohibition
1. Enable a method with zero optional restrictions: all supported geography, optional weights/dimensions and eligible weekdays remain unconstrained BY ADMIN for that method, while backend financial status remains explicit.
2. Save a geographic/weight/schedule restriction for `tipax:standard`: `tipax:express` remains unchanged in both Admin and actual server quote/eligibility.
3. Delete that restriction: that specific dimension returns to unrestricted state; do not affect pricing mode or other dimensions.
4. Empty tariffs without an explicit FREE mode cannot produce a zero customer quote or fake promised slot.
5. Fixed charge, free-to-customer, on-delivery and provider-quoted semantics are separate; all customer-visible totals, seller obligations and pending allocations remain consistent.
6. A missing/untrusted server binding must fail closed for WRITES; never derive method ownership by splitting a code string in the browser.

This addendum is a LOCKED REQUIREMENT; it is NOT a claim that the present shipping backend supports all modes or correct isolation. Code changes require separate scoped implementation and real write/read and checkout acceptance.


## Architect clarification — operational classification metadata (TB-195, 2026-10-22)
Shared operational shipping-type identity/display metadata (for example a configurable heavy category) may be referenced by several independently managed methods. This is NOT a provider/child-service hierarchy or a policy container: it owns no rate, coverage, packaging, schedule, payment or capacity rule and grants no sibling/provider/cross-owner inheritance. Sections B/C isolation remains fully enforced. No standalone former "Types of Service" tab or nested provider editor is restored. This clarification authorizes no runtime grouping, migration of legacy policy or customer quote change; the separate bounded task governs implementation.
