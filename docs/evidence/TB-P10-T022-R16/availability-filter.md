# TB-P10-T022-R16 — Availability Filter

## Reused path

1. `IOfferLookupGateway.FindOffersBatchAsync` — require `OfferStatus.Active`
2. `IInventoryAvailabilityGateway.GetAvailabilityBatchAsync` — sum Available across locations; exclude `Available ≤ 0`
3. Suspended/Archived/Draft offers excluded via status check

Membership alone never makes an offer visible.

## Batching

Single batch load for offers + stock + prices for the membership id set — no per-member queries.

Documented in `MerchandisingCampaignQuery.ProjectMembersAsync`.
