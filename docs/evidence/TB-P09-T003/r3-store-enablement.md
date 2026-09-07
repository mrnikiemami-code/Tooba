# R3 store enablement

| Environment | ManualCardToCardEnabled |
|-------------|-------------------------|
| appsettings.json (LOCAL/Dev baseline) | true (for smoke) |
| appsettings.Production.json | false |

`GET /v1/storefront/payment-methods` lists `manual` only when enabled.

Wallet quote includes `manualCardToCardEnabled` for FE picker.

SingleStore/Marketplace: same Gateway options resolve per Host deployment/tenant config; not globally forced on.
