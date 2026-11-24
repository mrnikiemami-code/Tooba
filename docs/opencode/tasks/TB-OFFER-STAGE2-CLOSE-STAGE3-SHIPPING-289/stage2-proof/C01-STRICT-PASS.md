# C01 STRICT PASS — TB-289-R1 (2026-11-24)
Candidate branch HEAD 7febfd634 (architect snapshot fix + Cart revalidation fix + Promotion guard).
Real integration CampaignCartPriceIntegrityTests (Testcontainers PostgreSQL 16) => Passed 2/2.
Archived Offer (with positive stock) read-side contract observed: Availability == Unavailable, QuotedAmount == null,
PriceId == null  -> not purchasable, no payable price, no retained campaign discount.
Active Offer still receives campaign price 70000; wrong campaign / expired window -> base/fail-closed.
See stage2-proof/C01-strict-PASS.txt.
