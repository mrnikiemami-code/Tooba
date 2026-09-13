# TB-P10-T004-R22-R1 — Runtime A–O

Host `:5088` + FE `:3000`. Script `docs/evidence/TB-P10-T004/_r22-r1-runtime.mjs` — failed 0.

A. Anonymous ATC 2+ lines (plus 1.25 accepted)
B. GET cart 200 without login
C–D. Continue/shipping/checkout/payment 401 `checkout.authentication_required`
E–G. OTP 09111111111 / 123456 session 200
H. Shipping after login 200; browser `/fa/login` → `/fa/shipping`
I. Merged lines still present
J. Existing auth cart + anonymous merge; same-offer qty merged; distinct offers kept
K. No reservation on cart/merge; inventory.reservations 11→12 only at submit
L. Shipping commit 200 checkout `01a09970-6b4d-7000-a12e-0235b2e15d66` PendingPayment (R21 atomic path)
M. Logout: revoked bearer cannot GET cart (400)
N–O. Admin GuestAllowed then restore AuthenticatedOnly

Raw: `_r22-r1-runtime-raw.json`
