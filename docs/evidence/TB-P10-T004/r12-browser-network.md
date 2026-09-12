# TB-P10-T004-R12 — Browser / network

- Local Next `:3000` returned HTTP 500 at wake (dev server, not a Host lifecycle defect). Proof of poll/ownership is API + unit tests.
- `shouldPollStorefrontPayment`: false for Succeeded/Failed/Cancelled/Expired; false for manual evidence submitted / canSubmit form; true only transient online Pending/Processing/Verifying; 20s hard stop.
- Cart GET does not ReserveAsync.
- Admin Orders/Payments query is one POST each (runtime N).
- Ownership: paymentId alone 400; wrong guest 401; empty replacement cart 401; client Ensure mode 401 (runtime P).
- Next preload warnings are not production lifecycle loops.
