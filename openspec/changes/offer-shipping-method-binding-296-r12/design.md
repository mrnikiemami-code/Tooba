# Design — offer-shipping-method-binding-296-r12

## Identity / ownership
- Method identity is resolved from the existing Shipping public Contracts read seam (published/active authoring methods), never replicated names/rates/types and never a mutable capture/snapshot id.
- `offer.offer_shipping_methods` stores OfferId + canonical stable method identity (and mode), owner-local FK only; no cross-module DB FK.
- Offer owns mode and links; Shipping owns method/type/publication; Order owns checkout eligibility.

## Persistence / CQRS
- Offer revision CAS (`ExpectedVersion`) covers assignment + link replacement in one owning transaction; history records old/new mode and method set; failures roll back all.
- `SetOfferShippingMethodsCommand` + validator; read via `IOfferShippingMethodsLookup`; typed semantic errors; transport is presence-aware so a legacy caller with absent fields preserves current restriction.
- Automatic = no links; Specific = explicit nonempty unique set; switching to Automatic deletes links.

## Eligibility
- Offer read/batch snapshot exposes selected method identities; Checkout candidate projection intersects them with actual owner/type/active/publication candidates; group shipping uses intersection of eligible sets, never union; stale/withdrawn methods are retained in authoring with a warning but never become eligible; captured paid orders are untouched.

## UI
- Reuse approved `page.tsx` presentation extracted into the shared `ShippingTab`; props from typed adapter; existing SearchableSelect/IconButton/MessageBox/tokens; no locale authority; 390×844 popup reachability via the existing overlay pattern.

## Test plan
- 26 immutable IDs SHIP-01..26 executed with real HTTP/Playwright/PostgreSQL; controlled faults only for negative tests; exact fixtures; architecture gates; OpenSpec strict; no fabricated PASS.
