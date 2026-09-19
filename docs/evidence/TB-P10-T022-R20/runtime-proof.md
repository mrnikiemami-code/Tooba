# Runtime Proof — TB-P10-T022-R20

- Host Development on `http://127.0.0.1:5088`
- Probe EXIT 0 — see `probe-report.json`
- Admin APIs under `/v1/admin/merchandising-campaigns`
- Cart regression via `/v1/storefront/cart` with `merchandisingCampaignId`
- Builder section `ProductCollection` + `source=PromotionCampaign` + `campaignId=null`
