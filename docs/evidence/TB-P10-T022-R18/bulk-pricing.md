# TB-P10-T022-R18 — Bulk Pricing

MerchandisingCampaignQuery.ProjectMembersAsync:
1. one membership query (capped)
2. FindOffersBatchAsync
3. GetAvailabilityBatchAsync
4. ResolvePricesBatchAsync (Base)
5. ResolveCampaignPricesBatchAsync (when applyCampaignPrices)
No per-member Pricing roundtrips. Bound by MaxMemberTake=48.
