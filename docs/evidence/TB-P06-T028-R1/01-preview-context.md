# 01 — Preview context

## Identity (Development)

| Role | ID |
| --- | --- |
| Customer actor | `aaaaaaaa-aaaa-4aaa-8aaa-000000000009` |
| Seller actor | `01a03628-3f68-7000-844d-99f1cadb54b0` |
| Seller party | `01a030d1-40cb-7000-8abe-6d31739956c5` |
| Demo offer | `01a04402-7a75-7000-b614-6f093cb072ac` (کتاب دمو) |
| Address | `aaaaaaaa-aaaa-4aaa-8aaa-0000000000a1` |

## How context is built

Legitimate Host APIs only (`docs/evidence/TB-P06-T028-R1/build-preview-urls.mjs`):

1. POST `/v1/storefront/cart`
2. POST cart lines (offer)
3. POST `/v1/storefront/checkout` with saved address + customer actor
4. GET wallet-quote (full cover)
5. Optional: wallet pay → fulfillment deliver → return destination Wallet → approve

**No direct DB mutation.**

## FE bootstrap

Query params on preview URL seed:

- `sessionStorage.tooba.storefront.cartId`
- `sessionStorage.tooba.storefront.guestSecret`
- `localStorage.tooba.customerActorUserId`

Source: `bootstrapCartSessionFromQuery` + confirmation `actor` param.

## Runtime note

Bind Next FE as `localhost:3000` (not `-H 127.0.0.1` alone). Binding only to 127.0.0.1 caused locale rewrite → 308 loops on `/fa/*`.
