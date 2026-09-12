# TB-P10-T004-R4 — Ownership Model

## Rule (LOCKED)

Once Order/Payment exists, Payment result ownership MUST NOT depend on the mutable active Cart.

## Authenticated

- Host Payment GET → OrderAccess via current authenticated session.
- New empty Cart cannot revoke owner access.
- Foreign customer denied.

## Guest

- Narrow committed proof: paymentId + checkoutId + committed cartId + guest secret (sessionStorage, not raw UI).
- Host validates guest secret against checkout’s committed CartId.
- Wrong secret denied; paymentId alone denied; new empty Cart + its secret denied for old Payment.

## Forbidden

- new empty Cart authorizing old Payment
- arbitrary cartId
- DevActor as customer substitute
- paymentId alone
