# Storefront responsibility decision — 2026-10-27

Authority: user clarified two families after reviewing the supplied Digikala checkout screenshot:
Seller delivery, or Store/Marketplace's own shipping services. Each group has independent price/date/time;
supermarket/ordinary/heavy are configured DB types/services, not hardcoded speed rules. Existing Tooba
Storefront UI is to be reused.

Architect implementation boundary: seller-specific Offer is the explicit assignment point for nullable
Seller/Platform responsibility and nullable real Fulfillment ShippingTypeId. No inference from SellerPartyId,
user id, display name or absent legacy fields; no automatic backfill. Platform is Store in trusted SingleStore
context and Marketplace in trusted Marketplace context. Seller uses the actual Offer seller identity.
Admin-only scoped assignment initially; seller authority to choose Platform is not granted by this decision.
Batch Contracts reads; no foreign schema join, FK or taxonomy copy. Incomplete assignment stays unavailable.

First vertical slice228 connects real group read projection to existing Storefront cards, using selected
immutable capture and accepted composer. This is NOT permission to make unready methods selectable:
selection/commit of captured plans remains explicitly blocked until authoritative parity plus compact
order evidence is implemented. No silent legacy-price fallback. Existing behavior remains when read opt-in OFF.
Actual two-family policy is resolved; persistence and real wiring were absent before228 and are implementation,
not another question for the user. 227 mobile empty-note residual remains recorded separately.
