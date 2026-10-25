# TB-216 — captured revision quote: Contracts edge + verified Geography

Mode: BOUNDED_EXACT_CAPTURE_QUOTE_CONTRACTS_AND_GEOGRAPHY_ADAPTER
Base: `88a28abf2eb1b9914c2e0872531e44dcd3f636be`
Parent: TB-SHIPPING-CAPTURED-MANUAL-QUOTE-215 (ACCEPT_BOUNDED_MANUAL_CAPTURE_QUOTE / NOT_CUSTOMER_ACTIVE)

## What this slice is

The existing TB-215 authority (`ShippingRatePolicyEngine.QuoteCapturedRevision`) is now callable through
`Tooba.Shipping.Contracts` with an **exact** owner + draft + revision and a **verified** Geography path.
It is still a staged read: no HTTP route, no command, no auth bypass, no publication pointer, no
Order/Storefront consumer, and no activation/eligibility claim. `Eligibility` is `NotEvaluated`.

## Contracts (new, cohesive, no Domain/Application/Infrastructure types)

`Tooba.Shipping.Contracts/Rates/ShippingCapturedRevisionQuoteContracts.cs`

- `IShippingCapturedRevisionQuoteLookup.QuoteAsync(ShippingCapturedQuoteLookupRequest, CancellationToken)`
  → `Result<ShippingCapturedQuoteResult>`.
- Request carries only server-resolved data: `ShippingCapturedQuoteOwnerKind` + tenant/seller identity,
  `DraftId`, `SourceRevision`, `DestinationIdentifiers`, and caller-declared group weight/count/final
  after-discount-and-tax amount/currency. No payload, no client price, no latest/current/active field.
- Result carries verdict + stable reason code, null money whenever not quoted, freight/packaging/total,
  checkout vs carrier-collect split, stored settings, snapshot/draft/revision/format, raw type id,
  group and row ids, min AND max delivery days, and the stored receiving week with its time zone.
- `ShippingCapturedQuoteVerdict`, `ShippingCapturedQuoteVerdictCodes`,
  `ShippingCapturedQuoteCustomerEligibility` are Contracts-owned; they intentionally do not reuse the
  Domain enums so a consumer never references Domain types.
- Types are named distinctly (`…Verdict`, `…LookupRequest`) because `Tooba.Shipping.Domain.Rates` already
  owns `ShippingCapturedQuoteOutcome`/`ShippingCapturedQuoteRequest`; two identical names in one file
  would make every consumer test ambiguous.

## Infrastructure adapter (registered once, scoped)

`Tooba.Shipping.Infrastructure/Rates/ShippingCapturedRevisionQuoteLookup.cs`
registered in `ShippingModule.AddServices` next to `IShippingRateQuoteLookup`.

Order of operations (fail-closed at each step, no fallback anywhere):

1. **Owner/edition coherence** using the existing `ICurrentEdition` + `ICurrentTenant` foundation, mirroring
   `ShippingInstallationContext` semantics without importing Endpoints: SingleStore requires an
   `Active` tenant whose id equals the requested Store tenant; Marketplace requires no tenant and accepts
   marketplace-without-seller or seller-with-id; anything else is `OwnerScopeUnavailable`. No fake tenant,
   no default store, no blanket rejection of non-Store owners.
2. **Exact revision read** through the existing `IShippingAuthoringDraftDirectory.GetCapturedRevisionAsync`
   (owner + draft + revision predicate, `AsNoTracking`). Missing/foreign revision →
   `CapturedRevisionUnavailable` before any Geography call.
3. **Payload header integrity** via `ReadPayload()`; a corrupt/foreign body is a defined refusal, not a price.
4. **Verified destination**: `IGeographyPlaceSnapshotReader` reads the country (must be active), the city
   (must be active, same country), and the administrative chain leaf→root (each area active, same country,
   no repeat = no cycle). The declared administrative area must equal the city's real parent; a city with no
   parent cannot be covered by a declared one; a service area must belong to the declared city and be active.
   Every foreign read is wrapped by one canonical `module.Shipping.Geography.<constant>` child span with no
   ids in tags; a response containing a duplicate or an id outside the request fails closed; provider
   exceptions and the caller token propagate unchanged (never caught into an empty success).
5. **One engine call** and an explicit Domain→Contracts map. No JSON round-trip, no cross-module cast.

Read-only: the directory query and `ReadPayload` never write, and `ChangeTracker` is empty after the call
(asserted).

## Two narrow Domain corrections (as reviewed in 215)

1. **Unknown alternative hides ambiguity.** `QuoteCapturedRevision` no longer returns a price when
   `matches == 1 && undetermined > 0`; it returns `MeasurementRequired` with null money until the missing
   axis is supplied. A **known outside** row still does not block (`Quoted` control) — 215's control is kept.
2. **Root admin coverage of a deep city.** `ShippingCapturedRevisionQuoteRequest` gained one additive
   optional `AdministrativeAreaAncestors` (root..leaf, verified by the adapter). When present, a stored
   `AdministrativeArea` node matches any member of that verified chain; when absent, only the leaf matches
   (215 behaviour). A chain that is empty, duplicated, or does not end at the path's leaf is
   `DestinationPathInvalid`. Stored-area identity is never inferred from request-supplied ancestors alone.
   Two groups covering ancestor and descendant ⇒ `GroupAmbiguous` (single-pass, no per-ancestor pricing loop).
   Existing legacy `Quote(...)` overloads and all 215 constructors/callers are unchanged.

## Executed proof (worker-run)

- `dotnet build Tooba.Shipping.Tests` → 0 errors.
- `ShippingCapturedQuoteContractsEdgeTests` → **9/9 passed**:
  1. real module registers exactly ONE scoped `IShippingCapturedRevisionQuoteLookup`
     (`ImplementationType` = adapter; same instance per scope, different across scopes);
  2. exact read returns declared freight `120000.00`, packaging `5000.75`, total/checkout `125000.75`,
     snapshot/draft/revision/format/type/group/row/min+max days/settings/week provenance, and an empty
     ChangeTracker;
  3. missing revision, SingleStore-with-null-tenant and Marketplace-with-Seller-owner all refuse with
     **0 Geography reads**;
  4. a **root** administrative coverage group matches a **deep** city through the verified chain, while a
     foreign-country parent breaks the chain (`DestinationUnresolved`);
  5. contradictory parent, inactive country, duplicated country row and outside-request row all fail closed;
     the lawful null-admin city still quotes;
  6. `CollectOnDelivery` splits checkout `5000.75` (packaging) / carrier `120000.00` (freight);
  7. unconfigured capture → `NotConfigured`, `Free` → `CalculationModeUnavailable`, both with null money
     (never a zero price);
  8. Domain: known-outside row does not block; an unknown alternative forces `MeasurementRequired`; supplying
     the measurement resolves to exactly one row with that row's own packaging;
  9. a provider sentinel keeps its exact instance, one canonical child span (parent span id, correlation,
     `Error`, no ids/messages in tags) and the caller token identity on cancellation.
- `ShippingCapturedRevisionQuoteTests` (215 exact class) → **11/11 passed** (legacy/215 behaviour intact).
- `OperationalQuoteCompatibilityTests` (exact class) → **23/23 passed** (transaction-boundary allowlists
  still hold with the new adapter present).

Not executed: no PostgreSQL, no container, no migration, no Host/FE, no browser, no 43/18/8/119 suites.

## Limits (explicit)

- **NOT CONNECTED** to any customer path. Storefront/checkout still uses the legacy window/price path;
  the authoring draft still never reaches Order.
- Owner key is **data identity, not permission**; the future Order caller must derive it from trusted
  cart/fulfillment responsibility, never from the browser.
- Incoming money and measurements are **caller-declared**; Cart tax/discount group allocation and the real
  weight authority are still missing. A typed UUID is not an existence proof; Geography existence is
  proven here, Cart facts are not.
- `DestinationIdentifiers` cannot express everything Geography owns: a service area is resolved through its
  owning city, and a caller that sends only a service area is refused rather than guessed.
- No active/published capture revision selection, no projection/selection/commit, no publication pointer.
- `Eligibility` is `NotEvaluated` forever in this slice: a monetary quote is not activation or readiness.

## Preserved

Default-OFF gate, TB-185 paused, PendingAllocation unchanged, §29 user decisions untouched, no schema or
migration, no FE/CSS/flag change, real user draft/unsaved work untouched, Host `:5088` / FE `:3000` not
restarted, 9 previously applied migrations not re-applied.

## Next (not issued)

Trusted owner + selected captured revision authority and group financial/measurement authority
(Cart amount after discount AND tax, real weight), then projection/selection/commit. Not another audit.
