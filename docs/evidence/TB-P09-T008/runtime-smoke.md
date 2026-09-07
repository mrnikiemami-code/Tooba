# Runtime Smoke

Host: http://127.0.0.1:5088 — FE: http://127.0.0.1:3000
DevActor: 01a036c2-970e-7000-8eb7-94bf5cc2d8db

- POST /v1/admin/fulfillments/work-queue/query → total=41, page=20 rows with orderReference/seller/method/actions
- queueFilter needs_action → 39; missing_tracking → 8
- Bulk mark_processing on one ReadyToFulfill row → attempted=1 succeeded=1
- Cross-seller bulk → HTTP 400 rejected
- GET /fa/admin/fulfillments → 200
- No full Orders lifecycle replay
