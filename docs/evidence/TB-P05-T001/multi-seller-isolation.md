# TB-P05-T001 — Multi-seller isolation

Live proof that Seller A cannot read Seller B operational data, and that a shared CheckoutGroup does not leak cross-seller lines.

## Sellers

| Role | PartyId | Display | Offer |
| --- | --- | --- | --- |
| Seller A | `01a030d1-40cb-7000-8abe-6d31739956c5` | فروشگاه آرمان | LIVE-A `01a030d1-40f1-7000-95f6-b8efc58e2619` |
| Seller B | `01a030d1-40db-7000-b90c-a0705133f0eb` | دیجی‌استایل نمونه | LIVE-B `01a030d1-4111-7000-8102-450cd76a8150` |

## Multi-seller checkout

Created live guest cart with both offers, then submitted checkout:

| Field | Value |
| --- | --- |
| CheckoutId | `01a03600-2f5a-7000-a178-79c81a44ab6d` |
| Seller A order | `01a03600-2f5d-7000-be3b-026ec06414c3` (`TB-20260824225936-01-c40f55`) |
| Seller B order | `01a03600-2f80-7000-9697-6670556187c5` (`TB-20260824225936-02-dc0c81`) |
| Recipient | Multi Seller Buyer |

Checkout payable split into two SellerOrders (A: 2,016,500 IRR; B: 1,951,100 IRR).

## API isolation (header `X-Tooba-Seller-Party-Id`)

| Call | Result |
| --- | --- |
| Seller A `GET /v1/seller/offers` | only LIVE-A |
| Seller B `GET /v1/seller/offers` | only LIVE-B |
| Seller A `GET /v1/seller/offers/{LIVE-B}` | **404** `seller.offer.missing` |
| Seller A `GET /v1/seller/orders/{SellerB}` | **404** `seller.order.missing` |
| Seller B `GET /v1/seller/orders/{SellerA}` | **404** `seller.order.missing` |
| Seller A order detail lines | only LIVE-A line |
| Seller B order detail lines | only LIVE-B line |
| Missing seller header | **400** `seller.identity.missing` |

## UI

As Seller A, opening Seller B offer route `/vendor-panel/products/01a030d1-4111-7000-8102-450cd76a8150` shows Persian denial (evidence `08-seller-authorization-denied.png`).

Authority is Host/composer filter by `SellerPartyId`, not frontend list filtering.
