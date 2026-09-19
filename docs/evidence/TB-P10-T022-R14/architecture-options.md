# Architecture Options

## Option 1 — Abuse checkout `PromotionDefinition` as Amazing rail

Reuse `promotion.promotions` with OfferId filter / category filter as “campaign”.

- Pros: no new table
- Cons: wrong semantics (coupons, stacking, single OfferId, no sort membership, no multi-offer campaign, Name not marketing). **Reject.**

## Option 2 — Boolean / tag explosion

`Offer.IsAmazing`, many booleans, or overload `CatalogTag` codes.

- Pros: fast hack
- Cons: violates anti-pattern gate; no windows; no multi-campaign; pollutes Offer. **Reject.**

## Option 3 — Time-bound AuthoredPrice only

Encode Amazing solely as overlapping Price rows.

- Pros: reuses Pricing windows
- Cons: no merchandising membership, priority, teasing, badge, Builder campaign pick, multi-offer sort. Insufficient alone.

## Option 4 — New Merchandising Campaign + membership (recommended)

New tables (names illustrative): `merchandising.campaigns` + `merchandising.campaign_offers` (+ translations), `PromotionType` code `AMAZING`.

- Campaign: type, StartAt/EndAt, priority, status/visibility, optional seller scope
- Membership: CampaignId, OfferId, SortOrder, optional PromoAmount, optional allocation later
- Resolver joins membership → Offer Active → Price → Inventory Available
- Builder source `PromotionCampaign` with nullable CampaignId

Pros: reuses Offer/Price/Stock; clear boundary from checkout Promotion; extensible to lightning/teasing. Cons: new schema (justified — no equivalent).

## Option 5 — Giant rule engine / audience / budget

Overbuild MarketingAutomation. **Reject for foundation.**
