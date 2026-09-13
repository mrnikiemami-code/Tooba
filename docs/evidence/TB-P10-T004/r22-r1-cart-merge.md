# TB-P10-T004-R22-R1 — Cart Merge

`POST /v1/storefront/cart/merge` + guest secret. CartId alone is not ownership.

- No active auth cart → adopt guest (`AdoptAuthenticatedOwner`)
- Active auth cart → merge lines by OfferId, sum quantity (decimal exact), revalidate quote; unavailable lines kept
- Guest abandoned after merge; no Reservation/Order/Payment
- Cross-user merge rejected (400 cart.rejected)
- Merge without secret 401

Runtime: adopt 2 lines; later auth+guest merge distinct offers qty 4 on same offer; reservations unchanged until checkout submit (11→12).
Pending seller_orders unchanged by login/merge; +1 only at L submit.
