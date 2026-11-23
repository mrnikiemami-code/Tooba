# C01 read-edge finding (real test attempt, 2026-11-24)

While closing C01 with a REAL Testcontainers PostgreSQL integration test (`CampaignCartPriceIntegrityTests`,
candidate 287+288+Promotion), the **add-path** guard was proven and the **read/revalidation edge exposed a real gap**.

## Proven (2/2 pass, committed)
- Active Offer + valid active campaign + stored membership -> campaign price 70000 (authorized discount preserved).
- Wrong campaign id -> base 100000, no campaign context.
- Campaign window expired -> reload reprices to base 100000.
- (These remain green.)

## Newly exposed gap (attempt 1; test addition reverted to keep suite green)
- Sequence: valid active campaign + membership -> offer line priced at 70000 -> archive the Offer via OfferDbContext ->
  `GetCartAsync` reload.
- Expected (C01): campaign drops, base 100000, `MerchandisingCampaignId == null`.
- Observed: reloaded line still carried `MerchandisingCampaignId` (non-null) => **the stored campaign price persisted
  for an archived Offer on the cart read/revalidation edge**.
- Meaning: `CampaignCartPriceAuthority.TryResolveEligibleCampaignPriceAsync` now correctly rejects non-Active Offers,
  but the cart read/revalidation path does not drop an already-applied campaign for a now-Archived Offer (the
  `CartDirectory` revalidation around lines 607/623 needs to re-evaluate and clear the line's campaign when the Offer
  is no longer Active, or the snapshot path must re-quote). This is the exact C01 runtime invariant ("no campaign
  price on cart for archived Offer") and must be fixed before C01 can PASS.

## Not executed
C02 (connected second boot), C04 (287 T01–T14 matrix), C05 (real VERIFY realOk), C07 (Playwright) — NOT_RUN.

No fabricated PASS. The add-path guard is a correct partial improvement; the read-edge fix is the next bounded step.
