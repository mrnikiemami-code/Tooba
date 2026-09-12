# TB-P10-T004-R5 — Review Expiry

Expiry worker still releases Held rows with `ExpiresAt <= now`. Review holds use longer ExpiresAt so Cart TTL cannot release AwaitingAdmin inventory.

When review ExpiresAt elapses: Released. Late Confirm uses reacquire path; never resurrects.
