# 20 — Preview seed quality (TB-P06-T029)

## Policy

Development/Test preview only · idempotent · coherent · not excessive · no Production pollution.

## This commercial demo

| Fact | Value |
| --- | --- |
| Artifact | `commercial-demo.json` |
| `directDbMutation` | **false** |
| `ok` | **true** |
| Checkout | `01a0453b-6829-7000-8c77-32cfb5f5d409` |
| Return | `4497a586-db39-4134-a90e-7b10a3eedde0` |
| Ticket | `01a0453b-707d-7000-b7cf-72e428758f43` |

## Preview coverage

| Expected User Preview page | Empty? |
| --- | --- |
| Storefront Home / Listing / Cart / Checkout | No — LIVE |
| Customer order / wallet / tickets | No — demo IDs + balances |
| Seller orders / returns | No — party-scoped URLs |
| Admin tickets | No |
| Blogs | No — Host published articles |

## Inherited seeds

| Domain | Prior ACCEPT |
| --- | --- |
| Access Control demo identities / categories | T024-R2 |
| Settings profiles / locale | T027 |
| Wallet / gift | T028 / T028-R1 |
| Content articles | T013 |

## Verdict

Development preview remains meaningful for commercial User Preview. Major commercial pages not empty after this demo run.
