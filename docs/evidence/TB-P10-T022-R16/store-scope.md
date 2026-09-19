# TB-P10-T022-R16 — Store Scope

## Model

`MerchandisingCampaign.StoreId` is required on every campaign.

Runtime queries always filter `campaign.StoreId == caller storeId`.

`ResolveCampaignMembersAsync` returns empty when campaign store mismatches.

Membership APIs (`AddOfferAsync` / `SyncSeedMembersAsync`) require `expectedStoreId == campaign.StoreId`.

## SellerOffer has no StoreId

Authoritative boundary for SingleStore: **tenant DB connection = store**. Offers loaded via tenant-scoped Offer schema cannot cross tenants. Campaign.StoreId provides explicit in-DB store key for Marketplace multi-store readiness (LOCK-SF-398).

Dev seed uses well-known `StoreAlphaId = aaaaaaaa-aaaa-7aaa-8aaa-aaaaaaaaaaa1`.
