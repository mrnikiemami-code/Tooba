# C01 PASS — archived Offer campaign discount removed (real Testcontainers, 2026-11-24)

Fix (Cart owner): in CartDirectory.RevalidateCampaignQuotesAsync, the previous behavior caught OfferUnavailable and
left the old campaign id + campaign price on the line. Now, for a campaign line whose Offer became unavailable/Archived,
it clears the campaign association and reprices at the canonical base price (via IPriceLookupGateway.ResolvePriceAsync);
if no base price exists it fails closed (amount 0, no campaign).

Real integration test CampaignCartPriceIntegrityTests (Testcontainers PostgreSQL 16) -> PASSED 2/2, asserting:
- Active Offer + valid active campaign + stored membership => campaign price 70000 (discount preserved).
- Wrong campaign id => base 100000, no campaign context.
- Campaign window expired => reload reprices to base 100000.
- NEW: Archive the Offer, then reload cart => MerchandisingCampaignId == null and QuotedAmount == 100000 (no retained discount).

This closes C01's runtime invariant (no campaign price for archived Offer on cart/checkout read).
See stage2-proof/cartprice-archived-PASS.txt.
