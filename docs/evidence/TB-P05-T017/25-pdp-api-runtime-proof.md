# 25 — PDP API Runtime Proof

Task: `TB-P05-T017`

## Endpoints exercised for evidence

| Method | Path | Role |
| --- | --- | --- |
| GET | `/v1/storefront/products/{slug}` | PDP composition |
| GET | `/v1/storefront/products/{slug}/questions` | Published Q&A page |
| POST | `/v1/customer/product-questions` | Authenticated/dev-actor ask |
| POST | `/v1/storefront/products/{slug}/bulk-inquiries` | Guest bulk inquiry |

## Artifacts

- `_api-pdp.json` — live detail payload used for screenshot slug
- `_api-questions.json` — questions list status/body at capture time
- Screenshots `11`–`20` from normal Next (`:3000`) + Host (`:5088`) without request mocking

## Seed

`ProductQnADevelopmentSeed` runs after `StorefrontDemoCatalogBootstrap` with CommerceContext assigned; targets `demo-mobile-1`.
