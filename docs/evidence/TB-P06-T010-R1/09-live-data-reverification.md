# 09 — Live data reverification (TB-P06-T010-R1)

Search scope: `src/frontend/app/fulfillment/**`, customer/seller/admin fulfillment pages.

| Check | Result |
| --- | --- |
| fake tracking arrays in UI | none |
| fake carrier defaults in UI | removed (`carrierName` initial `""`) |
| fake status labels hardcoded | only mapper localization of Host enums |
| fake timestamps | only `formatFulfillmentDate` of API ISO values |
| static shipment arrays | none — all from API mappers |

Unit test fixtures in `fulfillment-api.test.ts` are test-only (not rendered).

Repair: removed pre-filled carrier «پست» in seller detail form.
