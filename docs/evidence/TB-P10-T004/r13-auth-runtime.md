# TB-P10-T004-R13 — Authenticated runtime

Host `GetOwnedForPaymentResultAsync`: if `_session.IsAuthenticated`, maps checkout with stub Converted cart — no guest secret and no current Active Cart.

Dev actor header on a **guest** checkout is not a session: GET without guest secret → 404 `checkout.missing` (no leak of the guest Order to an unrelated actor).

Auth shopping Cart rotation uses the same `clearCartSession` + `ensureStorefrontCart` Active-only path. Guest proof is not copied into the auth composer branch.
