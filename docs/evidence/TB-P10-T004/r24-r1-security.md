# TB-P10-T004-R24-R1 — Security

- Cart access remains Host-authoritative (session userId or guest secret). CartId is not a bearer.
- Wrong user cannot read another customer's `/cart/current` or merge.
- Logout clears the browser cart pointer; authenticated cart stays owner-bound.
- Customer orders/address book require session.
- Auth is not inferred from FE-only flags; header uses `/api/auth/me`.
- Saved address ownership still via AddressBook actor.
- After login, shipping draft may be claimed only by the authenticated owner of that cart (guest hash leftover).
