# TYPE-DECISION-PROPOSAL — operational shipping type

Status: **PROPOSED** by Worker. Not APPROVED, not a product rule, not implementation authorization.
Date: 2026-10-22 · Parent: TB-SHIPPING-ADMIN-METHOD-MODE-DRAFTS-194 · Base: `4065fa89d`.

## 1. Concept mapping (exact, non-overlapping)

| Concept | What it is | Current owner (source) | Must not be confused with |
| --- | --- | --- | --- |
| Operational shipping type | Owner-visible classification of how a method operationally works (e.g. ordinary / heavy / very heavy / grocery — *examples only, not a closed list*) | **No owner today** (absent) — recommendation below | price band, weight tier, carrier name, provider child service |
| Canonical shipping method | One customer-selectable method identity, `serviceCode`(+`optionCode`) | Fulfillment (`ShippingMethod`/`ShippingService`/`ShippingServiceOption` + `LanguageId` translations), HTTP-owned | provider parent entity, type |
| Carrier service / provider product | External carrier catalogue (`post:express`, `tipax:standard`) + provider metadata | Fulfillment catalog + `ShippingProviderMetadata` adapter metadata | our operational taxonomy |
| Owner-local rule draft | Owner-scoped rules for one bound method (regions, rate rows, presentation, weekly schedule, payment/calculation settings) | Shipping (`authoring_drafts` aggregate) | shared definitions |
| Seller responsibility / inbound warehouse flow | Who ships from where, inbound flow | **UNKNOWN / not found** in Shipping/Fulfillment shipping tables | type, method |
| Membership benefit ("Plus") | A promotion/membership entitlement | Promotion/Offer domain (not this program) | type |
| Customer delivery group | Per-seller/per-origin/per-compatibility group with own cost and own time choice | Shipping grouping + Order snapshots (`MethodCode` only) | type alone |

## 2. Lock reconciliation (exact)

Locks involved:

1. `SHIPPING-ADMIN-FLAT-METHOD-CONTRACT.md` — Status: `LOCKED ARCHITECTURAL DECISION — USER APPROVED
   2026-10-16`; line "NO 'Types of Service' tab: the former child types become individual manageable
   methods in the list." Plus §B/§C: unconfigured optional constraints default to
   `UNRESTRICTED PER METHOD`; hard method isolation, no sibling/provider inheritance.
2. `SHIPPING-ADMIN-RULES-DECISION.md` (`SHIPPING-ADMIN-RULES-001`) §3 — owner-approved 2026-10-22:
   "shipping type" sits **above** the method name; the four examples are **not** a closed global
   list; taxonomy must be defined "in the executable architecture with a named owner"; type must not
   be guessed from name/weight in the browser; a valid item/offer → type binding is required;
   type does not by itself determine customer groups. §10 requires translated type name.

Reconciliation — what is compatible and what is not:

- **Compatible without amendment:** a *new* operational classification surface is not the removed
  provider→child-service-type tree, and it is not the standalone catalog "Service Types" tab. The
  older lock's prohibitions (nested provider children, one provider row owning its variants'
  rate/schedule/packaging, parent-child Admin UX, a global tab above the catalog) remain **fully in
  force** and must be preserved by any implementation.
- **Requires explicit Architect amendment (not silently decided here):** the flat-method lock's §B/§C
  hard method isolation was written when no cross-method owner-level classification existed. A type
  that is *shared by several methods of one owner* is the first genuinely cross-method owner concept.
  It is compatible with §C only if the type carries **no** rate, coverage, packaging, schedule or
  payment authority and if type-scoped restriction is never inherited by an unassigned method.
  Whether that is allowed as-is, or needs one explicit sentence added to the lock, is an Architect
  decision. Worker recommendation: an explicit one-line amendment, not a reinterpretation.
- **Must not be done:** replay TB-187-W0 as current work; delete the older lock; declare all older
  constraints superseded; seed the four example labels as global categories; or append the proposal
  to `SHIPPING-ADMIN-RULES-DECISION.md` as if approved.

`UNKNOWN` in source: whether the value set is platform-curated or per-owner authored; whether a type
is mandatory before a method can be enabled; whether type restricts method/item compatibility in the
first release; whether a type is scoped per store or per owner-with-seller (marketplace).

## 3. Preferred model (one option only; one alternative below)

**Preferred: shared type *definitions* in Fulfillment + owner-local *assignment* in Shipping.**

Why this split fits the actual source:

- The type is a **shared, cross-owner identity** with multilingual display text. Today the only place
  in the repository that owns a language-keyed shipping-named catalog with lifecycle, codes and
  `LanguageId` translations is the Fulfillment shipping catalog (`shipping_methods` +
  `shipping_method_translations`). Duplicating that mechanism per owner (or per draft) would create
  the "second catalog" and the "owner-local copy of a shared identity" both forbidden by the task.
- The **assignment** ("this owner's method for `tipax:standard` is *heavy*") is owner-local rule data,
  exactly like the existing method settings/schedule: one row per draft root, revisioned, explicitly
  UNCONFIGURED when absent. It belongs on `ShippingAuthoringDraft`, not in the shared catalog,
  because the same canonical method may legitimately be classified differently by two owners, and
  cross-owner inheritance is forbidden.
- Cross-module direction stays `Shipping → Fulfillment.Contracts`, an edge that already exists
  (`Tooba.Shipping.Infrastructure.csproj:22`). No FK, no join, no shared DbContext: existence and
  display text are resolved through a Contracts reader in the Shipping adapter, ids stay opaque.

Shape (PROPOSED, names illustrative, not final):

- Fulfillment `shipping_types`: `shipping_type_id` (Guid v7, stable), `code` (canonical machine code,
  lowercase/underscore, unique), `is_active`, `sort_order`, `created_at`, `updated_at`; no price,
  no region, no schedule, no currency.
- Fulfillment `shipping_type_translations`: `(shipping_type_id, language_id)` unique, `name`,
  optional `description`; `language_id` supplied by caller from the DB-backed language registry
  (`ILanguageLookup`/context), never a hardcoded fa/en pair.
- Fulfillment Contracts: `IShippingTypeReader` (`ListActiveAsync`, `GetByIdsAsync`) returning
  `(ShippingTypeId, Code, SortOrder, Translations[])`; fail-closed empty on gate/absence, mirroring
  the existing `IShippingOneLevelCatalogReader` precedent.
- Shipping draft: nullable `shipping_type_id` on the draft root (or a nullable singleton row, as
  TB-193/194 did). Absent = explicitly `UNCONFIGURED`, never a synthetic default type. Written
  through a dedicated GET/PUT `.../authoring-drafts/{draftId:guid}/method-type` on both Admin and
  Seller route groups, owner-authorizer before `ISender`, identical PUT = truthful no-op on revision,
  stale revision = 409. Revising the type never touches regions, rate rows, currency binding,
  translations, weekly schedule or method settings.
- Lifecycle: deactivate, never destructive delete while referenced. No inferred classification of
  existing drafts/legacy rows — they stay `UNCONFIGURED` and readable.

Alternative (only if the Architect rejects shared definitions): owner-scoped definitions in Shipping
(`authoring_types` + `authoring_type_translations` keyed by owner). This keeps everything in one
module and one transaction, but duplicates the language-keyed catalog mechanism per owner, makes
"common type" impossible without cross-owner copying, and requires deciding shared-vs-owner
promotion later. It is recorded only because "platform-curated vs owner-authored definitions" is
genuinely undecided (D2).

## 4. Authority and visibility per edition

| Actor | Common type definitions | Bind type to their own method |
| --- | --- | --- |
| Platform/Admin (SingleStore and Marketplace) | Create/deactivate/translate **only if** the Architect chooses platform-curated definitions | Admin authors the store owner draft |
| Seller (Marketplace) | **No** taxonomy administration merely because seller draft authoring exists | Only the seller's own draft, via the existing seller owner scope/authorizer |
| Store owner (SingleStore) | Same as platform row — no new owner/mode trust from browser | Own draft only |

- Edition, owner and scope stay server-authoritative from the already-implemented
  `IShippingAuthoringContextSource` / owner authorizer; nothing is derived from a browser-supplied
  owner/mode/edition, and no "admin picks any seller" permission is invented.
- Reading active types is allowed to every authoring audience (it is a shared, non-secret catalogue);
  `fulfillment.view` gates reads and `fulfillment.manage` gates the write, matching TB-194.

## 5. Configurable vs fixed; remaining product decisions

Fixed (safe, source-consistent): id/code identity independent of text and language; display text
`LanguageId`-keyed; type carries no money, geography, schedule, capacity or promise; type alone never
determines delivery groups; no universal enum.

Necessary product decisions (cannot be inferred from source):

1. **D2-a Definition authority:** platform-curated shared definitions (**recommended**) vs
   per-owner authored definitions vs hybrid (platform defaults + owner additions).
2. **D2-b Mandatory or optional:** can a method be enabled with `UNCONFIGURED` type, or is a type a
   precondition for activation?
3. **D2-c Compatibility semantics:** does type restrict which methods are selectable for an item/offer
   in the first release, and if so is the item→type binding authoritative in Catalog/Offer (new
   cross-module reference) or later?
4. **D2-d Marketplace scope:** one taxonomy per store, or per seller (and can a seller see the
   store's shared types without administering them)?
5. **D2-e** Whether the flat-method lock needs the explicit one-line amendment in §2 above.

## 6. Consumer and scope effects (not implemented here)

- Type may later constrain compatible methods/items and *contribute* to customer grouping; grouping
  still requires responsibility, real origin, item compatibility, available method and the
  customer's independent date/window choice.
- In authoring, type sits above method name; it is **not** a freight tariff tier, not a region
  selection, not a payment/calculation mode. Geography remains once per region group, multiple
  conditional rows remain beneath that group, and no geography/schedule/language copies per type.
- Existing consumers (`Order`, `Cart`, `Settlement`, Storefront) read `MethodCode` only; type must be
  read-only for them until an independently authorized task changes that.

## 7. Uniqueness / compatibility

- No change to `ux_authoring_drafts_owner_binding`; no change to `ux_method_coverages_service_option`,
  `rate_policies` or `packaging_policies` invariants. The assignment is a new nullable column/row, not
  a re-key.
- No cross-schema FK to `fulfillment.shipping_types`; validation via Contracts, so the modules remain
  separable. A future split into separate databases does not require a data migration for this edge.
- Legacy rows/quotes/order snapshots keep their current meaning; no inferred type for anything.

## 8. One bounded implementation handoff (future task, not authorized now)

Slice A — "shared type catalogue read + owner-local assignment persistence":

1. Fulfillment: `ShippingType` + `ShippingTypeTranslation` entities, `fulfillment` schema mapping with
   canonical-code/length CHECKs, additive migration, Contracts DTO + `IShippingTypeReader`,
   DI registration.
2. Shipping: nullable `shipping_type_id` on the draft root, domain preflight (exists + active, read
   through Contracts; fail-closed), `Get/Replace` method-type query+command+validators, Admin and
   Seller endpoints behind the existing owner authorizer, additive migration + CHECK
   (`shipping_type_id IS NULL OR ... canonical uuid` semantics as chosen), revision/concurrency reuse.
3. Proof: real HTTP tests for unconfigured state, set/clear, identical no-op, stale 409, unknown/
   inactive type refusal, cross-owner isolation, admin-vs-seller audience; one PostgreSQL 17 storage
   test for round-trip + no cross-capability loss + a two-reader revision race mirroring TB-194.

Explicitly NOT in Slice A: type administration HTTP endpoints (if platform-curated they are a
separate Fulfillment slice), item/offer compatibility, method-selection restriction, customer
grouping, pricing, activation, UI.

## 9. Focused acceptance proof plan (for that future slice)

- `GET` on a fresh draft returns `isConfigured=false` and `type=null` (not "ordinary").
- `PUT` with a real active type id → 200, revision+1; re-read matches; regions/rates/presentation/
  schedule/settings unchanged.
- Identical `PUT` → truthful no-op, revision unchanged.
- Stale `ExpectedRevision` → 409; unknown/inactive type id → stable listed error code; seller drafting
  another owner's draft → canonical 404.
- Raw-DB: NOT NULL columns, exact CHECKs, unique type code, unique `(type,language)` translation,
  revision token.
- Deterministic two-reader race on the draft root (same barrier precedent as TB-194).
