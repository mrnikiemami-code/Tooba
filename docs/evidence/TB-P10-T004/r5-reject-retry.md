# TB-P10-T004-R5 — Reject / Retry

- Reject: `ReleaseReservationsAfterManualRejectAsync` releases Held review holds; history remains Released.
- Retry same Payment/Order: `manual-retry` then new evidence → promote path reacquirers if needed → new review hold.
- No duplicate Order. Unlimited reject/retry does not keep inventory forever (each reject releases).
